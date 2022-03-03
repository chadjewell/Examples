using System;
using System.Linq;
using System.Windows;
using ViDi2.Common;
using ViDi2.Common.Utilities;
using ViDi2.Training.UI;
using ViDi2.UI;
using ViDi2.UI.ViewModels;

//using GalaSoft.MvvmLight.Messaging;

namespace ViDi2.Training.GUIExample
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application, IExpertModeStateService
    {   
        private void ApplicationStartup(object sender, StartupEventArgs e)
        {
            ViDi2.UI.ViDiSuiteServiceLocator.Initialize();

            var mainWindow = new MainWindow();

            Current.MainWindow = MainWindow;

            var workspaceDirectory = new ViDi2.Training.Local.WorkspaceDirectory();
            var control = new ViDi2.Training.Local.Control(new ViDi2.Training.Local.LibraryAccess(workspaceDirectory));

            mainWindow.workspaceBrowser.Control = control;

            mainWindow.Closing += (o, a) =>
            {
                foreach (var workspace in control.Workspaces)
                    if (workspace.IsModified)
                    {
                        var result = MessageBox.Show($"Do you want to save the changes in workspace '{workspace.DisplayName}'?",
                            "Closing ViDi Suite", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

                        if (result == MessageBoxResult.Yes)
                            workspace.Save();
                        else if (result == MessageBoxResult.Cancel)
                        {
                            a.Cancel = true;
                            return;
                        }
                    }

                IWorkspace ws = null;
                while ((ws = control.Workspaces.FirstOrDefault(w => w.IsOpen)) != null)
                    ws.Close();

                control.Dispose();

                (ViDi2.UI.ViDiSuiteServiceLocator.Instance as IDisposable)?.Dispose();
            };

            GalaSoft.MvvmLight.Threading.DispatcherHelper.Initialize();
            ServiceLocator.Instance.Register<IExpertModeStateService>(() => this);

            mainWindow.Show();
        }

        public bool IsExpertMode => false;

        public bool IsPreviewMode => false;
    }
}
