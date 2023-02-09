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
    public static class PathToolLocationFinder
    {
        private static readonly string plcncliFileName = "plcncli.exe";

        public static string SearchPlcncliToolInPath()
        {
            string toolLocation = string.Empty;

            CheckPathVariable(ref toolLocation);
            return toolLocation;
        }

        internal static bool CheckPathVariable(ref string toolLocation)
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
