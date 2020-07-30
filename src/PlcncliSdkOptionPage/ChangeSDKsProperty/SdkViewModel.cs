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

namespace PlcncliSdkOptionPage.ChangeSDKsProperty
{
    public enum SdkState
    {
        unchanged,
        removed,
        added,
        installed
    }

    public class SdkViewModel : INotifyPropertyChanged
    {
        private bool isSelected;
        private SdkState sdkState;
        private string tooltip;

        public SDKPageViewModel ParentViewModel { get; set; }

        public SdkViewModel(string path, SdkState sdkState = SdkState.unchanged)
        {
            Path = path;
            SdkState = sdkState;
        }

        public string Path { get; }

        public bool IsSelected
        {
            get => isSelected;
            set
            {
                if (value != isSelected)
                {
                    isSelected = value;
                    OnPropertyChanged();
                    if (ParentViewModel != null)
                        ParentViewModel.CheckSelectedState();
                }
            }
        }

        public SdkState SdkState
        {
            get => sdkState;
            set
            {
                sdkState = value;
                OnPropertyChanged();

                SetToolTip(value);
            }
        }

        private void SetToolTip(SdkState state)
        {
            switch (state)
            {
                case SdkState.unchanged:
                    Tooltip = string.Empty;
                    break;
                case SdkState.removed:
                    Tooltip = "This sdk will be removed when 'OK' is pressed.";
                    break;
                case SdkState.added:
                    Tooltip = "This sdk will be added when 'OK' is pressed.";
                    break;
                case SdkState.installed:
                    Tooltip = "This sdk will be installed when 'OK' is pressed.";
                    break;
                default:

                    Tooltip = string.Empty;
                    break;
            }
        }

        public string Tooltip
        {
            get => tooltip;
            set
            {
                tooltip = value;
                OnPropertyChanged();
            }
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
