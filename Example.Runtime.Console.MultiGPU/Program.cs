/**
 * @file Program.cs
 * @brief Example illustrating the use of the runtime library in a console application using multiple GPUs
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ViDi2;

namespace Example.Runtime
{
    class Program
    {
        /**
         * @brief this example uses the runtime workspace from the textile tutorial
         * to show how to process multiple samples in parallel using multiple GPUs and how to
         * process a sample using multiple GPUs per tool. These use correspond with
         * maximizing throughput and minimizing latency respectively.
         */
        static void Main(string[] args)
        {
            #region MaximizeThroughput
             
            // (1) 
            //
            // To maximize throughput all tools will use only one GPU. We can then use a hardware concurrency
            // equal to the number of GPUs.
            {
                System.Console.WriteLine("Example maximizing throughput");

                List<int> GPUList = new List<int>();
                // We could instead specify which gpu to use by initializing with :
                // List<int> GPUList = new List<int>(){0,1}; 
                // to use only first and second GPUs

                // Initialize a control
                using (ViDi2.Runtime.IControl control = new ViDi2.Runtime.Local.Control(GpuMode.Deferred, GPUList))
                {
                    // Initialilizes the Compute devices
                    // Parameters : - GPUMode.SingleDevicePerTool each tool will use a single GPU -> Maximizing throughput
                    //              - new GPUList : automatically resolve all available gpus if empty
                    control.InitializeComputeDevices(GpuMode.SingleDevicePerTool, GPUList);

                    var computeDevices = control.ComputeDevices;

                    // the example will run with fewer than 2 GPUs, but the results might not be meaningful
                    if (computeDevices.Count < 2)
                    {
                        Console.WriteLine("Warning ! Example needs at least two GPUs to be meaningfull");
                    }

                    Console.WriteLine("Available computing devices :");
                    foreach (var computeDevice in computeDevices)
                    {
                        Console.WriteLine($"{computeDevice.Name} : memory {computeDevice.Memory}");
                    }

                    // opens a runtime workspace from file
                    string WorkspaceFile = "..\\..\\..\\resources\\runtime\\Textile.vrws";

                    if (!File.Exists(WorkspaceFile))
                    {
                        // if you got here then it's likely that the resources were not extracted in the path
                        Console.WriteLine($"{WorkspaceFile} does not exist");
                        Console.WriteLine($"Current Directory = { Directory.GetCurrentDirectory()}");
                        return;
                    }

                    var workspace = control.Workspaces.Add("workspace1", "..\\..\\..\\resources\\runtime\\Textile.vrws");

                    // store a reference to the stream 'default'
                    var stream = workspace.Streams["default"];

                    String ImageName = "..\\..\\..\\resources\\images\\000000.png";
                    if (!File.Exists(ImageName))
                    {
                        Console.WriteLine($"{ImageName} does not exist");
                        return;
                    }
                    // load an image from file
                    IImage img = new ViDi2.Local.LibraryImage(ImageName);

                    // warm up operation, first call to process takes additionnal time
                    stream.Process(img);

                    // Action processing 10 images, DeviceId defines which device to use
                    Action<int> ProcessAction = new Action<int>((int DeviceId) =>
                    {
                        Console.WriteLine($"processing on device {DeviceId}");
                        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
                        sw.Start();
                        for (var iteration = 0; iteration < 10; ++iteration)
                        {
                            using (ISample sample = stream.CreateSample(img))
                            {
                            // process all tools
                            sample.Process(null, new List<int>() { DeviceId });

                                double totalDurationMs = 0;
                            // Iterating over all the markings to get ViDi Processing time
                            foreach (var marking in sample.Markings)
                                {
                                    totalDurationMs += marking.Value.Duration;
                                }
                                Console.WriteLine($"image processed on device {DeviceId} in {totalDurationMs} ms");
                            }
                        }
                        Console.WriteLine($"10 images processed on device {DeviceId} in {sw.ElapsedMilliseconds} ms");
                    });

                    var tasks = new List<Task>();

                    System.Diagnostics.Stopwatch globalSw = new System.Diagnostics.Stopwatch();

                    Console.WriteLine("----------------------------------------------------------");
                    Console.WriteLine($"Will now process 10*{computeDevices.Count} images with {computeDevices.Count} threads");

                    globalSw.Start();
                    // We will launch as many concurrent thread as there are devices available
                    for (int k = 0; k < computeDevices.Count; ++k)
                    {
                        int DeviceId = k;
                        tasks.Add(Task.Factory.StartNew(() => ProcessAction(DeviceId)));
                    }
                    // wait for all tasks to finish
                    Task.WaitAll(tasks.ToArray());

                    Console.WriteLine($"Processed {computeDevices.Count}*10 images in { globalSw.ElapsedMilliseconds} ms");
                    Console.WriteLine("----------------------------------------------------------");
                    Console.WriteLine("-----------Reference Measure using only one GPU-----------");
                    Console.WriteLine("----------------------------------------------------------");
                    Console.WriteLine("Will now process 10 images on device 0 with a single thread");

                    // Processes all images using only one thread to get a measure of processing time without overhead of using
                    //  multiple threads
                    ProcessAction(0);
                }
            }

            #endregion

            #region MinimizeLatency
            // (2) Minimize Latency
            //
            // To minimize latency, we will ask the stream to use all GPUs to process a single image.
            // Notes : This only applies to red tools
            //         There is overhead when dispatching the processing to multiple tools. 
            //         This is not recommended for small images or red tools with big feature sizes
            //         Always prefer maximize throughput (1) if possible.
            {
                Console.WriteLine();
                Console.WriteLine("Example minimizing latency");

                List<int> GPUList = new List<int>();
                // We could instead specify which gpu to use by initializing with :
                // List<int> GPUList = new List<int>(){0,1}; 
                // to use only first and second GPUs

                // Initialize a control

                using (ViDi2.Runtime.IControl control = new ViDi2.Runtime.Local.Control(GpuMode.Deferred, GPUList))
                {
                    // Initialilizes the Compute devices
                    // Parameters : - GPUMode.MultipleDevicesPerTool each tool will use a single GPU -> Maximizing throughput
                    //              - new GPUList : automatically resolve all available gpus if empty,
                    control.InitializeComputeDevices(GpuMode.MultipleDevicesPerTool, GPUList);
                    var computeDevices = control.ComputeDevices;

                    // the example will run with fewer than 2 GPUs, but the results might not be meaningful
                    if (computeDevices.Count < 2)
                    {
                        Console.WriteLine("Warning ! Example needs at least two GPUs to be meaningfull");
                    }

                    Console.WriteLine("Available computing devices :");
                    foreach (var computeDevice in computeDevices)
                    {
                        Console.WriteLine($"{computeDevice.Name} : memory {computeDevice.Memory}");
                    }

                    string WorkspaceFile = "..\\..\\..\\resources\\runtime\\Textile.vrws";

                    if (!File.Exists(WorkspaceFile))
                    {
                        // if you got here then it's likely that the resources were not extracted in the path
                        Console.WriteLine($"{WorkspaceFile} does not exists");
                        Console.WriteLine($"Current Directory = {Directory.GetCurrentDirectory()}");
                        return;
                    }

                    var workspace = control.Workspaces.Add("workspace1", "..\\..\\..\\resources\\runtime\\Textile.vrws");

                    // store a reference to the stream 'default'
                    var stream = workspace.Streams["default"];

                    String ImageName = "..\\..\\..\\resources\\images\\000000.png";
                    if (!File.Exists(ImageName))
                    {
                        Console.WriteLine("{ImageName} does not exists");
                        return;
                    }
                    // load an image from file
                    IImage img = new ViDi2.Local.LibraryImage(ImageName);

                    // warm up operation, sometimes first call takes additionnal time
                    stream.Process(img);

                    System.Diagnostics.Stopwatch globalSw = new System.Diagnostics.Stopwatch();

                    List<int> DevicesToUse = new List<int>();
                    for (var Device = 0; Device < computeDevices.Count; ++Device)
                    {
                        DevicesToUse.Add(Device);
                    }

                    Console.WriteLine("-----------------------------------------------------------");
                    Console.WriteLine($"Will now process the image using Devices [{string.Join(",", DevicesToUse)}]");

                    globalSw.Start();
                    for (var iteration = 0; iteration < 10; ++iteration)
                    {
                        using (ISample sample = stream.CreateSample(img))
                        {
                            sample.Process(null, DevicesToUse);

                            double totalDurationMs = 0;
                            // Iterating over all the markings to get ViDi Processing time
                            foreach (var marking in sample.Markings)
                            {
                                totalDurationMs += marking.Value.Duration;
                            }
                            Console.WriteLine($"image processed on devices [{string.Join(",", DevicesToUse)}] in {totalDurationMs} ms");
                        }
                    }
                    Console.WriteLine($"Processed 10 images in {globalSw.ElapsedMilliseconds} ms");

                    // dispose the control to free all resources
                }            }

            #endregion

            return;
        }
    }
}

