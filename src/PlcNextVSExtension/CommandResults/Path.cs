﻿#region Copyright
///////////////////////////////////////////////////////////////////////////////
//
//  Copyright (c) Phoenix Contact GmbH & Co KG
//  This software is licensed under Apache-2.0
//
///////////////////////////////////////////////////////////////////////////////
#endregion

using Newtonsoft.Json;

namespace PlcNextVSExtension.CommandResults
{
    public class Path
    {
        public Path(string pathValue)
        {
            PathValue = pathValue;
        }

        [JsonProperty(PropertyName = "path")]
        public string PathValue { get; }
    }

    public class UncheckedPath : Path
    {
        public UncheckedPath(string pathValue, bool exists) : base(pathValue)
        {
            Exists = exists;
        }

        [JsonProperty(PropertyName = "exists")]
        public bool Exists { get; }
    }
}
