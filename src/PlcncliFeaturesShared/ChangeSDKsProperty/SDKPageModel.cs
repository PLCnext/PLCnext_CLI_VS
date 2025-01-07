#region Copyright
///////////////////////////////////////////////////////////////////////////////
//
//  Copyright (c) Phoenix Contact GmbH & Co KG
//  This software is licensed under Apache-2.0
//
///////////////////////////////////////////////////////////////////////////////
#endregion

using PlcncliServices.CommandResults;
using PlcncliServices.PLCnCLI;
using PlcncliServices;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Microsoft.VisualStudio.Shell;

namespace PlcncliFeatures.ChangeSDKsProperty
{
    public class SDKPageModel
    {
        internal IPlcncliCommunication plcncli;

        public IEnumerable<SdkViewModel> Sdks { get; private set; }

        public ISdkChangesCollector SdkChangesCollector { get; private set; }

        public SDKPageModel(IPlcncliCommunication plcncliCommunication)
        {
            this.plcncli = plcncliCommunication;
            Initialize();
        }

        public void Initialize()
        {
            ThreadHelper.JoinableTaskFactory.Run(
                    "Creating sdk page",
                    async (progress) =>
                    {
                        try
                        {
                            progress.Report(new ThreadedWaitDialogProgressData("Fetching sdk information..."));
                            SdksCommandResult commandResult = plcncli.ExecuteCommand("get sdks", null, typeof(SdksCommandResult)) as SdksCommandResult;
                            Sdks = commandResult.Sdks.Select(sdk => new SdkViewModel(sdk.PathValue, sdk.Targets));
                        }
                        catch (PlcncliException e)
                        {
                            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                            MessageBox.Show(e.Message, $"{NamingConstants.ToolName} error");
                            try
                            {
                                SdkPathsSettingCommandResult commandResult =
                                    plcncli.ExecuteCommand("get setting", null, typeof(SdkPathsSettingCommandResult), "SdkPaths") as SdkPathsSettingCommandResult;
                                Sdks = commandResult.Settings.SdkPaths.Split(';').Select(sdk => new SdkViewModel(sdk, Enumerable.Empty<TargetResult>()));
                            }
                            catch (PlcncliException e1)
                            {
                                MessageBox.Show(e1.Message, $"{NamingConstants.ToolName} get settings error");
                            }
                        }
                    });
            SdkChangesCollector = new SdkChangesCollector();
        }
    }
}
