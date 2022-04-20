#region Copyright
///////////////////////////////////////////////////////////////////////////////
//
//  Copyright (c) Phoenix Contact GmbH & Co KG
//  This software is licensed under Apache-2.0
//
///////////////////////////////////////////////////////////////////////////////
#endregion

using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.VCProjectEngine;
using PlcncliServices.CommandResults;
using PlcncliServices.LocationService;
using PlcncliServices.PLCnCLI;
using System;
using System.Collections.Generic;
using System.Linq;
using Path = System.IO.Path;
using Task = System.Threading.Tasks.Task;
using Constants = PlcncliCommonUtils.Constants;
using PlcncliCommonUtils;

namespace PlcncliFeatures.PlcNextProject.OnDocSaveService
{
    class SaveCMakeListsEventHandler : IVsRunningDocTableEvents3
    {
        private readonly string fileName = "CMakeLists.txt";
        private readonly PlcncliOptionPage optionPage;
        private readonly IPlcncliCommunication cliCommunication;
        private VCProject vcProject = null;
        private string projectDirectory = string.Empty;
        private IncludesAndMacrosWrapper wrapper = null;
        private readonly IVsRunningDocumentTable runningDocumentTable;

        public SaveCMakeListsEventHandler(AsyncPackage package, IVsRunningDocumentTable rdt)
        {
            runningDocumentTable = rdt;
            optionPage = package.GetDialogPage(typeof(PlcncliOptionPage)) as PlcncliOptionPage;
            cliCommunication = Package.GetGlobalService(typeof(SPlcncliCommunication)) as IPlcncliCommunication;
        }

        public int OnAfterFirstDocumentLock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeLastDocumentUnlock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterSave(uint docCookie)
        {
            try
            {
                if (vcProject == null || string.IsNullOrEmpty(projectDirectory) || wrapper == null)
                    return VSConstants.S_OK;

                ProjectIncludesManager.UpdateIncludesAndMacrosInBackground(projectDirectory, wrapper.Macros, wrapper.Includes,
                                                                           vcProject, cliCommunication);
                SaveAndReset();
                return VSConstants.S_OK;

            }
            catch (Exception e)
            {
                Reset();
                try
                {
                    ThreadHelper.ThrowIfNotOnUIThread();
                    IVsActivityLog log = Package.GetGlobalService(typeof(SVsActivityLog)) as IVsActivityLog;
                    log.LogEntry((uint)__ACTIVITYLOG_ENTRYTYPE.ALE_ERROR, this.ToString(),
                        "An error occurred while updating the includes: " + e.Message+e.StackTrace);
                }
                catch (Exception) {/*try to log error in activity log*/}
                return VSConstants.S_OK;
            }

            void SaveAndReset()
            {
                projectDirectory = string.Empty;
                wrapper = null;
                vcProject.Save();
                vcProject = null;
            }
        }

        public int OnAfterAttributeChange(uint docCookie, uint grfAttribs)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeDocumentWindowShow(uint docCookie, int fFirstShow, IVsWindowFrame pFrame)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterDocumentWindowHide(uint docCookie, IVsWindowFrame pFrame)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterAttributeChangeEx(uint docCookie, uint grfAttribs, IVsHierarchy pHierOld, uint itemidOld, string pszMkDocumentOld, IVsHierarchy pHierNew, uint itemidNew, string pszMkDocumentNew)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeSave(uint docCookie)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            try
            {
                string documentPath = GetDocumentInfos();
                string GetDocumentInfos()
                {
                    runningDocumentTable.GetDocumentInfo(docCookie, out uint pgfFlags, out uint pdwReadLocks, out uint pdfEditLocks,
                                                            out string pbstrMkDocument, out IVsHierarchy ppHier, out uint pitemid,
                                                            out IntPtr ppunkDocData);
                    return pbstrMkDocument;
                }
                DTE dte = Package.GetGlobalService(typeof(DTE)) as DTE;
                ProjectItem projectItem = dte.Solution.FindProjectItem(documentPath);
                Project project = projectItem.ContainingProject;
                VCProject p = project.Object as VCProject;
                VCConfiguration configuration = p.ActiveConfiguration;
                IVCRulePropertyStorage plcnextRule = configuration.Rules.Item("PLCnextCommonProperties");
                string projectType = plcnextRule.GetUnevaluatedPropertyValue("ProjectType_");
                if (!string.IsNullOrEmpty(projectType))
                {
                    string name = Path.GetFileName(documentPath);
                    if (name.Equals(fileName))
                    {
                        if (optionPage.AskIncludesUpdate)
                        {
                            UpdateIncludesViewModel viewModel = new UpdateIncludesViewModel(p.Name);
                            UpdateIncludesDialogView view = new UpdateIncludesDialogView(viewModel);
                            bool result = (bool)view.ShowModal();

                            optionPage.UpdateIncludes = result;
                            if (viewModel.RememberDecision)
                            {
                                optionPage.AskIncludesUpdate = false;
                            }
                        }
                        if (optionPage.UpdateIncludes)
                        {
                            UpdateIncludesOnBeforeSave(p, p.ProjectDirectory);
                        }
                    }
                    return VSConstants.S_OK;
                }
            }
            catch (NullReferenceException)
            {
                //do nothing
            }
            return VSConstants.S_OK;
        }

        private void UpdateIncludesOnBeforeSave(VCProject p, string projectDirectory)
        {
            var (includesSaved, macrosSaved) = ProjectIncludesManager.CheckSavedIncludesAndMacros(p);
            IEnumerable<string> includesBefore = null;
            IEnumerable<CompilerMacroResult> macrosBefore = null;
            try
            {
                ThreadHelper.JoinableTaskFactory.Run("Updating includes and macros", async (progress) =>
                {
                    await Task.Run(() => GetIncludesAndMacros());

                    void GetIncludesAndMacros()
                    {

                        if (!includesSaved)
                        {
                            progress.Report(new ThreadedWaitDialogProgressData("Fetching project information"));
                            ProjectInformationCommandResult projectInformationBefore = null;
                            try
                            {
                                projectInformationBefore = cliCommunication.ExecuteCommand(Constants.Command_get_project_information, null,
                                typeof(ProjectInformationCommandResult), Constants.Option_get_project_information_project,
                                $"\"{projectDirectory}\"") as ProjectInformationCommandResult;
                            }
                            catch (PlcncliException ex)
                            {
                                projectInformationBefore = cliCommunication.ConvertToTypedCommandResult<ProjectInformationCommandResult>(ex.InfoMessages);
                            }
                            includesBefore = projectInformationBefore?.IncludePaths.Select(x => x.PathValue);
                            if (includesBefore == null)
                            {
                                includesBefore = Enumerable.Empty<string>();
                            }
                        }

                        if (!macrosSaved)
                        {
                            progress.Report(new ThreadedWaitDialogProgressData("Fetching compiler information"));

                            CompilerSpecificationCommandResult compilerSpecsBefore = null;
                            try
                            {
                                compilerSpecsBefore = cliCommunication.ExecuteCommand(Constants.Command_get_compiler_specifications, null,
                                typeof(CompilerSpecificationCommandResult), Constants.Option_get_compiler_specifications_project,
                                $"\"{projectDirectory}\"") as CompilerSpecificationCommandResult;
                            }
                            catch (PlcncliException ex)
                            {
                                compilerSpecsBefore = cliCommunication.ConvertToTypedCommandResult<CompilerSpecificationCommandResult>(ex.InfoMessages);
                            }
                            macrosBefore = compilerSpecsBefore?.Specifications.FirstOrDefault()
                            ?.CompilerMacros.Where(m => !m.Name.StartsWith("__has_include(")) ?? Enumerable.Empty<CompilerMacroResult>();
                        }
                    }
                });

                this.vcProject = p;
                this.projectDirectory = p.ProjectDirectory;
                this.wrapper = new IncludesAndMacrosWrapper(includesBefore, macrosBefore);
            }
            catch (Exception e)
            {
                Reset();
                try
                {
                    ThreadHelper.ThrowIfNotOnUIThread();
                    IVsActivityLog log = Package.GetGlobalService(typeof(SVsActivityLog)) as IVsActivityLog;
                    log.LogEntry((uint)__ACTIVITYLOG_ENTRYTYPE.ALE_ERROR, this.ToString(),
                        "An error occurred while updating the includes: " + e.Message + e.StackTrace);
                }
                catch (Exception) {/*try to log error in activity log*/}
            }
        }
        

        private void Reset()
        {
            projectDirectory = string.Empty;
            wrapper = null;
            vcProject = null;
        }
    }
}
