#region Copyright
///////////////////////////////////////////////////////////////////////////////
//
//  Copyright (c) Phoenix Contact GmbH & Co KG
//  This software is licensed under Apache-2.0
//
///////////////////////////////////////////////////////////////////////////////
#endregion

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System;

namespace PlcncliFeatures.ChangeSDKsProperty
{
    public class TargetViewModel : INotifyPropertyChanged
    {
        public TargetViewModel(string displayName, SdkViewModel parent)
        {
            DisplayName = displayName;
            Parent = parent;
            parent.PropertyChanged += Parent_PropertyChanged;
        }

        private void Parent_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged(e.PropertyName);
        }

        private SdkViewModel Parent { get; }

        public SdkState SdkState => Parent.SdkState;

        public bool IsSelected
        {
            get
            {
                return Parent.IsSelected;
            }
            set
            {
                Parent.IsSelected = value;
            }
        }

        public string DisplayName { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
