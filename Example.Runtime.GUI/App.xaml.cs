using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

using ViDi2.Runtime;

namespace Example.Runtime
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void ApplicationStartup(object sender, StartupEventArgs e)
        {
            var mainWindow = new MainWindow();

            Current.MainWindow = MainWindow;
            // Initalize the control
            var control = new ViDi2.Runtime.Local.Control(ViDi2.GpuMode.Deferred);

            // Initializes all CUDA devices
            control.InitializeComputeDevices(ViDi2.GpuMode.SingleDevicePerTool, new List<int>() { });

            mainWindow.Control = control;
            mainWindow.Show();
        }
    }
}
