#region Copyright
///////////////////////////////////////////////////////////////////////////////
//
//  Copyright (c) Phoenix Contact GmbH & Co KG
//  This software is licensed under Apache-2.0
//
///////////////////////////////////////////////////////////////////////////////
#endregion

using System;
using PlcNextVSExtension.CommandResults;

namespace PlcNextVSExtension
{
    public interface IPlcncliCommunication
    {
        CommandResult ExecuteCommand(string command, Type resultType, params string[] arguments);
    }
}
