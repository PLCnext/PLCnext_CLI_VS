#region Copyright
///////////////////////////////////////////////////////////////////////////////
//
//  Copyright (c) Phoenix Contact GmbH & Co KG
//  This software is licensed under Apache-2.0
//
///////////////////////////////////////////////////////////////////////////////
#endregion

using EnvDTE;
using EnvDTE80;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PlcNextVSExtension
{
    internal class ProjectConfigurationManager
    {
        const string releaseConfigurationNameRaw = "Release {0}";
        const string debugConfigurationNameRaw = "Debug {0}";
        const string targetBuildConfigName = "Target-specific";

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD010:Invoke single-threaded types on Main thread", Justification = "Handled in calling method")]
        private static void CreateConfigurationsForTarget(string target, Project project, SolutionConfiguration2 solutionConfiguration)
        {
            //*****create release and debug project configuration*****
            string releaseConfigurationName = string.Format(releaseConfigurationNameRaw, target);
            string debugConfigurationName = string.Format(debugConfigurationNameRaw, target);

            Array configurationNames = (Array)project.ConfigurationManager.ConfigurationRowNames;
            if (configurationNames.OfType<string>().Where(element => element == releaseConfigurationName).Count() == 0)
            {
                project.ConfigurationManager.AddConfigurationRow(releaseConfigurationName, "Release - all Targets", false);
            }
            if (configurationNames.OfType<string>().Where(element => element == debugConfigurationName).Count() == 0)
            {
                project.ConfigurationManager.AddConfigurationRow(debugConfigurationName, "Debug - all Targets", false);
            }
        }

        public static void CreateConfigurationsForAllProjectTargets(IEnumerable<string> targets, Project project)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            //*****create a 'target-specific build' solution configuration if not available
            
            Solution solution = project.DTE.Solution;

            SolutionConfigurations solutionConfigurations = solution.SolutionBuild.SolutionConfigurations;
            SolutionConfiguration2 targetSpecificConfiguration = solutionConfigurations.OfType<SolutionConfiguration2>()
                .Where(config => config.Name.Equals(targetBuildConfigName)).FirstOrDefault();

            if(targetSpecificConfiguration == null)
            {
                targetSpecificConfiguration = solutionConfigurations.Add(targetBuildConfigName, string.Empty, false) as SolutionConfiguration2;
                if (targetSpecificConfiguration == null)
                    return;
            }

            foreach (string target in targets)
            {
                CreateConfigurationsForTarget(target, project, targetSpecificConfiguration);
            }

            //***** set project configuration for solution configuration

            foreach (SolutionContext context in targetSpecificConfiguration.SolutionContexts)
            {
                if (context.ProjectName == project.UniqueName)
                {
                    if (context.ConfigurationName.Equals("Release - all Targets", StringComparison.OrdinalIgnoreCase)
                        || context.ConfigurationName.Equals("Debug - all Targets", StringComparison.OrdinalIgnoreCase))
                    {
                        context.ConfigurationName = string.Format(releaseConfigurationNameRaw, targets.First());
                        break;
                    }
                }
            }
        }

    }
}
