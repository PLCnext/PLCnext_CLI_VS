﻿#region Copyright
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
    public class DeployTask : PlcncliTask
    {
        public override bool Execute()
        {
            Log.LogMessage(MessageImportance.Low, "Starting deploy task.");
            Log.LogMessage(MessageImportance.Low, "Additional deploy options value: \"" + AdditionalOptions + "\"");

            string buildType = Configuration.StartsWith("Debug") ? "Debug" : "Release";
            string target = Configuration.Substring(buildType.Length);

            IEnumerable<string> options = new[] { "-p", ProjectDirectory };

            if (!target.Contains("all Targets"))
            {
                options = options.Append("-t");
                options = options.Append($"\"{ target.Trim()}\"");
            }

            options = options.Append("-b");
            options = options.Append(buildType);


            if (SourceFolders.Any())
            {
                options = options.Append("--sources").Append(string.Join(",", SourceFolders)).ToArray();

            }

            if (LibraryVersion == null)
            {
                LibraryVersion = string.Empty;
            }
            options = options.Append($"--libraryversion \"{LibraryVersion}\"");


            if (LibraryDescription == null)
            {
                LibraryDescription = string.Empty;
            }
            options = options.Append($"--librarydescription \"{LibraryDescription}\"");

            if (EngineerVersion != null)
            {
                options = options.Append($"--engineerversion \"{EngineerVersion}\"");
            }

            options = options.Append(AdditionalOptions);

            try
            {
                Communication.ExecuteWithoutResult("deploy", new TaskLogger(Log), options.ToArray());
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
        private IEnumerable<string> SourceFolders
        {
            get
            {
                return string.IsNullOrEmpty(SourceFoldersRaw)
                             ? Enumerable.Empty<string>()
                             : SourceFoldersRaw.Split(new[] { ';', ',' }, System.StringSplitOptions.RemoveEmptyEntries);
            }
        }
        public string SourceFoldersRaw { get; set; }

        public string LibraryVersion { get; set; }

        public string LibraryDescription { get; set; }

        public string EngineerVersion { get; set; }

    }
}
