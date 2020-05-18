#region Copyright
///////////////////////////////////////////////////////////////////////////////
//
//  Copyright (c) Phoenix Contact GmbH & Co KG
//  This software is licensed under Apache-2.0
//
///////////////////////////////////////////////////////////////////////////////
#endregion

using Microsoft.Build.Framework;
using PlcncliServices.PLCnCLI;

namespace PlcncliBuild
{
    public class DeployTask : PlcncliTask
    {
        public override bool Execute()
        {
            Log.LogMessage(MessageImportance.Low, "Starting deploy task.");
            Log.LogMessage(MessageImportance.Low, "Additional deploy options value: \"" + AdditionalOptions + "\"");

            Configuration = Configuration.StartsWith("Debug") ? "Debug" : "Release";

            try
            {
                Communication.ExecuteWithoutResult("deploy", new TaskLogger(Log), "-p", ProjectDirectory, "-b", Configuration, AdditionalOptions);
            }
            catch (PlcncliException ex)
            {
                if (!Log.HasLoggedErrors)
                    Log.LogErrorFromException(ex);
                return false;
            }
            Log.LogMessage(MessageImportance.Low, "deploy task finished.");
            return true;
        }
        public string Configuration { get; set; }

    }
}
