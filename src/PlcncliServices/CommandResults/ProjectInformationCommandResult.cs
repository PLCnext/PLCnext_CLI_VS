#region Copyright
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
    public class ProjectInformationCommandResult : CommandResult
    {
        [JsonProperty(PropertyName = "name", Required =Required.Always)]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "namespace")]
        public string Namespace { get; set; }

        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }

        [JsonProperty(PropertyName = "targets")]
        public IEnumerable<TargetResult> Targets { get; set; }

        [JsonProperty(PropertyName = "entities")]
        public IEnumerable<EntityResult> Entities { get; set; }

        [JsonProperty(PropertyName = "includePaths")]
        public IEnumerable<IncludePath> IncludePaths { get; set; }
    }

    public class EntityResult
    {
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "namespace")]
        public string Namespace { get; set; }

        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }

        [JsonProperty(PropertyName = "relatedEntity")]
        public IEnumerable<string> ChildEntities { get; set; }
    }
}
