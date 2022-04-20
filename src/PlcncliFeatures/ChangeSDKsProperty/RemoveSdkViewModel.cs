#region Copyright
///////////////////////////////////////////////////////////////////////////////
//
//  Copyright (c) Phoenix Contact GmbH & Co KG
//  This software is licensed under Apache-2.0
//
///////////////////////////////////////////////////////////////////////////////
#endregion

using Microsoft.VisualStudio.PlatformUI;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace PlcncliFeatures.ChangeSDKsProperty
{
    public class RemoveSdkViewModel
    {
        private readonly string rawDialogMessage = 
            "You are about to remove the SDK{0}\n{1}\nfrom your list of installed SDKs.\nPlease decide if you also want to delete the SDK directory from disk.";
        public RemoveSdkViewModel(IEnumerable<string> sdksToRemove)
        {
            DialogMessage = string.Format(rawDialogMessage, sdksToRemove.Count() >1?"s":string.Empty, string.Join("\n", sdksToRemove));
        }

        public string DialogMessage { get; }

        public bool RemoveFromDisk { get; set; } = false;

        public BitmapSource QuestionImage => Imaging.CreateBitmapSourceFromHIcon(SystemIcons.Question.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

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
            window.DialogResult = false;
            window.Close();
        }
        #endregion
    }
}
