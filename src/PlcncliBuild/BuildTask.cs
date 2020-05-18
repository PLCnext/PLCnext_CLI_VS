#region Copyright
///////////////////////////////////////////////////////////////////////////////
//
//  Copyright (c) Phoenix Contact GmbH & Co KG
//  This software is licensed under Apache-2.0
//
///////////////////////////////////////////////////////////////////////////////
#endregion


using System;
using Microsoft.Build.Framework;
using PlcncliServices.PLCnCLI;

namespace PlcncliBuild
{
    public class BuildTask : PlcncliTask
    {
        public override bool Execute()
        {
            //TODO generate and deploy: consider option -s (source folder)

            Log.LogMessage(MessageImportance.Low, "Starting plcncli build task.");
            Log.LogMessage(MessageImportance.Low, "Additional build options value: \"" + AdditionalOptions + "\"");

            Configuration = Configuration.StartsWith("Debug") ? "Debug" : "Release";

            try {
                Communication.ExecuteWithoutResult("build", new TaskLogger(Log), "-p", ProjectDirectory, "-b", Configuration, AdditionalOptions);
            }
            catch (PlcncliException ex)
            {
                if (!Log.HasLoggedErrors)
                    Log.LogErrorFromException(ex);
                return false;
            }
            Log.LogMessage(MessageImportance.Low, "plcncli build task finished.");

            return true;
        }
        public string Configuration { get; set; }

    }
}
