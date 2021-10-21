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
using System;
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

namespace PlcNextVSExtension.PlcNextProject.ProjectConfigWindow
{
    public class ProjectConfigWindowViewModel : INotifyPropertyChanged
    {
        private readonly string configFileName = "PLCnextSettings.xml";
        private readonly string settingElementName = "ProjectConfiguration";
        private readonly string libraryDescriptionElementName = "LibraryDescription";
        private readonly string libraryVersionElementName = "LibraryVersion";
        private readonly string engineerVersionElementName = "EngineerVersion";
        private readonly string configFilePath;
        private DTE2 dte;

        public ProjectConfigWindowViewModel()
        {
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
            configFilePath = Path.Combine(Path.GetDirectoryName(projectDirectory), configFileName);
            LoadFromFile();
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
            ErrorText = "No valid version! Please use format: 202x.x or 202x.x.x";
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
                if (version.Major > 2019)
                {
                    return true;
                }
            }
            return false;
        }

        private string errorText;
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
                && string.IsNullOrEmpty(EngineerVersion))
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
                        new XElement(xmlns + nameof(EngineerVersion), EngineerVersion)
                        )
                    );
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
}
