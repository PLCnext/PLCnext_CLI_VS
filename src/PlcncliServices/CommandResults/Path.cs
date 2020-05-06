#region Copyright
///////////////////////////////////////////////////////////////////////////////
//
//  Copyright (c) Phoenix Contact GmbH & Co KG
//  This software is licensed under Apache-2.0
//
///////////////////////////////////////////////////////////////////////////////
#endregion

using Newtonsoft.Json;

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
}
