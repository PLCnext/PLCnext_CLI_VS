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
    public class GenerateTask: PlcncliTask
    {
        public override bool Execute()
        {
            Log.LogMessage(MessageImportance.Low, "Starting generate task.");
            Log.LogMessage(MessageImportance.Low, "Plcncli location: \"" + PlcncliLocation + "\"");
            Log.LogMessage(MessageImportance.Low, "Additional generate options value: \"" + AdditionalOptions + "\"");
            try
            {
                Communication.ExecuteWithoutResult("generate all", new TaskLogger(Log), "-p", ProjectDirectory, AdditionalOptions);
            }
            catch (PlcncliException ex)
            {
                if(!Log.HasLoggedErrors)
                    Log.LogErrorFromException(ex);
                return false;
            }

            Log.LogMessage(MessageImportance.Low, "generate task finished.");

            return true;
        }
    }
}
