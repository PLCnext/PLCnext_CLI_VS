#region Copyright
///////////////////////////////////////////////////////////////////////////////
//
//  Copyright (c) Phoenix Contact GmbH & Co KG
//  This software is licensed under Apache-2.0
//
///////////////////////////////////////////////////////////////////////////////
#endregion

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace PlcncliServices.CommandResults
{
    public class SdkPathsSettingCommandResult : CommandResult
    {
        [JsonProperty(PropertyName = "setting")]
        public SdkPathsSetting Settings { get; set; }
    }

    public class SdkPathsSetting
    {
        [JsonProperty(PropertyName ="SdkPaths")]
        public string SdkPaths { get; set; }
    }
}