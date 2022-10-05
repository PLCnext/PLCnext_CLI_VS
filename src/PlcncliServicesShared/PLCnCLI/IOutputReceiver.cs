#region Copyright
///////////////////////////////////////////////////////////////////////////////
//
//  Copyright (c) Phoenix Contact GmbH & Co KG
//  This software is licensed under Apache-2.0
//
///////////////////////////////////////////////////////////////////////////////
#endregion

using System.Collections.Generic;

namespace PlcncliServices.PLCnCLI
{
    public interface IOutputReceiver
    {
        void WriteLine(string line);

        void WriteError(string error);

        List<string> InfoMessages { get; }

        List<string> ErrorMessages { get; }

        void LogDebugInfo(string info);
    }
}
