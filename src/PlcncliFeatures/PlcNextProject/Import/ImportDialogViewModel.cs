#region Copyright
///////////////////////////////////////////////////////////////////////////////
//
//  Copyright (c) Phoenix Contact GmbH & Co KG
//  This software is licensed under Apache-2.0
//
///////////////////////////////////////////////////////////////////////////////
#endregion

using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Microsoft.VisualStudio.PlatformUI;

namespace PlcncliFeatures.PlcNextProject.Import
{
    public class ImportDialogViewModel : INotifyPropertyChanged
    {
        private readonly ImportDialogModel _model;
        private string projectFilePath = string.Empty;
        private string errorText = string.Empty;
        private readonly string errorEmptyFile = "PLCnCLI project file cannot be empty";
        private readonly string errorPathNotExist = "{0} is not a valid path to a PLCnCLI project file.";

        public ImportDialogViewModel(ImportDialogModel model)
        {
            _model = model;

        }

        #region Properties
        public string Description { get; } = "Select .proj file of the PLCnCLI project to be imported.";

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

        public string ProjectFilePath
        {
            get => projectFilePath;
            set
            {
                projectFilePath = value;
                OnPropertyChanged();
                ValidateContents();
            }
        }
        #endregion

        private void ValidateContents()
        {
            if (string.IsNullOrEmpty(projectFilePath))
            {
                ErrorText = errorEmptyFile;
                return;
            }
            else
            {
                if (!File.Exists(projectFilePath) || !Path.GetExtension(projectFilePath).Equals(".proj"))
                {
                    ErrorText = string.Format(errorPathNotExist, projectFilePath);
                    return;
                }
            }

            ErrorText = string.Empty;
        }

        #region Commands
        public ICommand OkButtonClickCommand => new DelegateCommand<Window>(OnOkButtonClicked);
        public ICommand CancelButtonClickCommand => new DelegateCommand<Window>(OnCancelButtonClicked);
        public ICommand BrowseFileCommand => new DelegateCommand(OnBrowseFileButtonClicked);


        private void OnOkButtonClicked(Window window)
        {
            _model.ProjectFilePath = ProjectFilePath;
            window.DialogResult = true;
            window.Close();
        }

        private void OnCancelButtonClicked(Window window)
        {
            window.DialogResult = false;
            window.Close();
        }


        private void OnBrowseFileButtonClicked()
        {
            OpenFileDialog fileDialog = new OpenFileDialog
            {
                Title = "Select PLCnCLI project file",
                InitialDirectory = ProjectFilePath,
                DefaultExt = "proj"
            };
            DialogResult result = fileDialog.ShowDialog();
            if (result == DialogResult.OK)
                ProjectFilePath = fileDialog.FileName;
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
