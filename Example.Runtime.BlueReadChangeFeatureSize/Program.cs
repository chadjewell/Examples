
/**
 * @file Program.cs
 * @brief Example demonstrating the use of the blue read runtime library, while modifying the feature size at runtime.
 */

using System;
using System.IO;
using ViDi2;
using ViDi2.Local;

namespace Example.Runtime
{
    class Program
    {
        /**
         * @brief This example creates a ViDi control, loads a runtime workspace
         * with a blue read tool, having a certain trained feature size,
         * processes an image, and prints the string from the model in the image.
         * The example then modifies the feature size during runtime,
         * processes the same image, and prints the string from the model in the image.
         */
        static void Main(string[] args)
        {

            // holds the main control
            using (ViDi2.Runtime.IControl control = new ViDi2.Runtime.Local.Control())
            {
                // opens a runtime workspace from file
                IWorkspace workspace = control.Workspaces.Add("workspace", "..\\..\\..\\resources\\runtime\\bluereadchangefeaturesize.vrws");

                IStream stream = workspace.Streams["default"];

                // load an image from file
                using (IImage image = new LibraryImage("..\\..\\..\\resources\\images\\Image00524.bmp")) //disposing the image when we do not need it anymore
                {
                    ITool blueReadTool = stream.Tools["Read"];

                    // process image with the blue read tool
                    System.Console.WriteLine($"ToolParameters: Feature Size is {blueReadTool.Parameters.FeatureSize}");

                    using (ISample sample = stream.CreateSample(image))
                    {
                        sample.Process(blueReadTool);

                        IBlueMarking blueReadMarking = sample.Markings[blueReadTool.Name] as IBlueMarking;

                        foreach (IBlueView view in blueReadMarking.Views)
                        {
                            foreach (IMatch match in view.Matches)
                            {
                                System.Console.WriteLine($"This model says \" {(match as IReadModelMatch).FeatureString} \" with a score of {match.Score}");
                            }
                        }

                        // change the feature size of the blue read runtime tool
                        blueReadTool.Parameters.FeatureSize = new Size(20, 35);
                        System.Console.WriteLine();
                        System.Console.WriteLine($"ToolParameters: Feature Size has been changed to {blueReadTool.Parameters.FeatureSize}");

                        // process the same image again with the blue read tool, this time with updated feature size
                        // Since only the feature size changed we can reuse the same sample.
                        sample.Process(blueReadTool);
                        blueReadMarking = sample.Markings[blueReadTool.Name] as IBlueMarking;

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
