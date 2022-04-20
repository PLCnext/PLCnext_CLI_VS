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

namespace PlcncliFeatures.GeneratePortComment
{
    public class PortAttributeViewModel : INotifyPropertyChanged
    {
        private bool selected = false;

        public PortAttributeViewModel(string label, string description)
        {
            Label = label;
            Description = description;
        }
        public string Label { get; }
        public bool Selected
        {
            get => selected;
            set
            {
                selected = value;
                OnPropertyChanged();
            }
        }

        public string Description { get; }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
