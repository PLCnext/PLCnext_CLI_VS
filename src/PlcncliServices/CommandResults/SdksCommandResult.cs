﻿#region Copyright
///////////////////////////////////////////////////////////////////////////////
//
//  Copyright (c) Phoenix Contact GmbH & Co KG
//  This software is licensed under Apache-2.0
//
///////////////////////////////////////////////////////////////////////////////
#endregion

using System.Collections.Generic;
using Newtonsoft.Json;

namespace PlcncliServices.CommandResults
{
    public class SdksCommandResult : CommandResult
    {
        public SdksCommandResult(IEnumerable<Path> sdks)
        {
            Sdks = sdks;
        }

        [JsonProperty(PropertyName = "sdks")]
        public IEnumerable<Path> Sdks { get; }
    }
}