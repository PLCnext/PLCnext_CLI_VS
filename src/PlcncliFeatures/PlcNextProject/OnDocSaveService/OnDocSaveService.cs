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
using Task = System.Threading.Tasks.Task;

namespace PlcncliFeatures.PlcNextProject.OnDocSaveService
{
    public class OnDocSaveService
    {
        public async Task InitializeAsync(AsyncPackage package)
        {
            await package.JoinableTaskFactory.SwitchToMainThreadAsync();
            if (await package.GetServiceAsync(typeof(SVsRunningDocumentTable)) is IVsRunningDocumentTable rdt)
            {
                rdt.AdviseRunningDocTableEvents(new SaveCMakeListsEventHandler(package, rdt), out _);
            }
        }
    }
}
