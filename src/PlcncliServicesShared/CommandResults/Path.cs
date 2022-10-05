#region Copyright
///////////////////////////////////////////////////////////////////////////////
//
//  Copyright (c) Phoenix Contact GmbH & Co KG
//  This software is licensed under Apache-2.0
//
///////////////////////////////////////////////////////////////////////////////
#endregion

using Newtonsoft.Json;
using System.Collections.Generic;

namespace PlcncliServices.CommandResults
{
    public class Path
    {
        [JsonProperty(PropertyName = "path")]
        public string PathValue { get; set; }
    }

    public class UncheckedPath : Path
    {
        [JsonProperty(PropertyName = "exists")]
        public bool Exists { get; set; }
    }

    public class IncludePath : UncheckedPath
    {
        [JsonProperty(PropertyName = "targets")]
        public IEnumerable<TargetResult> Targets { get; set; }
    }

    public class SdkPath : Path
    {
        [JsonProperty(PropertyName = "targets")]
        public IEnumerable<TargetResult> Targets { get; set; }
    }
}
