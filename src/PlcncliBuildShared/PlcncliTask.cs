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
using PlcncliServices;
using Task = Microsoft.Build.Utilities.Task;

namespace PlcncliBuild
{
    public abstract class PlcncliTask : Task
    {
        private string projectDirectory;

        protected PlcncliTask()
        {
            PlcncliLocation = Environment.GetEnvironmentVariable("plcncli_toollocation");
            if (string.IsNullOrEmpty(PlcncliLocation))
            {
                PlcncliLocation = ToolLocationFinder.SearchPlcncliTool(null);
                if (string.IsNullOrEmpty(PlcncliLocation))
                {
                    throw new ArgumentException($"{NamingConstants.ToolName} tool location could not be resolved.");
                }
            }
            Communication = new PlcncliProcessCommunication(null, PlcncliLocation);
        }
        internal string PlcncliLocation { get; }

        internal IPlcncliCommunication Communication { get; }

        public string ProjectDirectory
        {
            get => projectDirectory; 
            set
            {
                projectDirectory = value.TrimEnd('\\');
            }
        }

        public string AdditionalOptions { get; set; }
    }
}
