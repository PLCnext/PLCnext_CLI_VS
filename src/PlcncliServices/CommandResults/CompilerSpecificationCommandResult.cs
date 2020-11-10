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
    public class CompilerSpecificationCommandResult : CommandResult
    {
        [JsonProperty(PropertyName = "compilerSpecifications")]
        public IEnumerable<CompilerSpecificationResult> Specifications { get; set; }
    }

    public class CompilerSpecificationResult
    {
        [JsonProperty(PropertyName = "compilerPath")]
        public string CompilerPath { get; set; }

        [JsonProperty(PropertyName = "language")]
        public string Language { get; set; }

        [JsonProperty(PropertyName = "compilerSysroot")]
        public string CompilerSystemRoot { get; set; }

        [JsonProperty(PropertyName = "compilerFlags")]
        public string CompilerFlags { get; set; }

        [JsonProperty(PropertyName = "includePaths")]
        public IEnumerable<Path> IncludePaths { get; set; }

        [JsonProperty(PropertyName = "compilerMacros")]
        public IEnumerable<CompilerMacroResult> CompilerMacros { get; set; }

        [JsonProperty(PropertyName ="targets")]
        public IEnumerable<TargetResult> Targets { get; set; }
    }

    public class CompilerMacroResult
    {
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "value")]
        public string Value { get; set; }
    }
}
