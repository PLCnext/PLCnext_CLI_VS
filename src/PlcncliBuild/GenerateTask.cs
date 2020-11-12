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
using System.Collections.Generic;
using System.Linq;

namespace PlcncliBuild
{
    public class GenerateTask: PlcncliTask
    {
        public override bool Execute()
        {
            Log.LogMessage(MessageImportance.Low, "Starting generate task.");
            Log.LogMessage(MessageImportance.Low, "Plcncli location: \"" + PlcncliLocation + "\"");
            Log.LogMessage(MessageImportance.Low, "Additional generate options value: \"" + AdditionalOptions + "\"");

            string[] args =new string[]
            {
                "-p",
                ProjectDirectory
            };

            if(GenerateDatatypesWorksheet == false)
            {
                args = args.Append("--no-datatypes-worksheet").ToArray();
            }

            args = args.Append(AdditionalOptions).ToArray();

            try
            {
                Communication.ExecuteWithoutResult("generate all", new TaskLogger(Log), args);
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

        public bool GenerateDatatypesWorksheet { get; set; }
    }
}
