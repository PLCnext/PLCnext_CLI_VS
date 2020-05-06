#region Copyright
///////////////////////////////////////////////////////////////////////////////
//
//  Copyright (c) Phoenix Contact GmbH & Co KG
//  This software is licensed under Apache-2.0
//
///////////////////////////////////////////////////////////////////////////////
#endregion

using System.Collections.Generic;
using System.Diagnostics;

namespace PlcncliServices.PLCnCLI
{

    public class OutputCollector : IOutputReceiver
    {
        public List<string> InfoMessages { get; } = new List<string>();
        public List<string> ErrorMessages { get; } = new List<string>();

        public void WriteLine(string line)
        {
            Debug.WriteLine(line);
            InfoMessages.Add(line);
        }

        public void WriteError(string error)
        {
            Debug.WriteLine(error);
            ErrorMessages.Add(error);
        }

    }
}
