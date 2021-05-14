#region Copyright
///////////////////////////////////////////////////////////////////////////////
//
//  Copyright (c) Phoenix Contact GmbH & Co KG
//  This software is licensed under Apache-2.0
//
///////////////////////////////////////////////////////////////////////////////
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlcNextVSExtension.PlcNextProject
{
    public static class Constants
    {
        // PLCnextCommonPropertiesRule
        public const string PLCnextIncludesKey = "PLCnCLIIncludes";
        public const string PLCnextMacrosKey = "PLCnCLIMacros";
        public const string PLCnextRuleName = "PLCnextCommonProperties";

        //VS
        public const string VCppIncludesRuleName = "ConfigurationDirectories";
        public const string VCppIncludesKey = "IncludePath";
        public const string VCPreprocessorsKey = "PreprocessorDefinitions";
        public const string CLRuleName = "CL";
    }
}
