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
using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json;
using PlcncliServices;
using PlcNextVSExtension.CommandResults;

namespace PlcNextVSExtension.PLCnCLI
{
    public class PlcncliProcessCommunication : IPlcncliCommunication
    {
        private IPlcncliLocationService locationService = null;

        private string PlcncliCommand
        {
            get
            {
                if(locationService != null)
                    return locationService.GetLocation();
                return "plcncli";
            }
        }

        public PlcncliProcessCommunication()
        {
                locationService = Package.GetGlobalService(typeof(SPlcncliLocationService)) as IPlcncliLocationService;
        }

        public CommandResult ExecuteCommand(string command, Type resultType = null, params string[] arguments)
        {
            OutputCollector receiver = new OutputCollector();
            int exitCode = 0;

            string commandline = $"{command} {string.Join(" ", arguments)}";

            using (ProcessFacade f = new ProcessFacade(PlcncliCommand, commandline, receiver, CancellationToken.None))
            {
                f.WaitForExit();
                exitCode = f.ExitCode;
            }


            if (exitCode != 0)
            {
                //TODO throw error correctly
                string error = receiver.ErrorMessages.Any()
                    ? string.Join("\n", receiver.ErrorMessages)
                    : string.Join("\n", receiver.InfoMessages);
                throw new InvalidOperationException($"An error occured while executing the command {commandline}. {error}");
            }

            List<string> infos = receiver.InfoMessages;
            
            var result = JsonConvert.DeserializeObject(string.Join("", infos.SkipWhile(s => !s.Trim().StartsWith("{"))), resultType??typeof(CommandResult));
            return result as CommandResult;
           
        }
    }
}
