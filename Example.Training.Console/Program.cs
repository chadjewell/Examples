/**
 * @file Program.cs
 * @brief Example demonstrating the use of the training library in a console application
 */

using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Linq;
using ViDi2;
using System.Collections.Generic;

namespace Example.Training
{
    using System;
    using System.CodeDom;
    using System.CodeDom.Compiler;
    using System.Text.RegularExpressions;
    using ViDi2.Training;

    class Program
    {
        /**
         * @brief This example creates an empty workspace, loads all of the images in a directiory
         * into the database, and trains a blue tool and green tool connected to the root. It also
         * trains a red tool connected to the blue tool.
         * 
         * While the created workspace will likely be meaningless, it relies on the user to populate
         * a directory with images in order to run.
         */
        static void Main(string[] args)
        {
            bool local = true; //changes this to false if you want to connect to a runtime service
                                //remote capabilities are only available with advanced licenses
            string remoteServerAddress = "http://localhost:8080";
            // create a new control
            IControl control = null;

            //----------------------------------------------
            //--------Local usage---------------------------
            //----------------------------------------------
            if (local)
            {
                var workspaceDirectory = new ViDi2.Training.Local.WorkspaceDirectory()
                {
                    Path = "c:\\ViDi" //path where the workspaces are stored
                };

                var libraryAccess = new ViDi2.Training.Local.LibraryAccess(workspaceDirectory);

                // holds the main control
                control = new ViDi2.Training.Local.Control(libraryAccess, GpuMode.SingleDevicePerTool, new List<int>());
            }
            //----------------------------------------------
            //--------Connection to a Remote Service--------
            //----------------------------------------------
            else
            {
                var remoteControl = new ViDi2.Training.Remote.Client.Http.HttpControl(ViDi2.FormsImage.Factory);
                try
                {
                    remoteControl.Connect(remoteServerAddress); //specifies the IP address + port of the running training server

                    remoteControl.ConnectionMonitor.ServerTimedOut += (e, a) => System.Console.WriteLine("server disconnected");
                    control = remoteControl;
                }
                catch(TimeoutException)
                {
                    System.Console.WriteLine("failed to connect to service");
                    remoteControl.Dispose(); //you must dispose the control and create a new one to retry a connection
                    return;
                }
                control = remoteControl;
            }


            // creates a new workspace
            var workspace = control.Workspaces.Add("PBQA");

            //creates a stream
            var stream = workspace.Streams.Add("default");

            //creates a blue tool at the root of the tool chain
            var blue = stream.Tools.Add("Locate", ToolType.Blue) as IBlueTool;
            
            //creates a red tool linked to the blue tool
            //var red = blue.Children.Add("analyze", ToolType.Red) as IRedTool;
            //creates a green tool at the root of the toolchain
            //var green = stream.Tools.Add("classify", ToolType.Green) as IGreenTool;

            //loading images from local directory
            var ext = new System.Collections.Generic.List<string> { ".jpg", ".bmp", ".png" };
            var myImagesFiles = Directory.GetFiles("C:\\Users\\cjewell\\OneDrive - Cognex Corporation\\Documents\\P&G\\PBQA Deep Learning\\images\\all", "*.*", SearchOption.AllDirectories).Where(s => ext.Any(e => s.EndsWith(e)));
            foreach (var file in myImagesFiles)
            {
                using (var image = new FormsImage(file))
                {
                    stream.Database.AddImage(image, Path.GetFileName(file));
                }
            }

            //define file locations for all labels
            var lblext = new System.Collections.Generic.List<string> { ".txt" };
            var myLabelFiles = Directory.GetFiles("C:\\Users\\cjewell\\OneDrive - Cognex Corporation\\Documents\\P&G\\PBQA Deep Learning\\labels\\all", "*.*", SearchOption.AllDirectories).Where(s => lblext.Any(e => s.EndsWith(e)));

            //---------------BLUE TOOL----------------------

            //modifying the ROI
            IManualRegionOfInterest blueROI = blue.RegionOfInterest as IManualRegionOfInterest; //gets the region of interest
            //changing the angle
            //blueROI.Angle = 10.0;

            //processes all images in order to apply the ROI
            blue.Database.Process();
            //waiting for the end of the processing
            blue.Wait();

            //set scaling for features
            blue.Parameters.ScaledFeatures = true;
            blue.Parameters.NonUniformlyScaledFeatures = true;

            //get label file name, get view with file name
            for (var i = 0; i < ((uint)myLabelFiles.Count()); i++)
            {
                //define directory end
                var key = @"\all\";

                //file name Index Start
                var fileIndex = myLabelFiles.ElementAt(i).IndexOf(key) + 5;

                //file length
                var fileLength = myLabelFiles.ElementAt(i).Length;

                //get file name
                var fileName = Regex.Escape(myLabelFiles.ElementAt(i).Substring(fileIndex, myLabelFiles.ElementAt(i).Length - fileIndex - 4));

                //read label data
                var lblData = System.IO.File.ReadAllText(myLabelFiles.ElementAt(i));

                //get corresponding view 
                var view = blue.Database.List($"/{fileName}/.test(filename)").First();

                //parse lbl data
                char[] delim = { ' ', '\r', '\n' };
                string[] parseLbl = lblData.Split(delim);

                //add feature to view in database
                blue.Database.AddFeature(view, "0", new Point(view.Size.Width * Convert.ToDouble(parseLbl[1]), view.Size.Height * Convert.ToDouble(parseLbl[2])), 0, 100);
                blue.Database.SetFeature(view, 0, "0", new Point(view.Size.Width * Convert.ToDouble(parseLbl[1]), view.Size.Height * Convert.ToDouble(parseLbl[2])), 0, new Size(view.Size.Width * Convert.ToDouble(parseLbl[3]), view.Size.Height * Convert.ToDouble(parseLbl[4])));
            }

            //get the first view in the database
            //var firstView = blue.Database.List().First();

            //add some features to the first view in the database
            //blue.Database.AddFeature(firstView, "0", new Point(firstView.Size.Width / 2, firstView.Size.Height / 2), 0.0, 1.0);
            //blue.Database.AddFeature(firstView, "1", new Point(firstView.Size.Width / 3, firstView.Size.Height / 3), 0.0, 1.0);

            //adding a model to the blue tool
            //var model = blue.Models.Add("model1") as INodeModel;
            //adding some nodes in the model
            //var node = model.Nodes.Add();
            //node.Fielding = new List<string> { "1" };
            //node.Position = new Point(0.0, 0.0);
            //node = model.Nodes.Add();
            //node.Fielding = new List<string> { "0" };
            //node.Position = new Point(1.0, 0.0);

            //changing some parameters
            //blue.Parameters.FeatureSize = new Size(30, 30);

            //saving the workspace
            workspace.Save();

            //trains and wait for the training to be finished
            blue.Train();

            try
            {
                while (!blue.Wait(1000))
                {
                    System.Console.WriteLine(blue.Progress.Description + " " + blue.Progress.ETA.ToString());
                }
            }
            catch (ViDi2.Exception e)
            {
                /* you'll likely get a "numeric instability detected" exception
                 * that will put you right here. That happens because the resources
                 * that are being used from the "images" folder are probably not
                 * well suited for the specific stream that we have set up.
                 */
                System.Console.WriteLine(e.Message);
                return;
            }

            var blueSummary = blue.Database.Summary();
            /*
            //---------------RED TOOL----------------------

            //setting the roi in the red tool. It is a IBlueRegionOfInterest because the red tool is linked to a blue tool
            var redRoi = red.RegionOfInterest as IBlueRegionOfInterest;
            //selecting the model used for the ROI
            redRoi.MatchFilter = "name='" + model.Name + "'";
            //setting the size of the ROI
            redRoi.Size = new Size(100, 100);

            //applying the ROI on all images
            red.Database.Process();

            //waiting for red tool to finish applying ROI
            red.Wait();

            //changing the rotation perturbation parameter
            red.Parameters.Rotation = new System.Collections.Generic.List<Interval>() { new Interval(0.0, 360.0) };

            //labellling images
            red.Database.LabelViews("'bad'", "bad"); //label good images
            red.Database.LabelViews("not 'bad'", ""); // label bad images
            workspace.Save();

            //training the workspace
            red.Train();
            try
            {
                while (!red.Wait(1000))
                {
                    System.Console.WriteLine(red.Progress.Description + " " + red.Progress.ETA.ToString());
                }
            }
            catch (ViDi2.Exception e)
            {
                System.Console.WriteLine(e.Message);
            }
            var redSummary = red.Database.Summary();

            //---------------GREEN TOOL----------------------

            //Applying the ROI to the green tool
            green.Database.Process();
            red.Wait();

            //tagging the images
            green.Database.Tag("'bad'", "b");
            green.Database.Tag("not 'bad'", "g");

            workspace.Save();
            green.Train();
            try
            {
                while (!green.Wait(1000))
                {
                    System.Console.WriteLine(green.Progress.Description + " " + green.Progress.ETA.ToString());
                }
            }
            catch (ViDi2.Exception e)
            {
                System.Console.WriteLine(e.Message);
            }

            var greenSummary = green.Database.Summary();
            */
            //closing the workspaces
            foreach (var w in control.Workspaces)
            {
                w.Close();
            }
            control.Dispose();

        }

        private static string ToLiteral(string input)
        {
            using (var writer = new StringWriter())
            {
                using (var provider = CodeDomProvider.CreateProvider("CSharp"))
                {
                    provider.GenerateCodeFromExpression(new CodePrimitiveExpression(input), writer, null);
                    return writer.ToString();
                }
            }
        }
    }    
}
