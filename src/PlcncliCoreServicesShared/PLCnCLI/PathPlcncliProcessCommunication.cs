#region Copyright
///////////////////////////////////////////////////////////////////////////////
//
//  Copyright (c) Phoenix Contact GmbH & Co KG
//  This software is licensed under Apache-2.0
//
///////////////////////////////////////////////////////////////////////////////
#endregion

using Newtonsoft.Json;
using PlcncliServices.CommandResults;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace PlcncliServices.PLCnCLI
{
    public class PathPlcncliProcessCommunication : IPlcncliCommunication, SPlcncliCommunication
    {
        public PathPlcncliProcessCommunication(string defaultLocation = "plcncli")
        {
            _defaultLocation = defaultLocation;
        }

        protected readonly string _defaultLocation;

        protected virtual string PlcncliCommand
        {
            get
            {
                return _defaultLocation;
            }
        }

        public CommandResult ExecuteCommand(string command, IOutputReceiver receiver = null, Type resultType = null, params string[] arguments)
        {
            if (receiver == null)
                receiver = new OutputCollector();

            int exitCode = 0;

            string commandline = $"{command} {string.Join(" ", arguments)}";

            using (ProcessFacade f = new ProcessFacade(PlcncliCommand, commandline, receiver, CancellationToken.None))
            {
                f.WaitForExit();
                exitCode = f.ExitCode;
            }


            if (exitCode != 0)
            {
                throw new PlcncliException(command, receiver.InfoMessages, receiver.ErrorMessages);
            }

            if (resultType == null)
                return null;

            List<string> infos = receiver.InfoMessages;

            var result = JsonConvert.DeserializeObject(string.Join("", infos.SkipWhile(s => !s.Trim().StartsWith("{"))), resultType ?? typeof(CommandResult));
            return result as CommandResult;

        }

        public void ExecuteWithoutResult(string command, IOutputReceiver receiver = null, params string[] arguments)
        {
            if (receiver == null)
                receiver = new OutputCollector();

            int exitCode = 0;

            string commandline = $"{command} {string.Join(" ", arguments)}";

            //replace password with * in logged arguments
            int index = -1;
            string[] argsWithoutPw = new string[arguments.Length];
            Array.Copy(arguments, argsWithoutPw, arguments.Length);

            if (arguments.Contains("--password"))
            {
                for (int i = 0; i < arguments.Length; i++)
                {
                    if (arguments[i] == "--password"
                        && arguments.Length > i + 1
                        && !arguments[i + 1].StartsWith("-"))
                    {
                        index = i + 1;
                        break;
                    }
                }
                if (index > -1)
                {
                    argsWithoutPw[index] = "*";
                }
            }

            string commandLineWithoutPassword = $"{command} {string.Join(" ", argsWithoutPw)}";

            receiver.LogDebugInfo($"Starting process {PlcncliCommand} with options {commandLineWithoutPassword}");
            using (ProcessFacade f = new ProcessFacade(PlcncliCommand, commandline, receiver, CancellationToken.None))
            {
                f.WaitForExit();
                exitCode = f.ExitCode;
            }


            if (exitCode != 0)
            {
                throw new PlcncliException(command, receiver.InfoMessages, receiver.ErrorMessages);
            }
        }

        public T ConvertToTypedCommandResult<T>(List<string> messages)
        {
            if (messages != null)
            {
                try
                {
                    var result = JsonConvert.DeserializeObject(string.Join(string.Empty, messages.SkipWhile(s => !s.Trim().StartsWith("{"))), typeof(T));
                    return (T)result;
                }
                catch (JsonException) { }
            }
            return default(T);
        }
    }
}
