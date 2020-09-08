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
using PlcncliServices.CommandResults;

namespace PlcncliServices.PLCnCLI
{
    public interface IPlcncliCommunication
    {
        CommandResult ExecuteCommand(string command, IOutputReceiver receiver, Type resultType, params string[] arguments);

        void ExecuteWithoutResult(string command, IOutputReceiver receiver = null, params string[] arguments);

        T ConvertToTypedCommandResult<T>(IEnumerable<string> messages);
    }
}
