/**
 * @file Program.cs
 * @brief Example demonstrating the use of the runtime library in a console application
 */

using System;
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
         * @brief This example creates a ViDi control, loads the runtime workspace
         * from the watch dial tutorial. Processes the blue, localize tool from
         * that workspace and prints out the score for each match and feature
         */
        static void Main(string[] args)
        {
            // create a new control
            ViDi2.Runtime.IControl control = null;

            ViDi2.Runtime.IWorkspace workspace = null;

            //----------------------------------------------
            //--------Connection to a Remote Service--------
            // the service must be launched before this example is 
            // and it must listen on port 8080 or modifiy remoteServerAddress
            //----------------------------------------------

            string remoteServerAddress = "http://localhost:8080";

            using (var remoteControl = new ViDi2.Runtime.Remote.Client.Http.HttpControl(ViDi2.FormsImage.Factory))
            {
                try
                {
                    remoteControl.Connect(remoteServerAddress); //specifies the IP address + port of the running runtime server

                    remoteControl.ConnectionMonitor.ServerTimedOut += (e, a) => System.Console.WriteLine("server disconnected");
                    control = remoteControl;
                }
                catch (TimeoutException)
                {
                    System.Console.WriteLine("failed to connect to service");
                    remoteControl.Dispose(); //you must dispose the control and create a new one to retry a connection
                    return;
                }
                if (control.Workspaces.Names.Contains("workspace")) //if the service already holds a runtime workspace, we open it if needed
                {
                    workspace = control.Workspaces["workspace"];
                    if (!workspace.IsOpen)
                    {
                        workspace.Open();
                    }
                }
                else
                {
                    // opens a runtime workspace from file on the client PC
                    //the path to this file relative to the example root folder
                    //and assumes the resource archive was extracted there.
                    workspace = control.Workspaces.Add("workspace", "..\\..\\..\\resources\\runtime\\Dials.vrws");
                }


                // store a reference to the stream 'default'
                IStream stream = workspace.Streams["default"];

                // load an image from file
                // We do not have acess to the vidi libary,
                // in this case we must use a .NET IImage format (i.e FormsImage or WPFImage)
                using (IImage image = new FormsImage("..\\..\\..\\resources\\images\\dial_bad.png")) //disposing the image when we do not need it anymore
                {
                    // Allocate one sample
                    using (ISample sample = stream.CreateSample(image))
                    {
                        ITool localize = stream.Tools["localize"];

                        sample.Process(localize);

                        // process the image by the tool. All upstream tools are also processed
                        IBlueMarking blueMarking = sample.Markings["localize"] as IBlueMarking;

                        if (blueMarking != null)
                        {
                            foreach (IBlueView view in blueMarking.Views)
                            {
                                foreach (IMatch match in view.Matches)
                                {
                                    System.Console.WriteLine($"This match has a score of {match.Score}");
                                }
                                foreach (IFeature feature in view.Features)
                                {
                                    System.Console.WriteLine($"This feature has a score of {feature.Score}");
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
