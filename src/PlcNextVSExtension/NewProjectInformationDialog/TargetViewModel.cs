#region Copyright
///////////////////////////////////////////////////////////////////////////////
//
//  Copyright (c) Phoenix Contact GmbH & Co KG
//  This software is licensed under Apache-2.0
//
///////////////////////////////////////////////////////////////////////////////
#endregion

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using PlcNextVSExtension.CommandResults;

namespace PlcNextVSExtension.NewProjectInformationDialog
{
    public class TargetViewModel : INotifyPropertyChanged
    {
        private bool _selected = false;

        public TargetViewModel(string displayName, TargetResult source)
        {
            DisplayName = displayName;
            Source = source;
            Name = source.Name;
            Version = source.LongVersion;
        }

        public bool Selected
        {
            get => _selected;
            set
            {
                _selected = value;
                OnPropertyChanged();
            }
        }

        public string DisplayName { get; }

        public string Name { get; }

        public string Version { get; }

        public TargetResult Source { get; }
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public static class TargetViewModelCollectionExtension
    {
        public static void AddIfNotExists(this ObservableCollection<TargetViewModel> collection, TargetResult target)
        {
            if (!collection.Select(t => t.DisplayName).Contains(target.GetDisplayName()))
                collection.Add(new TargetViewModel(target.GetDisplayName(), target));
        }
    }
}
