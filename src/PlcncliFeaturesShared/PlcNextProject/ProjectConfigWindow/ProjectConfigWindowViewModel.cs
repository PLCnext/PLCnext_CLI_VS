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
using PlcncliServices.CommandResults;
using PlcncliServices.PLCnCLI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using Constants = PlcncliCommonUtils.Constants;
using Path = System.IO.Path;

namespace PlcncliFeatures.PlcNextProject.ProjectConfigWindow
{
    public class ProjectConfigWindowViewModel : INotifyPropertyChanged
    {
        private readonly string configFileName = "PLCnextSettings.xml";
        private readonly string settingElementName = "ProjectConfiguration";
        private readonly string libraryDescriptionElementName = "LibraryDescription";
        private readonly string libraryVersionElementName = "LibraryVersion";
        private readonly string engineerVersionElementName = "EngineerVersion";
        private readonly string excludedFilesElementName = "ExcludedFiles";
        private readonly string fileElementName = "File";
        private readonly string configFilePath;
        private readonly string projectFilePath;
        private readonly string projectFileName = "plcnext.proj";
        private DTE2 dte;
        private readonly LibViewModel selectAll;

        public ProjectConfigWindowViewModel(IPlcncliCommunication plcncliCommunication)
        {
            selectAll = new LibViewModel(this, "Select/Deselect all elements");
            string projectDirectory = GetProjectLocation();
            string GetProjectLocation()
            {
                dte = Package.GetGlobalService(typeof(DTE)) as DTE2;
                ThreadHelper.ThrowIfNotOnUIThread();
                Array selectedItems = dte?.ToolWindows.SolutionExplorer.SelectedItems as Array;
                if (selectedItems == null || selectedItems.Length != 1)
                {
                    return string.Empty;
                }

                if (!((selectedItems.GetValue(0) as UIHierarchyItem)?.Object is Project project))
                {
                    return string.Empty;
                }

                return project.FullName;
            }
            if (string.IsNullOrEmpty(projectDirectory))
            {
                return;
            }

            ProjectInformationCommandResult projectInformation  = GetProjectInformation();
            IEnumerable<string> externalLibs = Enumerable.Empty<string>();
            if (projectInformation != null)
            {
                externalLibs = projectInformation.ExternalLibraries.Select(p => Path.GetFileName(p.PathValue));
                GenerateNamespaces = projectInformation.GenerateNamespaces;
            }

            configFilePath = Path.Combine(Path.GetDirectoryName(projectDirectory), configFileName);
            projectFilePath = Path.Combine(Path.GetDirectoryName(projectDirectory), projectFileName);

            IEnumerable<LibViewModel> libs = null;
            LoadFromFile();
            
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
                ThreadHelper.JoinableTaskFactory.Run(
                "Fetching project information",
                async (progress) =>
                {
                    progress.Report(new ThreadedWaitDialogProgressData("Fetching external libraries..."));

                    result = plcncliCommunication.ExecuteCommand(Constants.Command_get_project_information, null,
                    typeof(ProjectInformationCommandResult), Constants.Option_get_project_information_project, "\""+Path.GetDirectoryName(projectDirectory)+"\"")
                    as ProjectInformationCommandResult;

                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                }, TimeSpan.FromMilliseconds(5));
                return result;
            }
            void LoadFromFile()
            {
                if (File.Exists(configFilePath))
                {
                    try
                    {
                        XDocument document;
                        using (FileStream stream = File.OpenRead(configFilePath))
                        {
                            document = XDocument.Load(stream);
                        }
                        XElement settings = document.Elements().FirstOrDefault();
                        LibraryDescription = settings?.Elements(settings.GetDefaultNamespace() + libraryDescriptionElementName)
                                                     .FirstOrDefault()
                                                     ?.Value;
                        LibraryVersion = settings?.Elements(settings.GetDefaultNamespace() + libraryVersionElementName)
                                                 .FirstOrDefault()
                                                 ?.Value;
                        EngineerVersion = settings?.Elements(settings.GetDefaultNamespace() + engineerVersionElementName)
                                                  .FirstOrDefault()
                                                  ?.Value;
                        libs = settings?.Elements(settings.GetDefaultNamespace() + excludedFilesElementName)
                                .FirstOrDefault()
                                ?.Elements(settings.GetDefaultNamespace() + fileElementName)
                                .Select(e => new LibViewModel(this, e.Value, selected: true));

                    }
                    catch (Exception e)
                    {
                        _ = MessageBox.Show("Project configuration file could not be loaded." + e.Message);
                    }
                }
            }
        }

        #region Properties

        private string libraryDescription;
        private string libraryVersion;
        private string engineerVersion;

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
                if (!CheckVersion(value))
                {
                    SetErrorMessage();
                }
                else
                {
                    ClearErrorMessage();
                }
                engineerVersion = value;
                OnPropertyChanged();
            }
        }
        private void SetErrorMessage()
        {
            ErrorText = "Engineer Version not valid! Please use format: 202x.x or 202x.x.x";
        }

        private void ClearErrorMessage()
        {
            ErrorText = string.Empty;
        }
        private bool CheckVersion(string value)
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

        private string errorText;
        private bool generateNamespaces = true;

        public string ErrorText
        {
            get => errorText;
            private set
            {
                errorText = value;
                OnPropertyChanged();
            }
        }

        public BitmapSource ErrorImage => Imaging.CreateBitmapSourceFromHIcon(SystemIcons.Error.Handle, 
                                                                              Int32Rect.Empty,
                                                                              BitmapSizeOptions.FromEmptyOptions());

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
        #endregion
        #region Commands

        public ICommand SaveButtonClickCommand => new DelegateCommand<DialogWindow>(OnSaveButtonClicked);
        public ICommand CancelButtonClickCommand => new DelegateCommand<DialogWindow>(OnCancelButtonClicked);

        private void OnCancelButtonClicked(DialogWindow window)
        {
            window.Close();
        }

        private void OnSaveButtonClicked(DialogWindow window)
        {

            if (string.IsNullOrEmpty(LibraryDescription)
                && string.IsNullOrEmpty(LibraryVersion)
                && string.IsNullOrEmpty(EngineerVersion)
                && ExcludedFiles.Count < 1)
            {
                if (File.Exists(configFilePath))
                {
                    DeleteFile();
                }
                window.Close();
            }
            else
            {
                WriteFile(CreateFileContent());
                window.Close();
            }

            ProjectFileProvider projectProvider = new ProjectFileProvider(projectFilePath);
            projectProvider.SetGenerateNamespaces(GenerateNamespaces);
            projectProvider.WriteProjectFile();

            void WriteFile(XDocument document)
            {
                using (FileStream stream = File.OpenWrite(configFilePath))
                {
                    stream.SetLength(0);
                    document.Save(stream);
                }
            }
            void DeleteFile()
            {
                File.Delete(configFilePath);
            }

            XDocument CreateFileContent()
            {
                XNamespace xmlns = "http://www.phoenixcontact.com/schema/projectconfiguration";

                return new XDocument(
                    new XElement(xmlns + settingElementName, 
                        new XAttribute(XNamespace.Xmlns + "xsi", "http://www.w3.org/2001/XMLSchema-instance"),
                        new XAttribute(XNamespace.Xmlns + "xsd", "http://www.w3.org/2001/XMLSchema"),
                        new XElement(xmlns + nameof(LibraryDescription), LibraryDescription),
                        new XElement(xmlns + nameof(LibraryVersion), LibraryVersion),
                        new XElement(xmlns + nameof(EngineerVersion), EngineerVersion),
                        new XElement(xmlns + nameof(ExcludedFiles), ExcludedFiles.Where(e => e.Selected && e != selectAll)
                                                                .Select(e => new XElement(xmlns + "File",e.Name))
                                                                .ToArray())
                        )
                    );
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
