#region Copyright
///////////////////////////////////////////////////////////////////////////////
//
//  Copyright (c) Phoenix Contact GmbH & Co KG
//  This software is licensed under Apache-2.0
//
///////////////////////////////////////////////////////////////////////////////
#endregion

using System;
using System.IO;

namespace PlcncliServices.LocationService
{
    public static class ToolLocationFinder
    {
        private static readonly string plcncliFileName = "plcncli.exe";

        public static string SearchPlcncliTool(PlcncliOptionPage optionPage)
        {
            string toolLocation = string.Empty;

            if (CheckOption())
            {
                return toolLocation;
            }

            if (PathToolLocationFinder.CheckPathVariable(ref toolLocation))
            {
                return toolLocation;
            }
            
            return toolLocation;

            bool CheckOption()
            {
                string location = optionPage?.ToolLocation;
                if (!string.IsNullOrEmpty(location) && File.Exists(Path.Combine(location, plcncliFileName)))
                {
                    toolLocation = Path.Combine(location, plcncliFileName);
                    return true;
                }
                return false;
            }
        }
    }
}
