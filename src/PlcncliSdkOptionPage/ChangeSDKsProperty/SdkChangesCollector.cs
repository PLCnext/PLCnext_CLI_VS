#region Copyright
///////////////////////////////////////////////////////////////////////////////
//
//  Copyright (c) Phoenix Contact GmbH & Co KG
//  This software is licensed under Apache-2.0
//
///////////////////////////////////////////////////////////////////////////////
#endregion

using System.Collections.ObjectModel;
using System.Linq;

namespace PlcncliSdkOptionPage.ChangeSDKsProperty
{
    public class SdkChangesCollector : ISdkChangesCollector
    {
        public ObservableCollection<string> SdksToAdd { get; } = new ObservableCollection<string>();

        public ObservableCollection<string> SdksToRemove { get; } = new ObservableCollection<string>();

        public ObservableCollection<InstallSdk> SdksToInstall { get; } = new ObservableCollection<InstallSdk>();

        public void AddSdk(string sdk)
        {
            if(SdksToRemove.Remove(sdk))
                return;

            SdksToAdd.Add(sdk);
        }

        public void InstallSdk(string archiveFile, string destination, bool force)
        {
            SdksToInstall.Add(new InstallSdk(archiveFile, destination, force));
        }

        public void RemoveSdk(string sdk)
        {
            InstallSdk element = SdksToInstall.FirstOrDefault(s => s.Destination.Equals(sdk));
            if(element != null)
            {
                SdksToInstall.Remove(element);
                return;
            }
                
            if (SdksToAdd.Remove(sdk))
                return;
            SdksToRemove.Add(sdk);
        }
    }
}
