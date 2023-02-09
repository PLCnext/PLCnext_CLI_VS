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

namespace PlcncliServices.PLCnCLI
{
    public class PlcncliProcessCommunication : PathPlcncliProcessCommunication
    {
        private readonly PlcncliLocationService _locationService;

        public PlcncliProcessCommunication(PlcncliLocationService locationService, string defaultLocation = "plcncli")
            : base(defaultLocation)
        {
           _locationService = locationService;
        }

        protected override string PlcncliCommand
        {
            get
            {
                if (_locationService != null)
                    return _locationService.GetLocation();
                return _defaultLocation;
            }
        }
    }
}
