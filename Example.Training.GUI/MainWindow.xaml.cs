using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using GalaSoft.MvvmLight.CommandWpf;
using GalaSoft.MvvmLight.Messaging;
using ViDi2.Common.Utilities;
using ViDi2.Training.UI;
using ViDi2.Training.UI.ViewModels;
using ViDi2.UI;

/** @file MainWindow.xaml.cs
*  @brief Simple example illustrating the utilisation of the training API in a GUI application
*  @example ViDi.Suite.GUI
*/

namespace ViDi2.Training.GUIExample
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public MainWindow()
        {
            InitializeComponent();

            DataContext = this;

            // Register to receive filter expressions from external sources such as the DatabaseOverview,
            // using the Tool.Database instance as the token to keep filter expression isolated to the right tools
            Messenger.Default.Register<FilterRequestedMessage>(this, databaseExplorer.Database, m =>
            {
                databaseExplorer.Filter = m.Filter;
            });

            workspaceBrowser.WorkspaceSelected += (workspace) =>
            {
                ToolChainViewModel.Workspace = workspace;

                if (workspace != null)
                    workspaceBrowserExpander.IsExpanded = false;
            };

            ToolChainViewModel.ToolSelected += (tool) =>
            {
                var database = tool?.Database ?? ToolChainViewModel.Stream?.Database;

                ToolParametersViewModel = ServiceLocator.Instance.GetNewInstance<IToolParametersViewModelFactory>().Create(tool);
                ViewerViewModel = ServiceLocator.Instance.GetNewInstance<IViewerViewModelFactory>().Create(database);
                DatabaseExplorerViewModel = ServiceLocator.Instance.GetNewInstance<IDatabaseExplorerViewModelFactory>().Create(database, tool?.RecentFilters);
                DatabaseOverviewViewModel = ServiceLocator.Instance.GetNewInstance<IDatabaseOverviewViewModelFactory>().Create(database);
            };

            UpdateCommandBindings();
        }

        #region View Models

        public IToolChainViewModel ToolChainViewModel
        {
            get => toolChain?.ViewModel;
            set
            {
                toolChain.ViewModel?.Cleanup();
                toolChain.ViewModel = value;
                RaisePropertyChanged(nameof(ToolChainViewModel));
            }
        }

        private IDatabaseExplorerViewModel _DatabaseExplorerViewModel;
        public IDatabaseExplorerViewModel DatabaseExplorerViewModel
        {
            get => _DatabaseExplorerViewModel;
            set
            {
                _DatabaseExplorerViewModel?.Cleanup();
                _DatabaseExplorerViewModel = value;
                RaisePropertyChanged(nameof(DatabaseExplorerViewModel));
            }
        }

        private IViewerViewModel _ViewerViewModel;
        public IViewerViewModel ViewerViewModel
        {
            get => _ViewerViewModel;
            set
            {
                _ViewerViewModel?.Cleanup();
                _ViewerViewModel = value;
                RaisePropertyChanged(nameof(ViewerViewModel));
                RaisePropertyChanged(nameof(ToolViewerViewModel));
            }
        }

        public IToolViewerViewModel ToolViewerViewModel => ViewerViewModel as IToolViewerViewModel;

        public IToolParametersViewModel _ToolParametersViewModel;
        public IToolParametersViewModel ToolParametersViewModel
        {
            get => _ToolParametersViewModel;
            set
            {
                _ToolParametersViewModel?.Cleanup();
                _ToolParametersViewModel = value;
                RaisePropertyChanged(nameof(ToolParametersViewModel));
            }
        }

        public IDatabaseOverviewViewModel _DatabaseOverviewViewModel;
        public IDatabaseOverviewViewModel DatabaseOverviewViewModel
        {
            get => _DatabaseOverviewViewModel;
            set
            {
                _DatabaseOverviewViewModel?.Cleanup();
                _DatabaseOverviewViewModel = value;
                RaisePropertyChanged(nameof(DatabaseOverviewViewModel));
            }
        }

        #endregion

        #region Commands

        private IRelayCommand _About;
        public IRelayCommand About => _About ?? (_About = new ViDi2.UI.RelayCommand(() =>
        {
            var aboutDialog = new AboutDialog
            {
                Owner = this,
                DataContext = workspaceBrowser.Control
            };
            aboutDialog.ShowDialog();
        }));

        private IRelayCommand _Exit;
        public IRelayCommand Exit => _Exit ?? (_Exit = new ViDi2.UI.RelayCommand(Close));

        #endregion

        /// <summary>
        /// Clear the commands bindings for this Window and add the command bindings from all child ViDi user controls
        /// </summary>
        void UpdateCommandBindings()
        {
            CommandBindings.Clear();
            CommandBindings.AddRange(workspaceBrowser.CommandBindings);
            CommandBindings.AddRange(viewer.CommandBindings);
            CommandBindings.AddRange(toolChain.CommandBindings);
            CommandBindings.AddRange(databaseExplorer.CommandBindings);
            CommandBindings.AddRange(databaseOverview.CommandBindings);
        }

        private void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }

        public event PropertyChangedEventHandler PropertyChanged;

    }
}
