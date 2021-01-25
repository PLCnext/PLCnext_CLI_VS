#region Copyright
///////////////////////////////////////////////////////////////////////////////
//
//  Copyright (c) Phoenix Contact GmbH & Co KG
//  This software is licensed under Apache-2.0
//
///////////////////////////////////////////////////////////////////////////////
#endregion

using Microsoft.VisualStudio.PlatformUI;
using System.Drawing;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace PlcNextVSExtension.PlcNextProject.OnDocSaveService
{
    public class UpdateIncludesViewModel
    {
        public UpdateIncludesViewModel(string name)
        {
            Name = name;
            Message = $"CMakeLists.txt of project {Name} has changed, the project's includes might need an update.\n"
                                        + "Selecting 'OK' will check and update the includes while saving the file.\n"
                                        + "Selecting 'Cancel' will perform no check and update while saving the file.";
        }

        public string Name { get; }

        public bool RememberDecision { get; set; } = false;

        public string Message { get; }
        public BitmapSource QuestionImage => Imaging.CreateBitmapSourceFromHIcon(SystemIcons.Question.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

        #region Commands

        public ICommand OkButtonClickCommand => new DelegateCommand<Window>(OnOkButtonClicked);
        public ICommand CancelButtonClickCommand => new DelegateCommand<Window>(OnCancelButtonClicked);


        private void OnOkButtonClicked(Window window)
        {
            window.DialogResult = true;
            window.Close();
        }

        private void OnCancelButtonClicked(Window window)
        {
            window.DialogResult = false;
            window.Close();
        }
        #endregion
    }
}
