#region Copyright
///////////////////////////////////////////////////////////////////////////////
//
//  Copyright (c) Phoenix Contact GmbH & Co KG
//  This software is licensed under Apache-2.0
//
///////////////////////////////////////////////////////////////////////////////
#endregion

using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.VCProjectEngine;
using PlcncliCommonUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using Constants = PlcncliCommonUtils.Constants;
using Path = System.IO.Path;

namespace PlcncliFeatures.PlcNextProject
{
    public class SolutionLoadService : IVsSolutionEvents, IVsSolutionEvents5
    {
        private IVsSolution solution;
        private readonly Regex PlcncliMacroRegex = new Regex(@"<PLCnCLIMacros(.*/| *)>", RegexOptions.Compiled);
        private readonly Regex PlcncliIncludesRegex = new Regex(@"<PLCnCLIIncludes(.*/| *)>", RegexOptions.Compiled);
        private bool userDecision = false;

        public async System.Threading.Tasks.Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            solution = await package.GetServiceAsync(typeof(SVsSolution)) as IVsSolution;
            solution?.AdviseSolutionEvents(this, out _);
        }

        public int OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (userDecision)
            {
                string projectFilePath = GetProjectFile(pHierarchy);
                if (File.Exists(projectFilePath))
                {
                    string fileContent = File.ReadAllText(projectFilePath);
                    //check if includes update necessary -> user file should contain <PLCnCLIMacros> and <PLCnCLIIncludes>
                    if (fileContent.Contains(Constants.PLCnCLIProjectType))
                    {
                        //user file exists?
                        string userFilePath = projectFilePath + ".user";
                        if (File.Exists(userFilePath))
                        {
                            string userFileContent = File.ReadAllText(userFilePath);
                            if (PlcncliMacroRegex.IsMatch(userFileContent) && PlcncliIncludesRegex.IsMatch(userFileContent))
                            {
                                return VSConstants.S_OK;
                            }
                        }

                        VCProject vcProject = GetProjectObject(pHierarchy);
                        ProjectIncludesManager.UpdateIncludesAndMacrosInBackground(Path.GetDirectoryName(projectFilePath),
                                                                               null, null, vcProject);
                        vcProject.Save();
                        vcProject.SaveUserFile();
                        
                        //string solutionFile;
                        //solution.GetSolutionInfo(out _, out solutionFile, out _);
                        //if (solution?.CloseSolutionElement((int)__VSSLNSAVEOPTIONS.SLNSAVEOPT_SaveIfDirty, null, 0) == VSConstants.S_OK)
                        //{
                        //    solution.OpenSolutionFile(0, solutionFile);
                        //}
                    }
                }
                userDecision = false;
            }
            return VSConstants.S_OK;
        }

        private string GetProjectFile(IVsHierarchy pHierarchy)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            object projectDir;
            pHierarchy.GetProperty((uint)VSConstants.VSITEMID.Root, (int)__VSHPROPID.VSHPROPID_ProjectDir, out projectDir);
            object projectName;
            pHierarchy.GetProperty((uint)VSConstants.VSITEMID.Root, (int)__VSHPROPID.VSHPROPID_Name, out projectName);
            string fileName = string.IsNullOrEmpty((string)projectName) ? string.Empty : (string)projectName + ".vcxproj";
            return string.IsNullOrEmpty((string)projectDir) ? string.Empty : Path.Combine((string)projectDir, fileName);

        }

        private VCProject GetProjectObject(IVsHierarchy pHierarchy)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            object project;
            pHierarchy.GetProperty((uint)VSConstants.VSITEMID.Root, (int)__VSHPROPID.VSHPROPID_ExtObject, out project);

            EnvDTE.Project dteProject = project as EnvDTE.Project;

            if (dteProject == null)
            {
                return null;
            }
            return dteProject.Object as VCProject;
        }
        #region IVSSolutionEvent implementation - empty part
        public int OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy)
        {
            return VSConstants.S_OK;
        }

        public int OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
        {
            return VSConstants.S_OK;
        }

        public int OnQueryCloseSolution(object pUnkReserved, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeCloseSolution(object pUnkReserved)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterCloseSolution(object pUnkReserved)
        {
            return VSConstants.S_OK;
        }
#endregion

        public void OnBeforeOpenProject(ref Guid guidProjectID, ref Guid guidProjectType, string pszFileName)
        {
            string projectFilePath = pszFileName;
            if (File.Exists(projectFilePath))
            {
                string fileContent = File.ReadAllText(projectFilePath);
                
                if (fileContent.Contains(Constants.PLCnCLIProjectType))
                {
                    ProjectRootElement projectRootElement = ProjectRootElement.Open(projectFilePath);

                    //remove direct import of propertiesrulefile (version 20.6 projects)
                    ICollection<ProjectItemGroupElement> itemGroups = projectRootElement.ItemGroups;
                    ProjectItemElement projectItem = itemGroups.SelectMany(group => group.Items)
                              .Where(item => item.Include != null && item.Include.Equals(
                                        "$(MSBuildExtensionsPath)\\PHOENIX CONTACT\\PropertyRules\\PLCnextCommonPropertiesRule.xml"))
                              .FirstOrDefault();
                    if (projectItem != null)
                    {
                        projectItem.Parent.RemoveChild(projectItem);
                    }

                    //change path to plcncli.targets file
                    //<Import Project="$(MSBuildExtensionsPath)/PHOENIX CONTACT/PLCnCLI.targets" />
                    ICollection<ProjectImportElement> imports = projectRootElement.Imports;

                    //plm and acf
                    ProjectImportElement plcncliTargetsImport =
                        imports.Where(i => i.Project.Equals(
                            "$(MSBuildExtensionsPath)/PHOENIX CONTACT/PLCnCLI.targets")).FirstOrDefault();
                    string updatedTargetsImportText = "$(PLCNEXT_TOOLCHAIN_INSTALLDIR)/VSCPP/PLCnCLI.targets";

                    if (plcncliTargetsImport == null)
                    {
                        // consumable
                        plcncliTargetsImport = imports.Where(i => i.Project.Equals(
                            "$(MSBuildExtensionsPath)/PHOENIX CONTACT/PLCnCLIBuild.targets")).FirstOrDefault();
                        updatedTargetsImportText = "$(PLCNEXT_TOOLCHAIN_INSTALLDIR)/VSCPP/PLCnCLIBuild.targets";
                    }

                    ProjectImportElement updatedTargetsImport = null;
                    if (plcncliTargetsImport != null)
                    {
                        projectRootElement.RemoveChild(plcncliTargetsImport);
                        updatedTargetsImport = projectRootElement.CreateImportElement(updatedTargetsImportText);
                        projectRootElement.AppendChild(updatedTargetsImport);
                    }

                    ProjectIncludesManager.AddTargetsFileToOldProjects(projectRootElement);

                    if (projectRootElement.HasUnsavedChanges)
                    {
                        MessageBoxResult dialogResult = MessageBox.Show($"The project {Path.GetFileNameWithoutExtension(projectFilePath)}" +
                        $" was created with an older version of the {PlcncliServices.NamingConstants.TechnologyName} C++ Extension and needs to be converted." +
                        " If the conversion is not done, the project load may fail."+Environment.NewLine+
                        Environment.NewLine+"Do you want to convert the project now?",
                        "Necessary project conversion detected", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        if (dialogResult == MessageBoxResult.Yes)
                        {
                            userDecision = true;
                            projectRootElement.Save();
                        }
                        else
                        {
                            ProjectCollection.GlobalProjectCollection?.TryUnloadProject(projectRootElement);
                        }
                    }
                }
            }
        }
    }
}
