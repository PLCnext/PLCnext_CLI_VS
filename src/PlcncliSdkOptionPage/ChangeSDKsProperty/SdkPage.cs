#region Copyright
///////////////////////////////////////////////////////////////////////////////
//
//  Copyright (c) Phoenix Contact GmbH & Co KG
//  This software is licensed under Apache-2.0
//
///////////////////////////////////////////////////////////////////////////////
#endregion

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using PlcncliServices.PLCnCLI;
using System;
using System.IO;
using System.Windows;

namespace PlcncliSdkOptionPage.ChangeSDKsProperty
{
    public class SdkPage
    {
        private readonly SDKPageModel model;
        private readonly SDKPageViewModel viewModel;
        private readonly IPlcncliCommunication plcncliCommunication;

        public SdkPage()
        {
            
            try
            {
                plcncliCommunication = Package.GetGlobalService(typeof(SPlcncliCommunication)) as IPlcncliCommunication;
                model = new SDKPageModel(plcncliCommunication);
                viewModel = new SDKPageViewModel(model);
                PageControl = new SDKPageControl(viewModel);
            }catch(Exception e)
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                IVsActivityLog log = Package.GetGlobalService(typeof(SVsActivityLog)) as IVsActivityLog;
                log.LogEntry((uint)__ACTIVITYLOG_ENTRYTYPE.ALE_ERROR, this.ToString(),
                    "An error occurred while creating the sdk page: " + e.Message);
            }
        }

        public SDKPageControl PageControl { get; }

        public void ReinitializeControl()
        {
            try
            {
                model.Initialize();
                viewModel.Initialize();
            }
            catch (Exception e)
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                IVsActivityLog log = Package.GetGlobalService(typeof(SVsActivityLog)) as IVsActivityLog;
                log.LogEntry((uint)__ACTIVITYLOG_ENTRYTYPE.ALE_ERROR, this.ToString(),
                    "An error occurred while loading the sdk page: " + e.Message);
            }
        }

        public void ApplyChanges()
        {
            if (model.SdkChangesCollector.SdksToRemove.Count > 0)
            {
                RemoveSdkViewModel dialogVM = new RemoveSdkViewModel(model.SdkChangesCollector.SdksToRemove);
                RemoveSdkDialog dialog = new RemoveSdkDialog(dialogVM);
                bool? result = dialog.ShowModal();
                if(result == null || result == false)
                {
                    return;
                }
                if(dialogVM.RemoveFromDisk)
                {
                    foreach (string path in model.SdkChangesCollector.SdksToRemove)
                    {
                        Directory.Delete(path, true);
                    }
                }
            }
            try
            {
                foreach (string sdk in model.SdkChangesCollector.SdksToRemove)
                {
                    plcncliCommunication.ExecuteCommand("set setting", null, null, "-r", "SdkPaths", $"\"{sdk}\"");
                }
                foreach (string sdk in model.SdkChangesCollector.SdksToAdd)
                {
                    plcncliCommunication.ExecuteCommand("set setting", null, null, "-a", "SdkPaths", $"\"{sdk}\"");
                }
                foreach (InstallSdk sdk in model.SdkChangesCollector.SdksToInstall)
                {
                    plcncliCommunication.ExecuteCommand("install sdk", null, null, "--path", $"\"{sdk.ArchiveFile}\"",
                        "--destination", $"\"{sdk.Destination}\"", sdk.Force ? "--force" : "");
                }
            }
            catch(PlcncliException e)
            {
                MessageBox.Show(e.Message, "PLCnCLI error");
            }
        }
    }
}
