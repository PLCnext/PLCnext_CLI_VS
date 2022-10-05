#region Copyright
///////////////////////////////////////////////////////////////////////////////
//
//  Copyright (c) Phoenix Contact GmbH & Co KG
//  This software is licensed under Apache-2.0
//
///////////////////////////////////////////////////////////////////////////////
#endregion

using System.Collections.ObjectModel;

namespace PlcncliFeatures.ChangeSDKsProperty
{
    public interface ISdkChangesCollector
    {
        ObservableCollection<string> SdksToAdd { get; }
        ObservableCollection<string> SdksToRemove { get; }
        ObservableCollection<InstallSdk> SdksToInstall { get; }

        void AddSdk(string sdk);

        void RemoveSdk(string sdk);

        void InstallSdk(string archiveFile, string destination, bool force);
    }

    public class InstallSdk
    {
        public InstallSdk(string archiveFile, string destination, bool force)
        {
            ArchiveFile = archiveFile;
            Destination = destination;
            Force = force;
        }

        public string ArchiveFile { get; }
        public string Destination { get; }
        public bool Force { get; }
    }
}
