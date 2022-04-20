#region Copyright
///////////////////////////////////////////////////////////////////////////////
//
//  Copyright (c) Phoenix Contact GmbH & Co KG
//  This software is licensed under Apache-2.0
//
///////////////////////////////////////////////////////////////////////////////
#endregion

using Microsoft.VisualStudio.PlatformUI;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace PlcncliFeatures.ChangeSDKsProperty
{
    internal class AddSdkViewModel : INotifyPropertyChanged
    {
        private readonly string errorNoDirectory = "No directory selected.";
        private readonly string errorDirectoryNotExist = "The directory {0} does not exist.";
        private string sdkRootPath = string.Empty;
        private string errorText = string.Empty;


        #region Properties


        public string SdkRootPath
        {
            get => sdkRootPath; set
            {
                sdkRootPath = value;
                OnPropertyChanged();
                if (!string.IsNullOrEmpty(sdkRootPath))
                {
                    DirectoryInfo fileInfo = new DirectoryInfo(sdkRootPath);
                    if (fileInfo.Exists)
                    {
                        ErrorText = string.Empty;
                        return;
                    }
                    ErrorText = string.Format(errorDirectoryNotExist, sdkRootPath);
                    return;
                }
                ErrorText = errorNoDirectory;
            }
        }

        public string ErrorText
        {
            get => errorText;
            private set
            {
                errorText = value;
                OnPropertyChanged();
            }
        }

        public BitmapSource ErrorImage => Imaging.CreateBitmapSourceFromHIcon(SystemIcons.Error.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
        #endregion

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

        public ICommand BrowseCommand => new DelegateCommand(OnBrowseButtonClicked);

        private void OnBrowseButtonClicked()
        {
            FolderBrowserDialog folderDialog = new FolderBrowserDialog
            {
                SelectedPath = SdkRootPath
            };
            folderDialog.ShowDialog();
            SdkRootPath = folderDialog.SelectedPath;
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
