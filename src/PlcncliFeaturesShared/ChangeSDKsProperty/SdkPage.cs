﻿#region Copyright
///////////////////////////////////////////////////////////////////////////////
//
//  Copyright (c) Phoenix Contact GmbH & Co KG
//  This software is licensed under Apache-2.0
//
///////////////////////////////////////////////////////////////////////////////
#endregion

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TaskStatusCenter;
using PlcncliServices;
using PlcncliServices.PLCnCLI;
using System;
using System.IO;
using System.Windows;
using Task = System.Threading.Tasks.Task;

namespace PlcncliFeatures.ChangeSDKsProperty
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
            }
            catch (Exception e)
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
                if (ThreadHelper.CheckAccess())
                {
#pragma warning disable VSTHRD010 // Invoke single-threaded types on Main thread
                    IVsActivityLog log = Package.GetGlobalService(typeof(SVsActivityLog)) as IVsActivityLog;
                    log.LogEntry((uint)__ACTIVITYLOG_ENTRYTYPE.ALE_ERROR, this.ToString(),
                        "An error occurred while loading the sdk page: " + e.Message);
#pragma warning restore VSTHRD010 // Invoke single-threaded types on Main thread
                }
            }
        }

        public void ApplyChanges()
        {
            IVsTaskStatusCenterService taskCenter = Package.GetGlobalService(typeof(SVsTaskStatusCenterService)) as IVsTaskStatusCenterService;
            ITaskHandler taskHandler = taskCenter.PreRegister(
                new TaskHandlerOptions() { Title = "Updating sdks..." },
                new TaskProgressData());

            if (model.SdkChangesCollector.SdksToRemove.Count > 0)
            {
                RemoveSdkViewModel dialogVM = new RemoveSdkViewModel(model.SdkChangesCollector.SdksToRemove);
                RemoveSdkDialog dialog = new RemoveSdkDialog(dialogVM);
                bool? result = dialog.ShowModal();
                if (result == null || result == false)
                {
                    return;
                }
                if (dialogVM.RemoveFromDisk)
                {
                    foreach (string path in model.SdkChangesCollector.SdksToRemove)
                    {
                        try
                        {
                            Directory.Delete(path, true);
                        }
                        catch (DirectoryNotFoundException)
                        {
                            // do nothing
                        }
                        catch (Exception e)
                        {
                            MessageBox.Show(e.Message, "Error while trying to delete directory", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }

            taskHandler.RegisterTask(Task.Run(async () =>
            {
                foreach (string sdk in model.SdkChangesCollector.SdksToRemove)
                {
                    ITaskHandler subTaskHandler = taskCenter.PreRegister(
                        new TaskHandlerOptions() { Title = $"Removing sdk {sdk}" },
                        new TaskProgressData());

                    Task task = Task.Run(() =>
                    {
                        try
                        {
                            plcncliCommunication.ExecuteCommand("set setting", null, null, "-r", "SdkPaths", $"\"{sdk}\"");
                        }
                        catch (PlcncliException e)
                        {
                            ShowMessage(e.Message, $"{NamingConstants.ToolName} error");
                        }
                    });
                    subTaskHandler.RegisterTask(task);
                    await task;
                }

                foreach (string sdk in model.SdkChangesCollector.SdksToAdd)
                {
                    ITaskHandler subTaskHandler = taskCenter.PreRegister(
                        new TaskHandlerOptions() { Title = $"Adding sdk {sdk}" },
                        new TaskProgressData());

                    Task task = Task.Run(() =>
                    {
                        try
                        {
                            plcncliCommunication.ExecuteCommand("set setting", null, null, "-a", "SdkPaths", $"\"{sdk}\"");
                        }
                        catch (PlcncliException e)
                        {
                            ShowMessage(e.Message, $"{NamingConstants.ToolName} error");
                        }
                    });
                    subTaskHandler.RegisterTask(task);
                    await task;
                }
                foreach (InstallSdk sdk in model.SdkChangesCollector.SdksToInstall)
                {
                    ITaskHandler subTaskHandler = taskCenter.PreRegister(
                        new TaskHandlerOptions() { Title = $"Installing sdk {sdk.ArchiveFile} to {sdk.Destination}" },
                        new TaskProgressData());
                    Task task = Task.Run(() =>
                    {
                        try
                        {
                            plcncliCommunication.ExecuteCommand("install sdk", null, null, "--path", $"\"{sdk.ArchiveFile}\"",
                            "--destination", $"\"{sdk.Destination}\"", sdk.Force ? "--force" : "");
                        }
                        catch (PlcncliException e)
                        {
                            ShowMessage(e.Message, $"{NamingConstants.ToolName} error");
                        }
                    });
                    subTaskHandler.RegisterTask(task);
                    await task;
                }
            }));

            if(model.SdkChangesCollector.SdksToInstall.Count > 0)
                MessageBox.Show("Installing sdks in background. This may take a while. New sdks are available after background task has finished." +
                    " Check lower left corner for active background tasks.", "Background installation started", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private static void ShowMessage(string message, string title)
        {
            MessageBox.Show(message, title);
        }
    }
}
