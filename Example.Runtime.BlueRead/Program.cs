/**
 * @file Program.cs
 * @brief Example demonstrating the use of the blue read runtime library
 */

using System;
using System.Collections.Generic;
using System.IO;
using ViDi2;
using ViDi2.Local;

namespace Example.Runtime
{
    class Program
    {
        /**
         * @brief This example creates a ViDi control, loads a runtime workspace
         * with a blue read tool, processes an image, and prints the string from the
         * model in the image.
         */
        static void Main()
        {
            // holds the main control
            using (ViDi2.Runtime.IControl control = new ViDi2.Runtime.Local.Control(GpuMode.Deferred))
            {
                // Initializes all CUDA devices
                control.InitializeComputeDevices(GpuMode.SingleDevicePerTool, new List<int>() { });

                // opens a runtime workspace from file
                IWorkspace workspace = control.Workspaces.Add("workspace", "..\\..\\..\\resources\\runtime\\blueread.vrws");

                IStream stream = workspace.Streams["default"];

                // load an image from file
                using (IImage image = new LibraryImage("..\\..\\..\\resources\\images\\Image_34.png")) //disposing the image when we do not need it anymore
                {
                    // allocates a sample with the image
                    using (ISample sample = stream.CreateSample(image))
                    {
                        ITool tool = stream.Tools["Read"];

                        // process image with the blue read tool
                        sample.Process(tool);

                        IBlueMarking blueReadMarking = sample.Markings[tool.Name] as IBlueMarking;

                        foreach (IBlueView view in blueReadMarking.Views)
                        {
                            foreach (IMatch match in view.Matches)
                            {
                                System.Console.WriteLine($"This model says \" {(match as IReadModelMatch).FeatureString} \" with a score of {match.Score}");
                            }
                        }
                    }
                }
            }
        }
    }
}
