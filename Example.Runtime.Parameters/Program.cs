/**
 * @file Program.cs
 * @brief Example demonstrating the API for changing parameters and processing a sample
 */

using System;
using System.Linq;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using ViDi2;
using ViDi2.Local;

namespace Example.Runtime
{
    class Program
    {
        /**
         * @brief repeatedly process a red tool and show how the deviation varies with different parameters
         * 
         * We change the threshold and mask and split the grid of the ROI to see how it affects
         * the deviation.
         */
        static void Main(string[] args)
        {
            // Create the local control
            using (ViDi2.Runtime.IControl control = new ViDi2.Runtime.Local.Control())
            {
                // we could also use the same source for a Training Control. 
                // we would have to change " IWorkspace workspace = control.Workspaces.Add("workspace1", "Tutorial 1 - Textile.vrws"); " to match training interface. 

                if (!System.IO.File.Exists("..\\..\\..\\resources\\runtime\\Textile.vrws"))
                {
                    Console.WriteLine(System.IO.Directory.GetCurrentDirectory());
                    Console.WriteLine("tutorial textile runtime workspace not found !");
                    Console.WriteLine("Press any key to exit...");
                    Console.ReadKey();
                    return;
                }

                if (!System.IO.File.Exists("..\\..\\..\\resources\\images\\000004.png"))
                {
                    Console.WriteLine("image 000004.png not found !");
                    Console.WriteLine("Press any key to exit...");
                    Console.ReadKey();
                    return;
                }
                IWorkspace workspace = control.Workspaces.Add("workspace1", "..\\..\\..\\resources\\runtime\\Textile.vrws");

                // store a reference to the stream 'default'
                IStream stream = workspace.Streams["default"];

                // Selects the first tool for the rest of the example

                if (!(stream.Tools.First() is IRedTool))
                {
                    Console.WriteLine("Example requires a red tool as first tool");
                    return;
                }

                using (IImage image = new LibraryImage("..\\..\\..\\resources\\images\\000004.png")) //disposing the image when we do not need it anymore
                {
                    // Analyze is the first tool of the toolchain.
                    IRedTool analyze = stream.Tools.First() as IRedTool;

                    // retrieve and display the feature size, note it is read only using the runtime API

                    Console.WriteLine($"will process image using tool {analyze.Name} with feature size of {analyze.Parameters.FeatureSize} ");

                    using (ISample sample = stream.CreateSample(image))
                    {
                        // process the image by the tool (all upstream tools are processed)
                        // if you specify the last tool in the stream, all tools are processed
                        sample.Process(analyze);

                        IRedMarking AnalyzeResult = sample.Markings[analyze.Name] as IRedMarking;

                        IRedView view = (sample.Markings[analyze.Name] as IRedMarking).Views[0];
                        Console.WriteLine($"First view deviation : {view.Score}");

                        // example changing the threshold of the red tool
                        (analyze.Parameters as IRedToolParameters).Threshold = new Interval(2, 5);

                        // process again with updated threshold
                        sample.Process(analyze);
                        view = (sample.Markings[analyze.Name] as IRedMarking).Views[0];
                        Console.WriteLine($"First view Threshold : {view.Threshold}");

                        // erase the mask
                        analyze.RegionOfInterest.Mask = null;
                        sample.Process(analyze);
                        view = (sample.Markings[analyze.Name] as IRedMarking).Views[0];
                        Console.WriteLine($"First view deviation (without mask): {view.Score}");

                        // Create a mask at the border. The textile example tends to find
                        // erroneous blemishes near the border so masking the border improves
                        // performance.
                        ViDi2.IImage mask = DrawMaskBorder(image.Width, image.Height, 250);
                        analyze.RegionOfInterest.Mask = mask;
                        sample.Process(analyze);
                        view = (sample.Markings[analyze.Name] as IRedMarking).Views[0];
                        Console.WriteLine($"First view deviation (with mask at border) : {view.Score}");

                        // as the mask is a parameter of the red tool it is kept for all further processing
                        sample.Process(analyze);
                        view = (sample.Markings[analyze.Name] as IRedMarking).Views[0];
                        Console.WriteLine($"First view deviation (with same mask at border) : {view.Score}");

                        // check that the tool's ROI is of type manual such that we can change the splitting grid parameter
                        if (analyze.RegionOfInterest is IManualRegionOfInterest)
                        {
                            Console.WriteLine("Divide Image with a 3x3 splitting grid");
                            (analyze.RegionOfInterest as IManualRegionOfInterest).SplittingGrid = new Size(3, 3);
                            sample.Process(analyze);
                            int viewIdx = 0;
                            foreach (var subview in (sample.Markings[analyze.Name] as IRedMarking).Views)
                            {
                                Console.WriteLine($"{ viewIdx++} view deviation : { subview.Score}");
                            }
                        }
                    }
                }
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
        }


        /**
         * @brief creates a mask for an image of size Width, Height with the specified border width
         */
        private static IImage DrawMaskBorder(int Width, int Height, int border)
        {
            // mask is an byte array
            ImageChannelDepth channel_depth = ImageChannelDepth.Depth8;
            byte one = 1;
            byte zero = 0;

            // the mask is only 1 channel
            byte[] data = new byte[Width * Height];
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    data[y * Width + x] = ((y < border) || (y > Height - border) || (x < border) || (x > Width - border)) ? one : zero;
                }
            }

            IntPtr dataPtr = System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(data, 0);

            return new RawImage(Width, Height, 1/*only 1 channel*/, channel_depth, dataPtr, Width);
        }
    }
}
