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

namespace PlcNextVSExtension.CommandResults
{
    public class TargetsCommandResult : CommandResult
    {
        public TargetsCommandResult(IEnumerable<TargetResult> targets)
        {
            Targets = targets;
        }

        [JsonProperty(PropertyName = "targets")]
        public IEnumerable<TargetResult> Targets { get; }
    }

    public class TargetResult
    {
        public TargetResult(string name, string version, string longVersion, string shortVersion, bool? available = null)
        {
            Name = name;
            Version = version;
            LongVersion = longVersion;
            ShortVersion = shortVersion;
            Available = available;
        }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; }

        [JsonProperty(PropertyName = "version")]
        public string Version { get; }

        [JsonProperty(PropertyName = "longVersion")]
        public string LongVersion { get; }

        [JsonProperty(PropertyName = "shortVersion")]
        public string ShortVersion { get; }

        [JsonProperty(PropertyName = "available")]
        public bool? Available { get; }

        public string GetDisplayName()
        {
            return $"{Name} {LongVersion}";
        }
    }
}
