#region Copyright
///////////////////////////////////////////////////////////////////////////////
//
//  Copyright (c) Phoenix Contact GmbH & Co KG
//  This software is licensed under Apache-2.0
//
///////////////////////////////////////////////////////////////////////////////
#endregion

using PlcncliServices.CommandResults;
using System.Collections.Generic;

namespace PlcncliFeatures.PlcNextProject.OnDocSaveService
{
    public class IncludesAndMacrosWrapper
    {
        public IEnumerable<string> Includes { get; }
        public IEnumerable<CompilerMacroResult> Macros { get; }
        public IncludesAndMacrosWrapper(IEnumerable<string> includes, IEnumerable<CompilerMacroResult> macros)
        {
            Includes = includes;
            Macros = macros;
        }
    }
}
