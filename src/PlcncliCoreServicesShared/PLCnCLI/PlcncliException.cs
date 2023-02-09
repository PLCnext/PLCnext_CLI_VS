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
using System.Linq;

namespace PlcncliServices.PLCnCLI
{
    public class PlcncliException : Exception
    {
        public List<string> InfoMessages { get; }
        public List<string> ErrorMessages { get; }

        private readonly string _command;

        public PlcncliException(string command, List<string> infoMessages, List<string> errorMessages)
        {
            _command = command;
            InfoMessages = infoMessages;
            ErrorMessages = errorMessages;
        }

        public override string Message =>
            ErrorMessages.Any()
                ? string.Join("\n", ErrorMessages)
                : string.Join("\n", InfoMessages);
    }
}
