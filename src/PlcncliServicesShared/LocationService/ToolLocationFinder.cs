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

            if (CheckPathVariable())
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

            bool CheckPathVariable()
            {
                string pathVariable = Environment.GetEnvironmentVariable("PATH");
                if (pathVariable != null)
                {
                    string[] pathParts = pathVariable.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string path in pathParts)
                    {
                        DirectoryInfo fileInfo = new DirectoryInfo(path);
                        if (fileInfo.Exists)
                        {
                            FileInfo[] files = fileInfo.GetFiles();
                            foreach (FileInfo file in files)
                            {
                                if (file.Name.Equals(plcncliFileName))
                                {
                                    toolLocation = file.FullName;
                                    return true;
                                }
                            }
                        }
                    }
                }
                return false;
            }
        }
    }
}
