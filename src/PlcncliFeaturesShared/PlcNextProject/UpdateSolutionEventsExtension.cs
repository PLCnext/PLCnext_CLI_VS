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
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using PlcncliFeatures.PlcNextProject.ProjectConfigWindow;
using System;

namespace PlcncliFeatures.PlcNextProject
{
    class UpdateSolutionEventsExtension : IVsUpdateSolutionEvents
    {
        private readonly string pwPattern = "plcncli_deploy_pw_{0}";
        private DTE2 dte;
        private IVsSolutionPersistence solutionPersistence;

        public async System.Threading.Tasks.Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
#pragma warning disable VSSDK006 // Check whether the result of GetService calls is null
            dte = await package.GetServiceAsync(typeof(DTE)) as DTE2;
#pragma warning restore VSSDK006 // Check whether the result of GetService calls is null
            solutionPersistence = Package.GetGlobalService(typeof(SVsSolutionPersistence)) as IVsSolutionPersistence;
            IVsSolutionBuildManager buildManager = await package.GetServiceAsync(typeof(SVsSolutionBuildManager)) as IVsSolutionBuildManager;
            buildManager?.AdviseUpdateSolutionEvents(this, out _);
        }

        public int UpdateSolution_Begin(ref int pfCancelUpdate)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            PasswordService passwordService = new PasswordService(solutionPersistence);

            Array selectedItems = dte?.ToolWindows?.SolutionExplorer?.SelectedItems as Array;
            if (selectedItems == null)
            {
                return VSConstants.S_OK;
            }

            foreach (UIHierarchyItem item in selectedItems)
            {
                if (item.Object is Project project)
                {
                    SaveProjectPasswordInEnvironment(project);
                }
                else if (item.Object is Solution solution)
                {
                    Projects projects = solution?.Projects;
                    if (projects != null)
                    {
                        foreach (EnvDTE.Project proj in projects)
                        {
                            SaveProjectPasswordInEnvironment(proj);
                        }
                    }
                }
            }

            void SaveProjectPasswordInEnvironment(EnvDTE.Project project)
            {
                ProjectConfiguration config = ConfigFileProvider.LoadFromConfig(project.FullName);
                if (config.Sign)
                {
                    string password = string.Empty;

                    if (ConfigFileProvider.IsConfiguredToSignWithPEMFiles(config)
                        && !ConfigFileProvider.IsConfiguredToSignWithPKCS12(config))
                    {
                        password = passwordService.GetProjectPassword(project.Name, PasswordPersistFileType.PEMKeyFile);
                    }
                    else if (!ConfigFileProvider.IsConfiguredToSignWithPEMFiles(config)
                            && ConfigFileProvider.IsConfiguredToSignWithPKCS12(config))
                    {
                        password = passwordService.GetProjectPassword(project.Name, PasswordPersistFileType.PKCS12);
                    }
                    if (!string.IsNullOrEmpty(password))
                    {
                        Environment.SetEnvironmentVariable(string.Format(pwPattern, project.Name), password);
                    }
                }
            }

            return VSConstants.S_OK;
        }

        public int UpdateSolution_Done(int fSucceeded, int fModified, int fCancelCommand)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            DeleteAllPasswordEnvVars();
            return VSConstants.S_OK;
        }

        public int UpdateSolution_StartUpdate(ref int pfCancelUpdate)
        {
            return VSConstants.S_OK;
        }

        public int UpdateSolution_Cancel()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            DeleteAllPasswordEnvVars();
            return VSConstants.S_OK;
        }

        public int OnActiveProjectCfgChange(IVsHierarchy pIVsHierarchy)
        {
            return VSConstants.S_OK;
        }

        private void DeleteAllPasswordEnvVars()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            Solution solution = dte?.Solution;
            Projects projects = solution?.Projects;

            if (projects != null)
            {
                foreach (EnvDTE.Project project in projects)
                {
                    Environment.SetEnvironmentVariable(string.Format(pwPattern, project.Name), null);
                }
            }
        }
    }
}
