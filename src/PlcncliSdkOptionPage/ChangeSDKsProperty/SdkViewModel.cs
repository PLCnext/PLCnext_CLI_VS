#region Copyright
///////////////////////////////////////////////////////////////////////////////
//
//  Copyright (c) Phoenix Contact GmbH & Co KG
//  This software is licensed under Apache-2.0
//
///////////////////////////////////////////////////////////////////////////////
#endregion

using PlcncliServices.CommandResults;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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

        public SdkViewModel(string path, IEnumerable<TargetResult> targets, SdkState sdkState = SdkState.unchanged)
        {
            Path = path;
            SdkState = sdkState;
            Targets = targets?.Select(target => new TargetViewModel(target.GetDisplayName(), this));
        }

        #region Properties
        public SDKPageViewModel ParentViewModel { get; set; }
        public string Path { get; }

        public IEnumerable<TargetViewModel> Targets { get; }

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

        public string Tooltip
        {
            get => tooltip;
            set
            {
                tooltip = value;
                OnPropertyChanged();
            }
        }
        #endregion

        #region private methods
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
