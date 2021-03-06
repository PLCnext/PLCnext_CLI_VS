﻿#region Copyright
///////////////////////////////////////////////////////////////////////////////
//
//  Copyright (c) Phoenix Contact GmbH & Co KG
//  This software is licensed under Apache-2.0
//
///////////////////////////////////////////////////////////////////////////////
#endregion

using Microsoft.VisualStudio.VCProjectEngine;
using PlcncliServices.CommandResults;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;

namespace PlcNextVSExtension.PlcNextProject
{
    internal static class ProjectIncludesManager
    {
        //private static Regex ConfigurationNameRegex = new Regex(@"^(Release|Debug) (?<name>.*),(?<version>.*)$", RegexOptions.Compiled);

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

        //public static (IEnumerable<CompilerMacroResult> macros, IEnumerable<string> includes)
        //    FindMacrosAndIncludesForConfiguration(CompilerSpecificationCommandResult compilerSpecsCommandResult,
        //                                   ProjectInformationCommandResult projectInformation, string configurationName)
        //{
        //    Match match = ConfigurationNameRegex.Match(configurationName);
        //    if (match.Success)
        //    {
        //        string targetName = match.Groups["name"].Value;
        //        string targetVersion = match.Groups["version"].Value;

        //        TargetResult target = projectInformation?.Targets
        //                                                 .Where(t => t.Name == targetName &&
        //                                                             t.LongVersion == targetVersion)
        //                                                 .FirstOrDefault();
        //        if (target != null)
        //        {
        //            IEnumerable<CompilerMacroResult> macros = GetMacrosForTarget(target, compilerSpecsCommandResult);

        //            IEnumerable<string> includes = GetIncludesForTarget(target, projectInformation);

        //            return (macros, includes);
        //        }
        //    }

        //    return FindMacrosAndIncludesForMinTarget(compilerSpecsCommandResult, projectInformation);
        //}

        public static (IEnumerable<CompilerMacroResult> macros, IEnumerable<string> includes) 
            FindMacrosAndIncludesForMinTarget(CompilerSpecificationCommandResult compilerSpecsCommandResult,
                                           ProjectInformationCommandResult projectInformation)
        {
            IEnumerable<CompilerMacroResult> macros = Enumerable.Empty<CompilerMacroResult>();
            IEnumerable<string> includes = Enumerable.Empty < string>();

            TargetResult minCompilerTarget = compilerSpecsCommandResult?.Specifications
                                                              .SelectMany(x => x.Targets)
                                                              .MinTarget();
            if (minCompilerTarget != null)
            {
                macros = GetMacrosForTarget(minCompilerTarget, compilerSpecsCommandResult);
            }

            TargetResult minIncludeTarget = projectInformation?.Targets
                                                              .Where(t => t.Available == true)
                                                              .MinTarget();

            includes = GetIncludesForTarget(minIncludeTarget, projectInformation);


            return (macros, includes);
        }

        private static IEnumerable<string> GetIncludesForTarget(TargetResult target, ProjectInformationCommandResult projectInformation)
        {
            return projectInformation?.IncludePaths
                                      .Where(x => x.Targets == null ||
                                                  !x.Targets.Any() ||
                                                  (target != null && x.Targets.Any(t => t.Name.Equals(target.Name) &&
                                                                                        t.LongVersion.Equals(target.LongVersion))
                                                                                  )
                                                  )
                                      .Select(p => p.PathValue);
        }

        private static IEnumerable<CompilerMacroResult> GetMacrosForTarget(TargetResult target, CompilerSpecificationCommandResult compilerSpecsCommandResult)
        {
            return compilerSpecsCommandResult?.Specifications
                                              .FirstOrDefault(s => s.Targets
                                                                    .Any(t => t.Name.Equals(target.Name) &&
                                                                              t.LongVersion.Equals(target.LongVersion)
                                                                        )
                                                             )
                                              ?.CompilerMacros
                                              .Where(m => !m.Name.StartsWith("__has_include("));
        }

        public static (bool includesSaved, bool macrosSaved)  CheckSavedIncludesAndMacros(VCProject p)
        {
            bool includesSaved = true;
            bool macrosSaved = true;
            foreach (VCConfiguration2 config in p.Configurations)
            {
                IVCRulePropertyStorage plcnextCommonPropertiesRule = config.Rules.Item(Constants.PLCnextRuleName);
                if (plcnextCommonPropertiesRule == null)
                {
                    MessageBox.Show($"{Constants.PLCnextRuleName} rule was not found in configuration rules collection.");
                }

                if (string.IsNullOrEmpty(plcnextCommonPropertiesRule.GetUnevaluatedPropertyValue(Constants.PLCnextIncludesKey)))
                {
                    includesSaved = false;
                }

                if (string.IsNullOrEmpty(plcnextCommonPropertiesRule.GetUnevaluatedPropertyValue(Constants.PLCnextMacrosKey)))
                {
                    macrosSaved = false;
                }
                if (!includesSaved && !macrosSaved)
                {
                    break;
                }
            }
            return (includesSaved, macrosSaved);
        }

        public static void SetIncludesForNewProject(VCProject vcProject,
                                                    CompilerSpecificationCommandResult compilerSpecsCommandResult,
                                                    ProjectInformationCommandResult projectInformation)
        {
            (IEnumerable<CompilerMacroResult> macros, IEnumerable<string> includes) =
                    ProjectIncludesManager.FindMacrosAndIncludesForMinTarget(compilerSpecsCommandResult,
                                                                                 projectInformation);

            if (macros == null)
                macros = Enumerable.Empty<CompilerMacroResult>();
            if (includes == null)
                includes = Enumerable.Empty<string>();

            foreach (VCConfiguration2 config in vcProject.Configurations)
            {
                IVCRulePropertyStorage plcnextCommonPropertiesRule = config.Rules.Item(Constants.PLCnextRuleName);
                if (plcnextCommonPropertiesRule == null)
                {
                    MessageBox.Show("PLCnextCommonProperties rule was not found in configuration rules collection.");
                }

                string joinedMacros = macros.Any() ? string.Join(";", 
                        macros.Select(m => m.Name + (string.IsNullOrEmpty(m.Value.Trim()) ? null : "=" + m.Value)))
                        : string.Empty;
                string joinedIncludes = includes.Any() ? string.Join(";", includes) : string.Empty;

                plcnextCommonPropertiesRule.SetPropertyValue(Constants.PLCnextMacrosKey, joinedMacros);
                plcnextCommonPropertiesRule.SetPropertyValue(Constants.PLCnextIncludesKey, joinedIncludes);

                IVCRulePropertyStorage rule = config.Rules.Item(Constants.VCppIncludesRuleName);
                if (rule == null)
                {
                    MessageBox.Show("ConfigurationDirectories rule was not found in configuration rules collection.");
                }
                rule.SetPropertyValue(Constants.VCppIncludesKey, $"$({Constants.PLCnextIncludesKey})");

                IVCRulePropertyStorage clRule = config.Rules.Item(Constants.CLRuleName);
                if (clRule == null)
                {
                    MessageBox.Show("CL rule was not found in configuration rules collection.");
                }
                clRule.SetPropertyValue(Constants.VCPreprocessorsKey, $"$({Constants.PLCnextMacrosKey})");
            }
        }
        public static void UpdateIncludesAndMacrosForExistingProject(VCProject vcProject,
                                                                     IEnumerable<CompilerMacroResult> oldMacros,
                                                                     CompilerSpecificationCommandResult newCompilerSpecs,
                                                                     IEnumerable<string> oldIncludes,
                                                                     ProjectInformationCommandResult newProjectInformation)
        {
            var (newMacros, newIncludes) =
                    ProjectIncludesManager.FindMacrosAndIncludesForMinTarget(newCompilerSpecs,
                                                                                 newProjectInformation);
            if (newMacros == null)
                newMacros = Enumerable.Empty<CompilerMacroResult>();
            if (newIncludes == null)
                newIncludes = Enumerable.Empty<string>();

            foreach (VCConfiguration2 config in vcProject.Configurations)
            {
                IVCRulePropertyStorage plcnextCommonPropertiesRule = config.Rules.Item(Constants.PLCnextRuleName);

                CheckIncludesVariableIsInProjectIncludes(config);
                CheckMacrosVariableIsInProjectMacros(config);
                
                DeleteObsoleteIncludes();
                DeleteObsoleteMacros();

                plcnextCommonPropertiesRule.SetPropertyValue(Constants.PLCnextIncludesKey, newIncludes.Any() ?
                                                                                           string.Join(";", newIncludes) :
                                                                                           string.Empty);
               
                plcnextCommonPropertiesRule.SetPropertyValue(Constants.PLCnextMacrosKey, newMacros.Any() ?
                    string.Join(";", newMacros.Select(m => m.Name + (string.IsNullOrEmpty(m.Value?.Trim()) ? string.Empty : ("=" + m.Value))))
                    : string.Empty);


                void DeleteObsoleteIncludes()
                {
                    string savedIncludes = plcnextCommonPropertiesRule.GetUnevaluatedPropertyValue(Constants.PLCnextIncludesKey);
                    if (!string.IsNullOrEmpty(savedIncludes) || oldIncludes == null || !oldIncludes.Any())
                        return;

                    IVCRulePropertyStorage rule = config.Rules.Item(Constants.VCppIncludesRuleName);
                    IEnumerable<string> currentIncludes = rule.GetUnevaluatedPropertyValue(Constants.VCppIncludesKey)
                                                              .Split(';')
                                                              .Where(i => !oldIncludes.Contains(i));
                    rule.SetPropertyValue(Constants.VCppIncludesKey, currentIncludes.Any() ?
                                          string.Join(";", currentIncludes) : string.Empty);
                }

                void DeleteObsoleteMacros()
                {
                    string savedMacros = plcnextCommonPropertiesRule.GetUnevaluatedPropertyValue(Constants.PLCnextMacrosKey);
                    if (!string.IsNullOrEmpty(savedMacros) || oldMacros == null || !oldMacros.Any())
                        return;

                    IVCRulePropertyStorage clRule = config.Rules.Item(Constants.CLRuleName);
                    IEnumerable<CompilerMacroResult> currentMacros =
                        clRule.GetUnevaluatedPropertyValue(Constants.VCPreprocessorsKey)
                              .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                              .Select(m => m.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries))
                              .Select(m => new CompilerMacroResult { Name = m[0], Value = m.Length == 2 ? m[1].Trim() : null });
                    currentMacros = currentMacros.Where(m => !oldMacros.Any(y => y.Name.Equals(m.Name) &&
                                                                                   (y.Value != null ?
                                                                                   (m.Value == null ?
                                                                                        string.IsNullOrEmpty(y.Value.Trim()) :
                                                                                        y.Value.Trim().Equals(m.Value.Trim())) :
                                                                                   string.IsNullOrEmpty(m.Value?.Trim()))));
                    IEnumerable<string> macros = currentMacros.Select(m => m.Name + (string.IsNullOrEmpty(m.Value?.Trim()) ? string.Empty : ("=" + m.Value)));
                    clRule.SetPropertyValue(Constants.VCPreprocessorsKey, macros.Any() ? string.Join(";", macros) : string.Empty);

                }
            }


            void CheckIncludesVariableIsInProjectIncludes(VCConfiguration2 config)
            {
                IVCRulePropertyStorage rule = config.Rules.Item(Constants.VCppIncludesRuleName);
                string currentIncludes = rule.GetUnevaluatedPropertyValue(Constants.VCppIncludesKey);

                if (!currentIncludes.Split(';').Where(i => i.Trim().Equals($"$({Constants.PLCnextIncludesKey})", StringComparison.OrdinalIgnoreCase)).Any())
                {
                    string includes = string.IsNullOrEmpty(currentIncludes) ? $"$({Constants.PLCnextIncludesKey})"
                                                        : string.Join(";", currentIncludes, $"$({Constants.PLCnextIncludesKey})");
                    rule.SetPropertyValue(Constants.VCppIncludesKey, includes);
                }
            }

            void CheckMacrosVariableIsInProjectMacros(VCConfiguration2 config)
            {
                IVCRulePropertyStorage clRule = config.Rules.Item(Constants.CLRuleName);
                string currentMacros =
                    clRule.GetUnevaluatedPropertyValue(Constants.VCPreprocessorsKey);

                if (!currentMacros.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                                 .Where(m => m.Trim()
                                              .Equals($"$({Constants.PLCnextMacrosKey})", StringComparison.OrdinalIgnoreCase))
                                              .Any())
                {
                    string macros = string.IsNullOrEmpty(currentMacros) ? $"$({Constants.PLCnextMacrosKey})"
                                                       : string.Join(";", currentMacros, $"$({Constants.PLCnextMacrosKey})");
                    clRule.SetPropertyValue(Constants.VCPreprocessorsKey, macros);
                }
            }
        }
    }
}
