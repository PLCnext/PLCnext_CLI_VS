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
using System.Threading;
using Newtonsoft.Json;
using PlcncliServices.CommandResults;
using Task = System.Threading.Tasks.Task;

namespace PlcncliServices.PLCnCLI
{
    public class PlcncliProcessCommunication : IPlcncliCommunication, SPlcncliCommunication
    {
        private readonly PlcncliLocationService _locationService;
        private readonly string _defaultLocation;

        public PlcncliProcessCommunication(PlcncliLocationService locationService, string defaultLocation = "plcncli")
        {
           _locationService = locationService;
           _defaultLocation = defaultLocation;
        }
        
        private string PlcncliCommand
        {
            get
            {
                if (_locationService != null)
                    return _locationService.GetLocation();
                return _defaultLocation;
            }
        }

        public Task InitializeAsync(CancellationToken cancellationToken)
        {
            return _locationService.InitializeAsync(cancellationToken);
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
            
            var result = JsonConvert.DeserializeObject(string.Join("", infos.SkipWhile(s => !s.Trim().StartsWith("{"))), resultType??typeof(CommandResult));
            return result as CommandResult;
           
        }

        public void ExecuteWithoutResult(string command, IOutputReceiver receiver = null, params string[] arguments)
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
        }
    }
}
