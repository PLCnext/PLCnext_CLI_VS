#region Copyright
///////////////////////////////////////////////////////////////////////////////
//
//  Copyright (c) Phoenix Contact GmbH & Co KG
//  This software is licensed under Apache-2.0
//
///////////////////////////////////////////////////////////////////////////////
#endregion

using Microsoft.VisualStudio.PlatformUI;
using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace PlcncliSdkOptionPage.ChangeSDKsProperty
{
    public class InstallSdkViewModel : INotifyPropertyChanged
    {
        private string archiveFilePath;
        private string sdkDestination;
        private string errorText;
        private readonly string errorNoDestination = "No destination selected.";
        private readonly string errorFileNotExist = "The file {0} does not exist.";
        private readonly string errorNoFile = "No file selected.";
        private readonly string errorPathNotValid = "The path {0} is not a valid path.";


        #region Properties
        public string ArchiveFilePath
        {
            get => archiveFilePath;
            set
            {
                archiveFilePath = value;
                OnPropertyChanged();
                ValidatePath();
            }
        }

        public string SdkDestination
        {
            get => sdkDestination;
            set
            {
                sdkDestination = value;
                OnPropertyChanged();
                ValidatePath();
            }
        }

        public bool Force { get; set; }

        public string ErrorText { get => errorText;
            private set
            {
                errorText = value;
                OnPropertyChanged();
            }
        }
        public BitmapSource ErrorImage => Imaging.CreateBitmapSourceFromHIcon(SystemIcons.Error.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
        #endregion

        private void ValidatePath()
        {
            if (string.IsNullOrEmpty(archiveFilePath))
            {
                ErrorText = errorNoFile;
                return;
            }
            if (!File.Exists(archiveFilePath))
            {
                ErrorText = string.Format(errorFileNotExist, archiveFilePath);
                return;
            }
            if (string.IsNullOrEmpty(sdkDestination))
            {
                ErrorText = errorNoDestination;
                return;
            }

            try
            {
                Path.GetFullPath(sdkDestination);
                if (!Path.IsPathRooted(sdkDestination))
                {
                    ErrorText = string.Format(errorPathNotValid, sdkDestination);
                    return;
                }
            }
            catch (Exception)
            {
                ErrorText = string.Format(errorPathNotValid, sdkDestination);
                return;
            }
            ErrorText = "";
        }

        #region Commands

        public ICommand OKCommand => new DelegateCommand<Window>(OnOKButtonClicked);

        private void OnOKButtonClicked(Window window)
        {
            window.DialogResult = true;
            window.Close();
        }

        public ICommand CancelCommand => new DelegateCommand<Window>(OnCancelButtonClicked);

        private void OnCancelButtonClicked(Window window)
        {
            window.Close();
        }

        public ICommand BrowseFileCommand => new DelegateCommand(OnBrowseFileButtonClicked);

        private void OnBrowseFileButtonClicked()
        {
            OpenFileDialog fileDialog = new OpenFileDialog
            {
                InitialDirectory = ArchiveFilePath,
                DefaultExt = "tar.xz"
            };
            DialogResult result = fileDialog.ShowDialog();
            if (result == DialogResult.OK)
                ArchiveFilePath = fileDialog.FileName;
        }

        public ICommand BrowseDirCommand => new DelegateCommand(OnBrowseDirButtonClicked);

        private void OnBrowseDirButtonClicked()
        {
            FolderBrowserDialog folderDialog = new FolderBrowserDialog
            {
                SelectedPath = SdkDestination
            };
            folderDialog.ShowDialog();
            SdkDestination = folderDialog.SelectedPath;
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