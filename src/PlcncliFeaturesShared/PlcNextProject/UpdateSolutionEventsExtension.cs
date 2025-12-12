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
using System.IO;
using System.Windows;

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
                    if (project == null 
                        || project.FullName == null 
                        || !File.Exists(Path.Combine(Path.GetDirectoryName(project.FullName), "plcnext.proj")))
                    {
                        continue;
                    }
                    if (SaveProjectPasswordInEnvironment(project))
                    {
                        continue;
                    }
                    else
                    {
                        pfCancelUpdate = 1;
                        return VSConstants.E_FAIL;
                    }
                        
                }
                else if (item.Object is Solution solution)
                {
                    Projects projects = solution?.Projects;
                    if (projects != null)
                    {
                        foreach (EnvDTE.Project proj in projects)
                        {
                            if (proj == null 
                                || proj.FullName == null 
                                || !File.Exists(Path.Combine(Path.GetDirectoryName(proj.FullName), "plcnext.proj")))
                            {
                                continue;
                            }
                            if (SaveProjectPasswordInEnvironment(proj))
                            {
                                continue;
                            }

                            pfCancelUpdate = 1;
                            return VSConstants.E_FAIL;

                        }
                    }
                }
            }

            bool SaveProjectPasswordInEnvironment(EnvDTE.Project project)
            {
                IProjectConfiguration config;
                try
                {
                    config = ConfigFileProvider.LoadFromConfig(project.FullName);
                }
                catch (Exception e)
                {
                    _ = MessageBox.Show("Project configuration file 'PLCnextSettings.xml' could not be loaded. " + e.Message,
                        "Invalid Configuration found", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
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
                return true;
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
                    if (project == null 
                        || project.Name == null 
                        || project.FullName == null 
                        || !File.Exists(Path.Combine(Path.GetDirectoryName(project.FullName), "plcnext.proj")))
                    {
                        continue;
                    }
                    Environment.SetEnvironmentVariable(string.Format(pwPattern, project.Name), null);
                }
            }
        }
    }
}
