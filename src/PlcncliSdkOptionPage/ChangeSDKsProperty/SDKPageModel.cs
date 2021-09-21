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
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace PlcncliSdkOptionPage.ChangeSDKsProperty
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
            try
            {
                SdksCommandResult commandResult = plcncli.ExecuteCommand("get sdks", null, typeof(SdksCommandResult)) as SdksCommandResult;
                Sdks = commandResult.Sdks.Select(sdk => new SdkViewModel(sdk.PathValue, sdk.Targets));
            }
            catch(PlcncliException e)
            {
                MessageBox.Show(e.Message, "PLCnCLI error");
                try
                {
                    SdkPathsSettingCommandResult commandResult =
                        plcncli.ExecuteCommand("get setting", null, typeof(SdkPathsSettingCommandResult), "SdkPaths") as SdkPathsSettingCommandResult;
                    Sdks = commandResult.Settings.SdkPaths.Split(';').Select(sdk => new SdkViewModel(sdk, Enumerable.Empty<TargetResult>()));
                }
                catch(PlcncliException e1)
                {
                    MessageBox.Show(e1.Message, "PLCnCLI get settings error");
                }
            }
            SdkChangesCollector = new SdkChangesCollector();
        }
    }
}
