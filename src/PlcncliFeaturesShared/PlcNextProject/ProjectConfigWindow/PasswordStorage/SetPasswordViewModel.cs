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
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace PlcncliFeatures.PlcNextProject.ProjectConfigWindow
{
    internal class SetPasswordViewModel : INotifyPropertyChanged
    {
        private string password = string.Empty;
        private bool showPassword;

        public SetPasswordViewModel(string password = null)
        {
            ShowPassword = false;
            if (password != null)
            {
                Password = password;
                OnPropertyChanged(nameof(Password));
            }
        }

        #region Properties
        public string Password
        {
            get => password; 
            set
            {
                password = value;
                OnPropertyChanged();
            }
        }

        public bool ShowPassword
        {
            get => showPassword; 
            set
            {
                showPassword = value;
                OnPropertyChanged();
            }
        }
        #endregion

        #region Commands

        public ICommand SaveCommand => new DelegateCommand<Window>(OnSaveButtonClicked);

        private void OnSaveButtonClicked(Window window)
        {
            window.DialogResult = true;
            window.Close();
        }

        public ICommand CancelCommand => new DelegateCommand<Window>(OnCancelButtonClicked);

        private void OnCancelButtonClicked(Window window)
        {
            window.Close();
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
