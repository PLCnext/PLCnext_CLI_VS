#region Copyright
///////////////////////////////////////////////////////////////////////////////
//
//  Copyright (c) Phoenix Contact GmbH & Co KG
//  This software is licensed under Apache-2.0
//
///////////////////////////////////////////////////////////////////////////////
#endregion

using System;
using PlcncliServices.LocationService;
using PlcncliServices.PLCnCLI;
using Task = Microsoft.Build.Utilities.Task;

namespace PlcncliBuild
{
    public abstract class PlcncliTask : Task 
    {
        protected PlcncliTask()
        {
            PlcncliLocation = Environment.GetEnvironmentVariable("plcncli_toollocation");
            if (string.IsNullOrEmpty(PlcncliLocation))
            {
                PlcncliLocation = ToolLocationFinder.SearchPlcncliTool(null);
                if (string.IsNullOrEmpty(PlcncliLocation))
                {
                    throw new ArgumentException("PLCnCLI tool location could not be resolved.");
                }
            }
            Communication = new PlcncliProcessCommunication(null, PlcncliLocation);
        }
        internal string PlcncliLocation { get; }

        internal IPlcncliCommunication Communication { get; }

        public string ProjectDirectory { get; set; }

        public string AdditionalOptions { get; set; }
    }
}
