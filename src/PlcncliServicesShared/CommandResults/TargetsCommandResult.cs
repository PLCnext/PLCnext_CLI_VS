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
    public class TargetsCommandResult : CommandResult
    {
        [JsonProperty(PropertyName = "targets")]
        public IEnumerable<TargetResult> Targets { get; set; }
    }

    public class TargetResult
    {
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "version")]
        public string Version { get; set; }

        [JsonProperty(PropertyName = "longVersion")]
        public string LongVersion { get; set; }

        [JsonProperty(PropertyName = "shortVersion")]
        public string ShortVersion { get; set; }

        [JsonProperty(PropertyName = "available")]
        public bool? Available { get; set; }

        public string GetDisplayName()
        {
            return $"{Name} {LongVersion}";
        }

        public string GetNameFormattedForCommandLine()
        {
            return $"{Name},{LongVersion}";
        }
    }
}
