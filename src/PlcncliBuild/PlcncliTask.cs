#region Copyright
///////////////////////////////////////////////////////////////////////////////
//
//  Copyright (c) Phoenix Contact GmbH & Co KG
//  This software is licensed under Apache-2.0
//
///////////////////////////////////////////////////////////////////////////////
#endregion

using System;
using PlcncliServices.PLCnCLI;
using Task = Microsoft.Build.Utilities.Task;

namespace PlcncliBuild
{
    public abstract class PlcncliTask : Task 
    {
        protected PlcncliTask()
        {
            PlcncliLocation = Environment.GetEnvironmentVariable("plcncli_toollocation");
            Communication = new PlcncliProcessCommunication(null, PlcncliLocation);
        }

        internal string PlcncliLocation { get; }

        internal PlcncliProcessCommunication Communication { get; }

        public string ProjectDirectory { get; set; }

        public string AdditionalOptions { get; set; }
    }
}
