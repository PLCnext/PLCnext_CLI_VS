#region Copyright
///////////////////////////////////////////////////////////////////////////////
//
//  Copyright (c) Phoenix Contact GmbH & Co KG
//  This software is licensed under Apache-2.0
//
///////////////////////////////////////////////////////////////////////////////
#endregion

using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.VCProjectEngine;
using PlcncliServices.CommandResults;
using PlcncliServices.PLCnCLI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using System.Windows.Input;
using Constants = PlcncliCommonUtils.Constants;
using Path = System.IO.Path;

namespace PlcncliFeatures.PlcNextProject.ProjectConfigWindow
{
    public class ProjectConfigWindowViewModel : INotifyPropertyChanged, IDataErrorInfo
    {
        private readonly string projectDirectory;
        private readonly string projectName;
        private readonly LibViewModel selectAll;

        public ProjectConfigWindowViewModel(IPlcncliCommunication plcncliCommunication)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            DTE2 dte = Package.GetGlobalService(typeof(DTE)) as DTE2;

            selectAll = new LibViewModel(this, "Select/Deselect all elements");
            GetProjectLocation(out projectDirectory, out projectName);
            
            void GetProjectLocation(out string projectDirectory, out string projectName)
            {
                projectName = string.Empty;
                projectDirectory = string.Empty;

                Array selectedItems = dte?.ToolWindows.SolutionExplorer.SelectedItems as Array;
                if (selectedItems == null || selectedItems.Length != 1)
                {
                    return;
                }

                if (!((selectedItems.GetValue(0) as UIHierarchyItem)?.Object is Project project))
                {
                    return;
                }

                projectName = project.Name;
                projectDirectory = project.FullName;
            }

            if (string.IsNullOrEmpty(projectDirectory))
            {
                return;
            }

            ProjectInformationCommandResult projectInformation = GetProjectInformation();
            IEnumerable<string> externalLibs = Enumerable.Empty<string>();
            if (projectInformation != null)
            {
                externalLibs = projectInformation.ExternalLibraries.Select(p => Path.GetFileName(p.PathValue));
                GenerateNamespaces = projectInformation.GenerateNamespaces;
            }

            IEnumerable<LibViewModel> libs = null;
            ProjectConfiguration config;
            LoadFromFile();

            SigningViewModel = new SigningViewModel(config, projectName);

            ExcludedFiles = libs != null
                                ? new ObservableCollection<LibViewModel>(libs)
                                : new ObservableCollection<LibViewModel>();
            
            
            if (projectInformation.Type != Constants.ProjectType_PLM &&
                projectInformation.Type != Constants.ProjectType_ACF)
            {
                ShowGenerateNamespaces = false;
            }
            if (projectInformation.Type != Constants.ProjectType_PLM
                && projectInformation.Type != Constants.ProjectType_SN)
            {
                ShowExcludedFiles = false;
                return;
            }
            

            //project library itself can be selected as excluded file for shared native projects only
            string projectLibraryName = $"lib{projectInformation.Name}.so";
            LibViewModel projectLibrary = ExcludedFiles.Where(f => f.Name == projectLibraryName).FirstOrDefault();
            if (projectInformation.Type == Constants.ProjectType_SN && projectLibrary == null)
            {
                projectLibrary = new LibViewModel(this, projectLibraryName);
                ExcludedFiles.Add(projectLibrary);
            }

            foreach (string lib in externalLibs)
            {
                if (!ExcludedFiles.Select(f => f.Name).Contains(lib))
                {
                    ExcludedFiles.Add(new LibViewModel(this, lib));
                }
            }

            if (!ExcludedFiles.Any())
            {
                ExcludedFiles.Add(new LibViewModel(this, "No files found", invalid: true));
                EnableExcludedFiles = false;
            }
            else
            {
                ExcludedFiles.Insert(0,selectAll);
                if (libs != null && ExcludedFiles.Count == libs.Count() + 1)
                {
                    selectAll.SetSelected(true);
                }
            }

            foreach (LibViewModel lib in ExcludedFiles.Where(f => !externalLibs.Contains(f.Name) && f != selectAll && f != projectLibrary))
            {
                lib.SetInvalid();
            }

            ProjectInformationCommandResult GetProjectInformation()
            {
                ProjectInformationCommandResult result = null;
                Projects projects = dte.Solution?.Projects;
                string configurationName = null;
                if (projects != null)
                {
                    foreach (Project project in projects)
                    {
                        if (project.Name == Path.GetFileNameWithoutExtension(projectDirectory))
                        {
                            VCProject vcProject = project.Object as VCProject;
                            configurationName = vcProject?.ActiveConfiguration?.ConfigurationName;
                        }
                    }
                }

                ThreadHelper.JoinableTaskFactory.Run(
                "Fetching project information",
                async (progress) =>
                {
                    progress.Report(new ThreadedWaitDialogProgressData("Fetching external libraries..."));

                    result = plcncliCommunication.ExecuteCommand(
                        Constants.Command_get_project_information,
                        null,
                        typeof(ProjectInformationCommandResult),
                        Constants.Option_get_project_information_project,
                        "\""+Path.GetDirectoryName(projectDirectory)+"\"",
                        Constants.Option_get_project_information_buildtype,
                        configurationName?.StartsWith("Debug", StringComparison.OrdinalIgnoreCase) == true ? "Debug" : "Release"
                    ) as ProjectInformationCommandResult;
                    
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                }, TimeSpan.FromMilliseconds(5));
                return result;
            }
            void LoadFromFile()
            {
                try 
                { 
                    config = ConfigFileProvider.LoadFromConfig(projectDirectory);
                }
                catch (Exception e)
                {
                    _ = MessageBox.Show("Project configuration file could not be loaded." + e.Message);
                    config = new ProjectConfiguration();
                }
                LibraryDescription = config.LibraryDescription;
                LibraryVersion = config.LibraryVersion;
                EngineerVersion = config.EngineerVersion;
                LibraryInfos = config.LibraryInfo!= null ? new ObservableCollection<ProjectConfigurationLibraryInfo>(config.LibraryInfo) 
                                                : new ObservableCollection<ProjectConfigurationLibraryInfo>();
                libs = config.ExcludedFiles?.Select(e => new LibViewModel(this, e, selected: true));
            }
        }

        #region Properties

        private string libraryDescription;
        private string libraryVersion;
        private string engineerVersion;
        private string errorText;
        private bool generateNamespaces = true;

        public string LibraryDescription
        {
            get => libraryDescription;
            set
            {
                libraryDescription = value;
                OnPropertyChanged();
            }
        }

        public string LibraryVersion
        {
            get => libraryVersion;
            set
            {
                libraryVersion = value;
                OnPropertyChanged();
            }
        }

        public string EngineerVersion
        {
            get => engineerVersion;
            set
            {
                engineerVersion = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<ProjectConfigurationLibraryInfo> LibraryInfos
        { 
            get; set;
        }

        public string ExcludedFilesLabel => "Excluded Files - checked files will not be added to pcwlx";

        public ObservableCollection<LibViewModel> ExcludedFiles { get; }

        public bool ShowExcludedFiles { get; private set; } = true;
        public bool EnableExcludedFiles { get; private set; } = true;

        public bool ShowGenerateNamespaces { get; private set; } = true;

        public bool GenerateNamespaces
        {
            get => generateNamespaces;
            set
            {
                generateNamespaces = value;
                OnPropertyChanged();
            }
        }

        public SigningViewModel SigningViewModel { get; }

        #endregion
        #region Commands

        public ICommand SaveButtonClickCommand => new DelegateCommand<DialogWindow>(OnSaveButtonClicked);
        public ICommand CancelButtonClickCommand => new DelegateCommand<DialogWindow>(OnCancelButtonClicked);

        public ICommand AddButtonClickCommand => new DelegateCommand(OnAddButtonClicked);
        public ICommand RemoveButtonClickCommand => new DelegateCommand<object>(OnRemoveButtonClicked);
        public ICommand EditButtonClickCommand => new DelegateCommand<ProjectConfigurationLibraryInfo>(OnEditButtonClicked);

        private void OnAddButtonClicked()
        {
            AddItemViewModel viewModel = new AddItemViewModel(LibraryInfos);
            AddItemDialog dialog = new AddItemDialog(viewModel);
            bool? dialogResult = dialog.ShowModal();

            if (dialogResult == true)
            {
                LibraryInfos.Add(new ProjectConfigurationLibraryInfo(){name=viewModel.Key, Value=viewModel.Value});
            }
        }

        private void OnRemoveButtonClicked(object selectedItem)
        {
            if(selectedItem is ProjectConfigurationLibraryInfo libraryInfoItem)
                LibraryInfos.Remove(libraryInfoItem);
        }

        private void OnEditButtonClicked(object selectedObject)
        {
            if (!(selectedObject is ProjectConfigurationLibraryInfo selectedItem))
                return;

            AddItemViewModel viewModel = new AddItemViewModel(LibraryInfos, selectedItem.name, selectedItem.Value);
            AddItemDialog dialog = new AddItemDialog(viewModel);
            bool? dialogResult = dialog.ShowModal();

            if (dialogResult == true 
                && (selectedItem.name != viewModel.Key || selectedItem.Value != viewModel.Value))
            {
                LibraryInfos.Remove(selectedItem);
                LibraryInfos.Add(new ProjectConfigurationLibraryInfo() { name = viewModel.Key, Value = viewModel.Value });
            }
        }

        private void OnCancelButtonClicked(DialogWindow window)
        {
            window.Close();
        }

        private void OnSaveButtonClicked(DialogWindow window)
        {
            if (!string.IsNullOrEmpty(SigningViewModel.PKCS12File) && (!string.IsNullOrEmpty(SigningViewModel.PrivateKeyFile) ||
                                                      !string.IsNullOrEmpty(SigningViewModel.PublicKeyFile) ||
                                                      SigningViewModel.CertificateFiles?.Any() == true))
            {
                string unpersistedValue = SigningViewModel.UsePEMFiles ? "PKCS#12" : "PEM";
                DialogResult result = MessageBox.Show($"The entered {unpersistedValue} file(s) will not be persisted.",
                                                      "Value will not be saved",
                                                      MessageBoxButtons.OKCancel,
                                                      MessageBoxIcon.Information);
                if (result != DialogResult.OK)
                {
                    return;
                }
            }
            ConfigFileProvider.WriteConfigFile(CreateConfigFileContent(), projectDirectory);

            ProjectFileProvider projectProvider = new ProjectFileProvider(projectDirectory);
            projectProvider.SetGenerateNamespaces(GenerateNamespaces);
            projectProvider.WriteProjectFile();

            window.Close();

            ProjectConfiguration CreateConfigFileContent()
            {
                ProjectConfiguration config =  new ProjectConfiguration()
                {
                    LibraryDescription = LibraryDescription,
                    LibraryVersion = LibraryVersion,
                    EngineerVersion = EngineerVersion,
                    LibraryInfo = LibraryInfos.ToArray(),
                    ExcludedFiles = ExcludedFiles.Where(e => e.Selected && e != selectAll)
                                                 .Select(e => e.Name)
                                                 .ToArray(),
                    Sign = SigningViewModel.Sign,
                    Pkcs12 = SigningViewModel.UsePEMFiles ? null : SigningViewModel.PKCS12File,
                    PrivateKey = SigningViewModel.UsePEMFiles ? SigningViewModel.PrivateKeyFile : null,
                    PublicKey = SigningViewModel.UsePEMFiles ? SigningViewModel.PublicKeyFile : null,
                    Certificates = SigningViewModel.UsePEMFiles ? SigningViewModel.CertificateFiles.ToArray() : null,
                    TimestampConfiguration = SigningViewModel.TimestampConfiguration
                    
                };
                if (SigningViewModel.Timestamp)
                {
                    config.Timestamp = true;
                }
                else 
                {
                    config.NoTimestamp = true;
                }
                return config;
            }
        }

        internal void SelectionChanged(LibViewModel element, bool isSelected)
        {
            if (element == selectAll)
            {
                foreach (LibViewModel lib in ExcludedFiles.Where(f => f != selectAll))
                {
                    lib.SetSelected(isSelected);
                }
            }
            else
            {
                if (selectAll.Selected && ExcludedFiles.Any(l => !l.Selected))
                {
                    selectAll.SetSelected(false);
                }
                else if(!selectAll.Selected && ExcludedFiles.Where(l => l != selectAll).All(l => l.Selected))
                {
                    selectAll.SetSelected(true);
                }
            }
        }

        #endregion

        #region IDataErrorInfo
        public string Error => errorText;

        public string this[string columnName]
        {
            get
            {
                if (columnName == nameof(EngineerVersion))
                {
                    if (!ValidateVersion(EngineerVersion))
                    {
                        errorText = "Engineer Version not valid! Please use format: 202x.x or 202x.x.x";
                        OnPropertyChanged(nameof(Error));
                        return errorText;
                    }
                }
                errorText = string.Empty;
                OnPropertyChanged(nameof(Error));
                return errorText;
            }
        }
        private bool ValidateVersion(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return true;
            }

            if (System.Version.TryParse(value, out System.Version version))
            {
                if (version.Major > 2019 && version.Major < 2030)
                {
                    return true;
                }
            }
            return false;
        }
        #endregion

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion        
    }

    public class LibViewModel : INotifyPropertyChanged
    {
        private bool selected;
        private bool invalid;
        private ProjectConfigWindowViewModel parent;

        public LibViewModel(ProjectConfigWindowViewModel parent, string name, bool selected = false, bool invalid = false)
        {
            Name = name;
            this.selected = selected;
            this.invalid = invalid;
            this.parent = parent;
        }

        public bool Selected
        {
            get => selected;
            set
            {
                selected = value;
                OnPropertyChanged();
                parent.SelectionChanged(this, value);
            }
        }

        public string Name { get; }

        public bool Invalid 
        {
            get => invalid;
            private set
            {
                invalid = value;
                OnPropertyChanged();
            }
        }

        public void SetInvalid()
        {
            Invalid = true;
        }

        internal void SetSelected(bool value)
        {
            selected = value;
            OnPropertyChanged(nameof(Selected));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
