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
            InfoMessages.Add(line);
            string cmake = "[cmake]: ";
            if (line.Contains(cmake))
                line = line.Substring(line.IndexOf(cmake) + cmake.Length);
            _loggingHelper.LogMessageFromText(line, MessageImportance.Normal);
            
        }

        public void WriteError(string error)
        {
           throw new NotImplementedException();
            
        }

        public List<string> InfoMessages { get; } = new List<string>();
        public List<string> ErrorMessages { get; } = new List<string>();
    }
}
