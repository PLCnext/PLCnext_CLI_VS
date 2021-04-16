#region Copyright
///////////////////////////////////////////////////////////////////////////////
//
//  Copyright (c) Phoenix Contact GmbH & Co KG
//  This software is licensed under Apache-2.0
//
///////////////////////////////////////////////////////////////////////////////
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using PlcncliServices.PLCnCLI;

namespace PlcncliBuild
{
    class TaskLogger : IOutputReceiver
    {
        private readonly TaskLoggingHelper _loggingHelper;
        public TaskLogger(TaskLoggingHelper loggingHelper)
        {
            _loggingHelper = loggingHelper;
        }

        public void WriteLine(string line)
        {
            bool logAsError = false;
            string rawLine = line;
            
            InfoMessages.Add(line);

            RemoveCmakePrefix();

            FormatLinkerPrefix();
            
            // output format of librarybuilder does not match visual studio error format
            string libBuilder = "[EngineeringLibraryBuilder]:";

            if (line.TrimStart().StartsWith(libBuilder) && ErrorMessages.Contains(line))
            {
                logAsError = true; 
            }

            if (logAsError)
            {
                _loggingHelper.LogMessageFromText("error: "+line, MessageImportance.High);
            }
            else
            {
                _loggingHelper.LogMessageFromText(line, MessageImportance.Normal);
            }

            void RemoveCmakePrefix()
            {
                string cmake = "[cmake]: ";
                if (line.Contains(cmake))
                    line = line.Substring(line.IndexOf(cmake) + cmake.Length);
            }

            void FormatLinkerPrefix()
            {
                string linker = "real-ld.exe";
                if (line.Contains(linker + ":"))
                {
                    string linkerPath = line.Substring(0, line.IndexOf(linker) + linker.Length);
                    if (File.Exists(linkerPath))
                    {
                        if (ErrorMessages.Contains(rawLine))
                        {
                            logAsError = true;
                        }
                            
                        line = line.Substring(line.IndexOf(linker) + linker.Length + 1) + $"(message from {linkerPath})";
                    }
                }
            }
        }

        public void WriteError(string error)
        {
            ErrorMessages.Add(error);
            WriteLine(error);
        }

        void IOutputReceiver.LogDebugInfo(string info)
        {
            _loggingHelper.LogCommandLine(info);
        }

        public List<string> InfoMessages { get; } = new List<string>();
        public List<string> ErrorMessages { get; } = new List<string>();
    }
}
