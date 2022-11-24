#region Copyright
///////////////////////////////////////////////////////////////////////////////
//
//  Copyright (c) Phoenix Contact GmbH & Co KG
//  This software is licensed under Apache-2.0
//
///////////////////////////////////////////////////////////////////////////////
#endregion


using System.Collections.Generic;
using System.Linq;
using Microsoft.Build.Framework;
using PlcncliServices.PLCnCLI;
using PlcncliServices;

namespace PlcncliBuild
{
    public class BuildTask : PlcncliTask
    {
        public override bool Execute()
        {
            Log.LogMessage(MessageImportance.Low, $"Starting {NamingConstants.ToolName} build task.");
            Log.LogMessage(MessageImportance.Low, "Additional build options value: \"" + AdditionalOptions + "\"");

            string buildType = Configuration.StartsWith("Debug") ? "Debug" : "Release";
            string target = Configuration.Substring(buildType.Length);

            IEnumerable<string> options = new[] { "-p", $"\"{ProjectDirectory}\"" };
            if (!target.Contains("all Targets"))
            {
                options = options.Append("-t");
                options = options.Append($"\"{target.Trim()}\"");
            }

            
            options = options.Append("-b");
            options = options.Append(buildType);
            options = options.Append(AdditionalOptions);

            try {
                Communication.ExecuteWithoutResult("build", new TaskLogger(Log), options.ToArray());
            }
            catch (PlcncliException ex)
            {
                if (!Log.HasLoggedErrors)
                    Log.LogErrorFromException(ex, false, true, "-");
                return false;
            }
            Log.LogMessage(MessageImportance.Low, $"{NamingConstants.ToolName} build task finished.");

            return true;
        }
        public string Configuration { get; set; }

    }
}
