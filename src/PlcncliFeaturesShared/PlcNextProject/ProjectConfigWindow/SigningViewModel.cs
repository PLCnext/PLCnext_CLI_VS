#region Copyright
///////////////////////////////////////////////////////////////////////////////
//
//  Copyright (c) Phoenix Contact GmbH & Co KG
//  This software is licensed under Apache-2.0
//
///////////////////////////////////////////////////////////////////////////////
#endregion

using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.VisualStudio.Shell.Interop;
using EnvDTE;
using EnvDTE80;

namespace PlcncliFeatures.PlcNextProject.ProjectConfigWindow
{
    public class SigningViewModel : INotifyPropertyChanged, IDataErrorInfo
    {
        private readonly string projectName;

        private readonly IVsSolutionPersistence solutionPersistence;

        public SigningViewModel(ProjectConfiguration config, string projectName)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            this.projectName = projectName;

            DTE2 dte = Package.GetGlobalService(typeof(DTE)) as DTE2;
            solutionPersistence = Package.GetGlobalService(typeof(SVsSolutionPersistence)) as IVsSolutionPersistence;

            Sign = config.Sign;
            PKCS12File = config.Pkcs12;
            PrivateKeyFile = config.PrivateKey;
            PublicKeyFile = config.PublicKey;
            if (config.Timestamp && config.NoTimestamp)
            {
                throw new ArgumentException("Invalid configuration: Timestamp and NoTimestamp cannot be combined.");
            }
            Timestamp = config.Timestamp;
            TimestampConfiguration = config.TimestampConfiguration;
            foreach (string item in config.Certificates ?? Enumerable.Empty<string>())
            {
                CertificateFiles.Add(item);
            }

            UsePEMFiles = !string.IsNullOrEmpty(PrivateKeyFile)
                          || !string.IsNullOrEmpty(PublicKeyFile)
                          || CertificateFiles.Count > 0;
            this.projectName = projectName;
        }

        #region Properties

        private string privateKeyFile;
        private string pKCS12File;
        private string publicKeyFile;
        private string timestampConfiguration;
        private bool usePEMFiles;
        private bool timestamp;

        public bool Sign { get; set; }

        public string PKCS12File
        {
            get => pKCS12File; set
            {
                pKCS12File = value;
                OnPropertyChanged();
            }
        }
        public string PrivateKeyFile
        {
            get => privateKeyFile; set
            {
                privateKeyFile = value;
                OnPropertyChanged();
            }
        }
        public string PublicKeyFile
        {
            get => publicKeyFile; set
            {
                publicKeyFile = value;
                OnPropertyChanged();
            }
        }
        public ObservableCollection<string> CertificateFiles { get; } = new ObservableCollection<string>();
        public string TimestampConfiguration
        {
            get => timestampConfiguration;
            set
            {
                timestampConfiguration = value;
                OnPropertyChanged();
            }
        }

        public bool Timestamp
        {
            get => timestamp;
            set
            {
                timestamp = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TimestampConfiguration));
            }
        }

        public bool UsePEMFiles
        {
            get => usePEMFiles;
            set
            {
                usePEMFiles = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region IDataErrorInfo
        private string errorText;
        public string Error => errorText;

        public string this[string columnName]
        {
            get
            {
                if (columnName == nameof(Timestamp) || columnName == nameof(TimestampConfiguration))
                {
                    if (Timestamp && string.IsNullOrEmpty(TimestampConfiguration))
                    {
                        errorText = "TimestampConfiguration must be provided if timestamp is requested";
                        OnPropertyChanged(nameof(Error));
                        return errorText;
                    }
                }
                errorText = string.Empty;
                OnPropertyChanged(nameof(Error));
                return errorText;
            }
        }

        #endregion

        #region Commands

        public ICommand BrowseCommand => new DelegateCommand<string>(OnBrowseButtonClicked);
        public ICommand DeleteCommand => new DelegateCommand<string>(OnDeleteButtonClicked);
        public ICommand SetPWCommand => new DelegateCommand<PasswordPersistFileType>(OnPasswordButtonClicked);

        private void OnPasswordButtonClicked(PasswordPersistFileType fileType)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            PasswordService passwordService = new PasswordService(solutionPersistence);
            passwordService.ProvidePasswordPersistence(projectName, fileType);
        }

        private void OnDeleteButtonClicked(string itemToDelete)
        {
            CertificateFiles.Remove(itemToDelete);
            OnPropertyChanged();
        }
        private void OnBrowseButtonClicked(string propertyName)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            switch (propertyName)
            {
                case nameof(PKCS12File):
                    fileDialog.Filter = "PKCS#12 container|*.p12;*.pfx|All files|*.*";
                    fileDialog.InitialDirectory = PKCS12File;
                    break;
                case nameof(PrivateKeyFile):
                    fileDialog.Filter = "Privacy-Enhanced Mail (PEM)|*.pem;*.cer;*.crt;*.key|All files|*.*";
                    fileDialog.InitialDirectory = PrivateKeyFile;
                    break;
                case nameof(PublicKeyFile):
                    fileDialog.Filter = "Privacy-Enhanced Mail (PEM)|*.pem;*.cer;*.crt;*.key|All files|*.*";
                    fileDialog.InitialDirectory = PublicKeyFile;
                    break;
                case nameof(CertificateFiles):
                    fileDialog.Filter = "Privacy-Enhanced Mail (PEM)|*.pem;*.cer;*.crt;*.key|All files|*.*";
                    fileDialog.Multiselect = true;
                    break;
                case nameof(TimestampConfiguration):
                    fileDialog.Filter = "JSON|*.json";
                    fileDialog.InitialDirectory = TimestampConfiguration;
                    break;
                default:
                    break;
            }

            bool? result = fileDialog.ShowDialog();
            if (result == true)
            {
                switch (propertyName)
                {
                    case nameof(PKCS12File):
                        PKCS12File = fileDialog.FileName;
                        break;
                    case nameof(PrivateKeyFile):
                        PrivateKeyFile = fileDialog.FileName;
                        break;
                    case nameof(PublicKeyFile):
                        PublicKeyFile = fileDialog.FileName;
                        break;
                    case nameof(CertificateFiles):
                        foreach (string item in fileDialog.FileNames)
                        {
                            CertificateFiles.Add(item);
                            OnPropertyChanged(nameof(CertificateFiles));
                        }
                        break;
                    case nameof(TimestampConfiguration):
                        TimestampConfiguration = fileDialog.FileName;
                        break;
                    default:
                        break;
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
}
