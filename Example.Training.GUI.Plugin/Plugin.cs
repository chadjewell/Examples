using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ViDi2.Training;
using ViDi2.Training.UI;

namespace ViDi2.Training.GUI.Plugins
{
    /// <summary>
    /// Example of a plugin assembly that can be loaded by ViDi GUI.
    /// Place the generated assembly in the plugin folder of the GUI and
    /// using the Plugin Manager.
    /// </summary>
    public class ExamplePlugin : IPlugin
    {
        /// <summary>
        /// Context of other plugins, can be used to list other plugins.
        /// </summary>
        IPluginContext context;

        MenuItem pluginMenuItem;

        /// <summary>
        /// Gives a human readeable name of the plugin
        /// </summary>
        string IPlugin.Name { get { return "Example Plugin"; } }

        /// <summary>
        /// Gives a short description of the plugin
        /// </summary>
        string IPlugin.Description
        {
            get { return "Example of a ViDi Gui Plugin \n shows the current workspace/stream/tool"; }
        }

        /// <summary>
        /// Initilization method of the plugin. Called by the ViDi Gui directly after the plugin is loaded.
        /// </summary>
        void IPlugin.Initialize(IPluginContext context)
        {
            this.context = context;

            var pluginContainerMenuItem =
                context.MainWindow.MainMenu.Items.OfType<MenuItem>().
                First(i => (string)i.Header == "Plugins");

            pluginMenuItem = new MenuItem()
            {
                Header = ((IPlugin)this).Name,
                IsEnabled = true,
                ToolTip = ((IPlugin)this).Description
            };

            pluginMenuItem.Click += (o, a) => { Run(); };

            pluginContainerMenuItem.Items.Add(pluginMenuItem);
        }

        /// <summary>
        /// Deinitialization method of the plugin. Called by the ViDi Gui when exiting or when unloading the plugin.
        /// </summary>
        void IPlugin.DeInitialize() 
        {
            var pluginContainerMenuItem =
                context.MainWindow.MainMenu.Items.OfType<MenuItem>().
                First(i => (string)i.Header == "Plugins");

            pluginContainerMenuItem.Items.Remove(pluginMenuItem);
        }

        /// <summary>
        /// Version of the plugin.
        /// </summary>
        int IPlugin.Version { get { return 1; } }

        void Run()
        {
            string message ;
            IWorkspace workspace = context.MainWindow.WorkspaceBrowserViewModel.CurrentWorkspace;
            if (workspace == null)
                message = "No workspace selected";
            else
            {
                message = $"Current workspace is : {workspace.DisplayName}{Environment.NewLine}";
                IStream stream = context.MainWindow.ToolChainViewModel.Stream;

                if(stream != null)
                {
                    message += $"Current stream is : {stream.Name}{ Environment.NewLine}";

                    ITool tool = context.MainWindow.ToolChainViewModel.Tool;
                    if(tool != null)
                    {
                        message += $"Current tool is : {tool.Name} ({tool.Type})";
                    }
                }
            }

            MessageBox.Show(message, "Example Plugin", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
