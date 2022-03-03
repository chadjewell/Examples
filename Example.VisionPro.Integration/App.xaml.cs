using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Example.VisionPro.Integration;
using ViDi2.Runtime;

namespace Example.VisionPro.Integration
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void ApplicationStartup(object sender, StartupEventArgs e)
        {
            ViDi2.VisionPro.Compatibility.EnableVisionProVersionCompatibility();
            var mainWindow = new MainWindow();

            Current.MainWindow = MainWindow;

            var control = new ViDi2.Runtime.Local.Control();
            VisionProIntegration vProIntegration = mainWindow.DataContext as VisionProIntegration;
            if (vProIntegration != null)
            {
                vProIntegration.Control = control;
            }

            mainWindow.Show();
        }
    }
}
