/**
 * @file Program.cs
 * @brief Example demonstrating the use of the runtime library in a console application
 */

using System;
using System.IO;
using System.Collections.Generic;
using ViDi2;
using ViDi2.Local;

namespace Example.Runtime
{
    class Program
    {
        /**
         * @brief This example creates a ViDi control, loads the runtime workspace
         * from the watch dial tutorial. Processes the blue, localize tool from
         * that workspace and prints out the score for each match and feature
         */
        static void Main(string[] args)
        {
            // Initializes the control
            // This initialization does not allocate any gpu ressources.
            using(ViDi2.Runtime.Local.Control control = new ViDi2.Runtime.Local.Control(GpuMode.Deferred))
            {

                // Initializes all CUDA devices
                control.InitializeComputeDevices(GpuMode.SingleDevicePerTool, new List<int>() { });

                // Open a runtime workspace from file
                // the path to this file relative to the example root folder
                // and assumes the resource archive was extracted there.
                ViDi2.Runtime.IWorkspace workspace = control.Workspaces.Add("workspace", "..\\..\\..\\resources\\runtime\\Dials.vrws");

                // Store a reference to the stream 'default'
                IStream stream = workspace.Streams["default"];

                // Load an image from file
                using (IImage image = new LibraryImage("..\\..\\..\\resources\\images\\dial_bad.png")) //disposing the image when we do not need it anymore
                {
                    // Allocates a sample with the image
                    using (ISample sample = stream.CreateSample(image))
                    {
                        ITool blueTool = stream.Tools["localize"];

                        // Process the image by the tool. All upstream tools are also processed
                        sample.Process(blueTool);
                        IBlueMarking blueMarking = sample.Markings[blueTool.Name] as IBlueMarking;

                        foreach (IBlueView view in blueMarking.Views)
                        {
                            foreach (IMatch match in view.Matches)
                            {
                                Console.WriteLine($"This match has a score of {match.Score}");
                            }
                            foreach (IFeature feature in view.Features)
                            {
                                Console.WriteLine($"This feature has a score of {feature.Score}");
                            }
                        }

                        // Get the red tool
                        ITool redTool = stream.Tools["analyze"];

                        // We can process the red tool, it won't reprocess the blue tool since the result
                        // is up to date.
                        sample.Process(redTool);

                        IRedMarking redMarking = sample.Markings[redTool.Name] as IRedMarking;

                        foreach (IRedView view in redMarking.Views)
                        {
                            System.Console.WriteLine($"This view has a score of {view.Score}");
                            foreach (IRegion region in view.Regions)
                            {
                                Console.WriteLine($"This region has a score of {region.Score}");
                            }
                        }
                    }
                }
            }
        }
    }
}