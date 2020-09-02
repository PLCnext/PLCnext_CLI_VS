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
    public class CompilerSpecificationCommandResult : CommandResult
    {
        public CompilerSpecificationCommandResult(IEnumerable<CompilerSpecificationResult> specifications)
        {
            Specifications = specifications;
        }

        [JsonProperty(PropertyName = "compilerSpecifications")]
        public IEnumerable<CompilerSpecificationResult> Specifications { get; }
    }

    public class CompilerSpecificationResult
    {
        public CompilerSpecificationResult(string compilerPath, string language, string compilerSystemRoot, string compilerFlags, IEnumerable<Path> includePaths, IEnumerable<CompilerMacroResult> compilerMacros)
        {
            CompilerPath = compilerPath;
            Language = language;
            CompilerSystemRoot = compilerSystemRoot;
            CompilerFlags = compilerFlags;
            IncludePaths = includePaths;
            CompilerMacros = compilerMacros;
        }

        [JsonProperty(PropertyName = "compilerPath")]
        public string CompilerPath { get; }

        [JsonProperty(PropertyName = "language")]
        public string Language { get; }

        [JsonProperty(PropertyName = "compilerSysroot")]
        public string CompilerSystemRoot { get; }

        [JsonProperty(PropertyName = "compilerFlags")]
        public string CompilerFlags { get; }

        [JsonProperty(PropertyName = "includePaths")]
        public IEnumerable<Path> IncludePaths { get; }

        [JsonProperty(PropertyName = "compilerMacros")]
        public IEnumerable<CompilerMacroResult> CompilerMacros { get; }
    }

    public class CompilerMacroResult
    {
        public CompilerMacroResult(string name, string value)
        {
            Name = name;
            Value = value;
        }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; }

        [JsonProperty(PropertyName = "value")]
        public string Value { get; }
    }
}
