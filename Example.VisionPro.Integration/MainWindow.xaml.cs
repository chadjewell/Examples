using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Cognex.VisionPro;
using Cognex.VisionPro.ToolBlock;
using Cognex.VisionPro.ToolGroup;
using ViDi2.VisionPro;
using ViDi2.VisionPro.Records;
using ViDi2;
using ViDi2.UI;
using Exception = System.Exception;
using IControl = ViDi2.Runtime.IControl;
using IStream = ViDi2.Runtime.IStream;
using IWorkspace = ViDi2.Runtime.IWorkspace;

/** @file MainWindow.xaml.cs
*  @brief Simple example illustrating the utilisation of the runtime API
*  @example Example.Runtime.cs
*/

namespace Example.VisionPro.Integration
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private VisionProIntegration _vProIntegration;

        public MainWindow()
        {
            InitializeComponent();

            _vProIntegration = new VisionProIntegration();
            DataContext = _vProIntegration;

            Display = new CogRecordDisplay();
            WfHost.Child = Display;

            DragEnter += CheckDrop;
            DragOver += CheckDrop;
            Drop += DoDrop;
        }

        /// <summary>
        /// The display used to show an ICogImage and
        /// a record containing its result graphics.
        /// </summary>
        private CogRecordDisplay Display { get; }

        /// <summary>
        /// Makes sure a VisionPro tool block and
        /// a Deep Learning stream are loaded before
        /// accepting images.
        /// </summary>
        void CheckDrop(object sender, DragEventArgs e)
        {
            e.Effects = _vProIntegration.Stream != null && _vProIntegration.ToolBlock != null
                ? DragDropEffects.All : DragDropEffects.None;
            e.Handled = true;
        }

        /// <summary>
        /// Runs the VisionPro and Deep Learning tools
        /// on the dropped image, updating the VisionPro
        /// display with the image and graphics.
        /// </summary>
        private void DoDrop(object sender, DragEventArgs e)
        {
            var lst = (IEnumerable<string>)e.Data.GetData(DataFormats.FileDrop);
            var fileName = lst.First();

            try
            {
                using (var image = new WpfImage(fileName))
                {
                    // Get a record containing the deep learning and VPro results
                    var resultRecord = _vProIntegration.RunTools(image);

                    if (_vProIntegration.RecordConfig == null)
                    {
                        _vProIntegration.RecordConfig = new RecordConfiguration(resultRecord);
                    }

                    // Hide the records that were previously hidden, if any were hidden
                    _vProIntegration.RecordConfig.ApplyRecordConfiguration(resultRecord);

                    Display.Record = resultRecord;
                    Display.Fit(true);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Failed to Load",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void openWorkspace_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                DefaultExt = ".vrws",
                Filter = "ViDi Runtime Workspaces (*.vrws)|*.vrws"
            };

            if (dialog.ShowDialog() == true)
            {
                using (var fs = new System.IO.FileStream(dialog.FileName, System.IO.FileMode.Open))
                {
                    Mouse.OverrideCursor = Cursors.Wait;
                    var workspace = _vProIntegration.Control.Workspaces.Add(System.IO.Path.GetFileNameWithoutExtension(dialog.FileName), fs);

                    if (_vProIntegration.IsRuntimeWorkspaceValid(workspace))
                    {
                        _vProIntegration.Workspace = workspace;
                        _vProIntegration.WorkspaceName = Path.GetFileName(dialog.FileName);
                    }
                    else
                    {
                        _vProIntegration.Control.Workspaces.Remove(workspace.UniqueName);
                        MessageBox.Show(this, "Workspace must have at least one stream with at least one tool.", "Invalid Workspace", MessageBoxButton.OK, MessageBoxImage.Error);
                    }

                    Mouse.OverrideCursor = null;
                }
            }
        }

        private void openFixturing_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    DefaultExt = ".vpp",
                    Filter = "VisionPro Persistence Files (*.vpp)|*.vpp"
                };

            if (dialog.ShowDialog() == true)
            {
                Mouse.OverrideCursor = Cursors.Wait;
                var toolBlock = CogSerializer.LoadObjectFromFile(dialog.FileName) as CogToolBlock;

                if (_vProIntegration.IsToolBlockValid(toolBlock))
                {
                    _vProIntegration.ToolBlock = toolBlock;
                    _vProIntegration.ToolBlockName = Path.GetFileName(dialog.FileName);
                }
                else
                {
                    MessageBox.Show(this,
                                    "Fixturing tool block must take only one input (of ICogImage) and output at least one CogTransform2DLinear.",
                                    "Invalid Tool Block",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Error);
                }

                Mouse.OverrideCursor = null;
            }
        }

        /// <summary>
        /// Hide all graphics for the selected view, if they are currently shown; otherwise, show them.
        /// After the graphics are shown/hidden, a new RecordConfiguration is saved,
        /// describing which views should be shown/hidden on future executions.
        /// </summary>
        private void toggleViewButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (_vProIntegration.SelectedViewRecord != null)
            {
                var show = !_vProIntegration.SelectedViewRecord.GraphicsVisible;
                _vProIntegration.SelectedViewRecord.SetGraphicVisibilityForSelfAndSubRecords(show);

                // Save which views are shown/hidden as a RecordConfiguration
                _vProIntegration.RecordConfig = new RecordConfiguration(Display.Record);
            }
        }
    }
}
