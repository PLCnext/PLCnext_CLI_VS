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
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace PlcncliFeatures.PlcNextProject.ProjectConfigWindow
{
    internal class AddItemViewModel : INotifyPropertyChanged
    {
        private string key;
        private readonly string initialKey;
        private string value;
        private readonly IEnumerable<ProjectConfigurationLibraryInfo> libraryInfos;

        public AddItemViewModel(IEnumerable<ProjectConfigurationLibraryInfo> libraryInfos, string key, string value)
        {
            WindowTitle = "Edit Library Info";
            Description = "Edit key value pair of library infos.";

            this.libraryInfos = libraryInfos;
            initialKey = key;
            this.Key = key?? string.Empty;
            this.Value = value?? string.Empty;
        }

        public AddItemViewModel(IEnumerable<ProjectConfigurationLibraryInfo> libraryInfos)
        {
            this.libraryInfos = libraryInfos;
            WindowTitle = "Add Library Info";
            Description = "Add key value pair to list of library infos.";
        }

        #region Properties
        public string Key
        {
            get => key; set
            {
                key = value;
                OnPropertyChanged();
            }
        }
        public string Value
        {
            get => value; set
            {
                this.value = value;
                OnPropertyChanged();
            }
        }

        public string WindowTitle { get; }
        public string Description { get; }
        #endregion

        #region Commands

        public ICommand OKCommand => new DelegateCommand<Window>(OnOKButtonClicked);

        private void OnOKButtonClicked(Window window)
        {
            if(((!string.IsNullOrEmpty(initialKey) && Key != initialKey) || (string.IsNullOrEmpty(initialKey))) &&
                libraryInfos.Where(info => info.name.Equals(Key, StringComparison.OrdinalIgnoreCase)).Any())
            {
                MessageBox.Show($"Key {Key} is already available in Library Infos.", "Duplicate Key", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

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
