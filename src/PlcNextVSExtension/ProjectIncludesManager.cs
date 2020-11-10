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
using System.Linq;

namespace PlcNextVSExtension
{
    internal static class ProjectIncludesManager
    {
        public static TargetResult MinTarget(this IEnumerable<TargetResult> targets)
        {
            if (!targets.Any())
                return null;
            return targets.Aggregate((current, next) =>
            {
                int result = current.ShortVersion.CompareTo(next.ShortVersion);
                if (result == 0)
                {
                    result = next.Name.CompareTo(current.Name);
                }
                if (result <= 0)
                    return current;
                return next;
            });
        }

        public static (IEnumerable<CompilerMacroResult> macros, IEnumerable<string> includes) FindMacrosAndIncludes(CompilerSpecificationCommandResult compilerSpecsCommandResult,
                                                                                                                    ProjectInformationCommandResult projectInformation)
        {
            IEnumerable<CompilerMacroResult> macros = Enumerable.Empty<CompilerMacroResult>();
            IEnumerable<string> includes = Enumerable.Empty < string>();

            TargetResult minCompilerTarget = compilerSpecsCommandResult.Specifications
                                                              .SelectMany(x => x.Targets)
                                                              .MinTarget();
            if (minCompilerTarget != null)
            {
                macros = compilerSpecsCommandResult?.Specifications
                                                    .FirstOrDefault(s => s.Targets
                                                                          .Any(t => t.Name.Equals(minCompilerTarget.Name) &&
                                                                                    t.LongVersion.Equals(minCompilerTarget.LongVersion)
                                                                              )
                                                                   )
                                                    ?.CompilerMacros
                                                    .Where(m => !m.Name.StartsWith("__has_include("));
            }

            TargetResult minIncludeTarget = projectInformation.Targets
                                                              .Where(t => t.Available == true)
                                                              .MinTarget();

            includes = projectInformation.IncludePaths
                                         .Where(x => x.Targets == null ||
                                                     !x.Targets.Any() ||
                                                     (minIncludeTarget != null && x.Targets.Any(t => t.Name.Equals(minIncludeTarget.Name) &&
                                                                                                    t.LongVersion.Equals(minIncludeTarget.LongVersion))
                                                                                                )
                                                     )
                                         .Select(p => p.PathValue);


            return (macros, includes);
        }
    }
}
