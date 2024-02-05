#region Copyright
///////////////////////////////////////////////////////////////////////////////
//
//  Copyright (c) Phoenix Contact GmbH & Co KG
//  This software is licensed under Apache-2.0
//
///////////////////////////////////////////////////////////////////////////////
#endregion

using Microsoft.Build.Construction;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.VCProjectEngine;
using PlcncliServices.CommandResults;
using PlcncliServices.PLCnCLI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace PlcncliCommonUtils
{
    public static class ProjectIncludesManager
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


        public static (IEnumerable<CompilerMacroResult> macros, IEnumerable<string> includes) 
            FindMacrosAndIncludesForMinTarget(CompilerSpecificationCommandResult compilerSpecsCommandResult,
                                           ProjectInformationCommandResult projectInformation)
        {
            IEnumerable<CompilerMacroResult> macros = Enumerable.Empty<CompilerMacroResult>();
            IEnumerable<string> includes = Enumerable.Empty < string>();

            TargetResult minCompilerTarget = FindMinTargetForMacros(compilerSpecsCommandResult);
            if (minCompilerTarget != null)
            {
                macros = GetMacrosForTarget(minCompilerTarget, compilerSpecsCommandResult);
            }

            TargetResult minIncludeTarget = FindMinTargetForIncludes(projectInformation);

            includes = GetIncludesForTarget(minIncludeTarget, projectInformation);


            return (macros, includes);
        }

        #region private methods
        private static TargetResult FindMinTargetForMacros(CompilerSpecificationCommandResult compilerSpecsCommandResult)
        {
            return compilerSpecsCommandResult?.Specifications
                                              .SelectMany(x => x.Targets)
                                              .MinTarget();
        }

        private static TargetResult FindMinTargetForIncludes(ProjectInformationCommandResult projectInformation)
        {
            return projectInformation?.Targets
                                      .Where(t => t.Available == true)
                                      .MinTarget();
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
        #endregion
        public static (bool includesSaved, bool macrosSaved)  CheckSavedIncludesAndMacros(VCProject p)
        {
            bool includesSaved = true;
            bool macrosSaved = true;
            foreach (VCConfiguration2 config in p.Configurations as IVCCollection)
            {
                IVCRulePropertyStorage plcnextCommonPropertiesRule = config.Rules.Item(Constants.PLCnextRuleName) as IVCRulePropertyStorage;
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

            foreach (VCConfiguration2 config in vcProject.Configurations as IVCCollection)
            {
                IVCRulePropertyStorage plcnextCommonPropertiesRule = config.Rules.Item(Constants.PLCnextRuleName) as IVCRulePropertyStorage;
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
            }
        }

        public static void AddTargetsFileToOldProjects(ProjectRootElement projectRootElement)
        {
            ICollection<ProjectImportElement> imports = projectRootElement.Imports;
            ProjectPropertyGroupElement userMacrosPropertyGroup = projectRootElement.PropertyGroups.Where(g => g.Label.Equals("UserMacros")).FirstOrDefault();
            ProjectImportElement userFileImport = imports.Where(i => i.Project.Equals("$(VCTargetsPath)\\Microsoft.Cpp.targets")).FirstOrDefault();

            if (userMacrosPropertyGroup != null && !userMacrosPropertyGroup.NextSibling.Equals(userFileImport))
            {
                projectRootElement.RemoveChild(userFileImport);

                projectRootElement.InsertAfterChild(userFileImport, userMacrosPropertyGroup);

                //delete all occurences of PLCnCLIIncludes and PLCnCLIMacros -> saved in .users file now
                IEnumerable<ProjectPropertyElement> elementsToDelete = projectRootElement.PropertyGroups
                                  .Where(g => g.Label.Equals("Configuration"))
                                  .SelectMany(g => g.Properties
                                                    .Where(p => p.Name.Equals("PLCnCLIMacros") || p.Name.Equals("PLCnCLIIncludes")));
                elementsToDelete.ToList().ForEach(e => e.Parent.RemoveChild(e));
            }
        }

        public static void UpdateIncludesAndMacrosForExistingProject(VCProject vcProject,
                                                                     IEnumerable<CompilerMacroResult> oldMacros,
                                                                     CompilerSpecificationCommandResult newCompilerSpecs,
                                                                     IEnumerable<string> oldIncludes,
                                                                     ProjectInformationCommandResult newProjectInformation)
        {
            var (newMacros, newIncludes) =
                    FindMacrosAndIncludesForMinTarget(newCompilerSpecs, newProjectInformation);
            if (newMacros == null)
                newMacros = Enumerable.Empty<CompilerMacroResult>();
            if (newIncludes == null)
                newIncludes = Enumerable.Empty<string>();

            IVCRulePropertyStorage plcnextCommonPropertiesRule;


            foreach (VCConfiguration2 config in vcProject.Configurations as IVCCollection)
            {
                plcnextCommonPropertiesRule = config.Rules.Item(Constants.PLCnextRuleName) as IVCRulePropertyStorage;

                CheckIncludesVariableIsInProjectIncludes(config);
                CheckMacrosVariableIsInProjectMacros(config);
                
                DeleteObsoleteIncludes();
                DeleteObsoleteMacros();

                void DeleteObsoleteIncludes()
                {
                    string savedIncludes = plcnextCommonPropertiesRule.GetUnevaluatedPropertyValue(Constants.PLCnextIncludesKey);
                    if (!string.IsNullOrEmpty(savedIncludes) || oldIncludes == null || !oldIncludes.Any())
                        return;

                    IVCRulePropertyStorage rule = config.Rules.Item(Constants.VCppIncludesRuleName) as IVCRulePropertyStorage;
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

                    IVCRulePropertyStorage clRule = config.Rules.Item(Constants.CLRuleName) as IVCRulePropertyStorage;
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
            plcnextCommonPropertiesRule = vcProject.ActiveConfiguration.Rules.Item(Constants.PLCnextRuleName) as IVCRulePropertyStorage;

            plcnextCommonPropertiesRule.SetPropertyValue(Constants.PLCnextIncludesKey, newIncludes.Any() ?
                                                                                           string.Join(";", newIncludes) :
                                                                                           string.Empty);

            plcnextCommonPropertiesRule.SetPropertyValue(Constants.PLCnextMacrosKey, newMacros.Any() ?
                string.Join(";", newMacros.Select(m => m.Name + (string.IsNullOrEmpty(m.Value?.Trim()) ? string.Empty : ("=" + m.Value))))
                : string.Empty);


            void CheckIncludesVariableIsInProjectIncludes(VCConfiguration2 config)
            {
                IVCRulePropertyStorage rule = config.Rules.Item(Constants.VCppIncludesRuleName) as IVCRulePropertyStorage;
                string currentIncludes = rule.GetUnevaluatedPropertyValue(Constants.VCppIncludesKey);
                string includes = currentIncludes?? string.Empty;

                if (!currentIncludes.Split(';').Where(i => i.Trim().Equals($"$({Constants.PLCnextIncludesKey})", StringComparison.OrdinalIgnoreCase)).Any())
                {
                    includes = string.IsNullOrEmpty(currentIncludes) ? $"$({Constants.PLCnextIncludesKey})"
                                                        : string.Join(";", currentIncludes, $"$({Constants.PLCnextIncludesKey})");
                }
                rule.SetPropertyValue(Constants.VCppIncludesKey, includes);
            }

            void CheckMacrosVariableIsInProjectMacros(VCConfiguration2 config)
            {
                IVCRulePropertyStorage clRule = config.Rules.Item(Constants.CLRuleName) as IVCRulePropertyStorage;
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

        public static void UpdateIncludesAndMacrosInBackground(string projectDirectory, 
                                                               IEnumerable<CompilerMacroResult> oldMacros,
                                                               IEnumerable<string> oldIncludes,
                                                               VCProject vcProject,
                                                               IPlcncliCommunication cliCommunication = null)
        {
            if (cliCommunication == null)
            {
                cliCommunication = Package.GetGlobalService(typeof(SPlcncliCommunication)) as IPlcncliCommunication;
            }
            ThreadHelper.JoinableTaskFactory.Run("Updating includes and macros", async (progress) =>
            {
                ProjectInformationCommandResult projectInformationAfter;
                CompilerSpecificationCommandResult compilerSpecsAfter;
                try
                {
                    progress.Report(new ThreadedWaitDialogProgressData("Fetching project information."));
                    projectInformationAfter = cliCommunication.ExecuteCommand(Constants.Command_get_project_information, null,
                    typeof(ProjectInformationCommandResult), Constants.Option_get_project_information_project,
                    $"\"{projectDirectory}\"") as ProjectInformationCommandResult;
                }
                catch (PlcncliException ex)
                {
                    projectInformationAfter = cliCommunication.ConvertToTypedCommandResult<ProjectInformationCommandResult>(ex.InfoMessages);
                    if (projectInformationAfter == null)
                    {
                        MessageBox.Show("The following problem occured during execution of get project-information.\n" +
                                        "Includes might not be updated correctly.\n Please resolve the problem and update includes again.\n"+ex.Message, "Problem during fetching of project information", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                try
                {
                    progress.Report(new ThreadedWaitDialogProgressData("Fetching compiler information."));
                    compilerSpecsAfter = cliCommunication.ExecuteCommand(Constants.Command_get_compiler_specifications, null,
                 typeof(CompilerSpecificationCommandResult), Constants.Option_get_compiler_specifications_project,
                 $"\"{projectDirectory}\"") as CompilerSpecificationCommandResult;
                }
                catch (PlcncliException ex)
                {
                    compilerSpecsAfter = cliCommunication.ConvertToTypedCommandResult<CompilerSpecificationCommandResult>(ex.InfoMessages);
                }

                progress.Report(new ThreadedWaitDialogProgressData("Computing includes and macros."));

                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                UpdateIncludesAndMacrosForExistingProject(vcProject, oldMacros, compilerSpecsAfter,
                                                                                 oldIncludes, projectInformationAfter);
            });
        }

    }
}
