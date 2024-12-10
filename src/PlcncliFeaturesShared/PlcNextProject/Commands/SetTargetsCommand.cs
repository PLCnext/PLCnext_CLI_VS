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
using System.ComponentModel.Design;
using System.Linq;
using System.Windows;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TaskStatusCenter;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.VCProjectEngine;
using PlcncliServices.CommandResults;
using PlcncliServices.PLCnCLI;
using PlcncliFeatures.PlcNextProject.ProjectTargetsEditor;
using Path = System.IO.Path;
using Task = System.Threading.Tasks.Task;
using Constants = PlcncliCommonUtils.Constants;
using PlcncliCommonUtils;
using EnvDTE80;
using System.Threading.Tasks;

namespace PlcncliFeatures.PlcNextProject.Commands
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class SetTargetsCommand : PlcNextCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0101;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("d03b062f-5831-4deb-b619-beb902e75a3e");

        /// <summary>
        /// Initializes a new instance of the <see cref="SetTargetsCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private SetTargetsCommand(AsyncPackage package, OleMenuCommandService commandService) : base(package)
        {
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new OleMenuCommand(this.SetTargets, this.ChangeHandler, this.QueryStatus, menuCommandID) { Visible = false };
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static SetTargetsCommand Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread - the call to AddCommand in SetTargetsCommand's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new SetTargetsCommand(package, commandService);
        }

        private void ChangeHandler(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void SetTargets(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            IServiceProvider serviceProvider = package;
            Project project = GetProject();
            if (project == null)
                return;
            string projectDirectory = Path.GetDirectoryName(project.FullName);

            if (!(serviceProvider.GetService(typeof(SPlcncliCommunication)) is IPlcncliCommunication cliCommunication))
            {
                MessageBox.Show("Could not set project targets because no plcncli communication found.", 
                    "PLCnCLI communication problem", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            //show dialog
            ProjectTargetValueEditorModel model = new ProjectTargetValueEditorModel(cliCommunication, projectDirectory);
            ProjectTargetValueEditorViewModel viewModel = new ProjectTargetValueEditorViewModel(model);
            ProjectTargetValueEditorView view = new ProjectTargetValueEditorView(viewModel);
            view.ShowModal();

            if (view.DialogResult == true)
            {
                VCProject p = project.Object as VCProject;
                if (ProjectIsDirty())
                {
                    MessageBoxResult result = MessageBox.Show("Do you want to save the current project?", "Project changes detected",
                                                               MessageBoxButton.OKCancel, MessageBoxImage.Question);
                    if (result == MessageBoxResult.Cancel)
                    {
                        return;
                    }
                }
                bool needProjectInformation = false;
                bool needCompilerInformation = false;

                var (includesSaved, macrosSaved) = ProjectIncludesManager.CheckSavedIncludesAndMacros(p);
                needProjectInformation = !includesSaved;
                needCompilerInformation = !macrosSaved;
                
                IEnumerable<string> includesBefore = null;
                IEnumerable<CompilerMacroResult> macrosBefore = Enumerable.Empty<CompilerMacroResult>();
                CompilerSpecificationCommandResult compilerSpecsAfter = null;
                ProjectInformationCommandResult projectInformationAfter = null;
                

                ThreadHelper.JoinableTaskFactory.Run(
                    "Setting project targets",
                    async (progress) =>
                {
                    try
                    {
                        if (needProjectInformation)
                        {
                            progress.Report(new ThreadedWaitDialogProgressData("Fetching project information."));

                            ProjectInformationCommandResult projectInformationBefore = null;
                            try
                            {
                                projectInformationBefore = cliCommunication.ExecuteCommand(
                                    Constants.Command_get_project_information, 
                                    null,
                                    typeof(ProjectInformationCommandResult), 
                                    Constants.Option_get_project_information_project, 
                                    $"\"{projectDirectory}\"",
                                    Constants.Option_get_project_information_buildtype, 
                                    p.ActiveConfiguration?.ConfigurationName?.StartsWith("Debug", StringComparison.OrdinalIgnoreCase) == true ? "Debug" : "Release"
                                    ) as ProjectInformationCommandResult;
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

                        if (needCompilerInformation)
                        {
                            progress.Report(new ThreadedWaitDialogProgressData("Fetching compiler information."));

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
                        
                        progress.Report(new ThreadedWaitDialogProgressData("Setting targets."));

                        SetTargets();

                        progress.Report(new ThreadedWaitDialogProgressData("Fetching project information."));
                        try
                        {
                            projectInformationAfter = cliCommunication.ExecuteCommand(
                                Constants.Command_get_project_information, 
                                null,
                                typeof(ProjectInformationCommandResult), 
                                Constants.Option_get_project_information_project,
                                $"\"{projectDirectory}\"",
                                Constants.Option_get_project_information_buildtype,
                                p.ActiveConfiguration?.ConfigurationName?.StartsWith("Debug", StringComparison.OrdinalIgnoreCase) == true ? "Debug" : "Release"
                                ) as ProjectInformationCommandResult;
                        }
                        catch (PlcncliException ex)
                        {
                            projectInformationAfter = cliCommunication.ConvertToTypedCommandResult<ProjectInformationCommandResult>(ex.InfoMessages);
                        }

                        progress.Report(new ThreadedWaitDialogProgressData("Fetching compiler information."));
                        try
                        {
                            compilerSpecsAfter = cliCommunication.ExecuteCommand(Constants.Command_get_compiler_specifications, null,
                             typeof(CompilerSpecificationCommandResult), Constants.Option_get_compiler_specifications_project,
                             $"\"{projectDirectory}\"") as CompilerSpecificationCommandResult;
                        }
                        catch (PlcncliException ex)
                        {
                            compilerSpecsAfter = cliCommunication.ConvertToTypedCommandResult<CompilerSpecificationCommandResult>(ex.InfoMessages);
                        }

                        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                        progress.Report(new ThreadedWaitDialogProgressData("Create configurations."));
                        ProjectConfigurationManager.CreateConfigurationsForAllProjectTargets
                            (projectInformationAfter?.Targets.Select(t => t.GetNameFormattedForCommandLine()), project);


                        progress.Report(new ThreadedWaitDialogProgressData("Update includes."));
                        ProjectIncludesManager.UpdateIncludesAndMacrosForExistingProject(p, macrosBefore, 
                                                compilerSpecsAfter, includesBefore, projectInformationAfter);

                        progress.Report(new ThreadedWaitDialogProgressData("Reloading project."));
                        p.SaveUserFile();
                        p.Save();
                        p.LoadUserFile();

                        Solution solution = project.DTE.Solution;
                        string fileName = solution.FileName;
                        if (string.IsNullOrEmpty(fileName))
                        {
                            solution.SaveAs(project.Name);
                            fileName = solution.FileName;
                        }
                        solution.Close(true);
                        solution.Open(fileName);

                        void SetTargets()
                        {
                            foreach (TargetResult target in model.TargetsToAdd)
                            {
                                string[] args = new string[] { Constants.Option_set_target_add,
                                    Constants.Option_set_target_project, $"\"{projectDirectory}\"",
                                    Constants.Option_set_target_name, target.Name};
                                if (!string.IsNullOrEmpty(target.LongVersion))
                                {
                                    args = args.Append(Constants.Option_set_target_version).Append($"\"{target.LongVersion}\"").ToArray();
                                }
                                _ = cliCommunication.ExecuteCommand(Constants.Command_set_target, null, null, args);
                            }

                            foreach (TargetResult target in model.TargetsToRemove)
                            {
                                string[] args = new string[] { Constants.Option_set_target_remove,
                                            Constants.Option_set_target_project, $"\"{projectDirectory}\"",
                                            Constants.Option_set_target_name, target.Name};
                                if (!string.IsNullOrEmpty(target.LongVersion))
                                {
                                    args = args.Append(Constants.Option_set_target_version).Append($"\"{target.LongVersion}\"").ToArray();
                                }
                                _ = cliCommunication.ExecuteCommand(Constants.Command_set_target, null, null, args);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _ = MessageBox.Show(ex.Message + ex.StackTrace ?? string.Empty, "Exception during setting of targets");
                        throw ex;
                    }
                });

                IVsTaskStatusCenterService taskCenter = Package.GetGlobalService(typeof(SVsTaskStatusCenterService)) as IVsTaskStatusCenterService;
                taskCenter.PreRegister(
                    new TaskHandlerOptions() { Title = $"Updating intellisense" },
                    new TaskProgressData())
                    .RegisterTask(Task.Run(async () =>
                    {
                        try
                        {
                            System.Threading.Thread.Sleep(1000);
#pragma warning disable VSSDK006 // Check services exist
#pragma warning disable VSTHRD010 // Invoke single-threaded types on Main thread
                            DTE2 dte = (DTE2)serviceProvider.GetService(typeof(DTE));
#pragma warning restore VSTHRD010 // Invoke single-threaded types on Main thread
#pragma warning restore VSSDK006 // Check services exist
                            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                            Array selectedItems = (Array)dte?.ToolWindows.SolutionExplorer.SelectedItems;
                            if (selectedItems == null || selectedItems.Length != 1)
                                return;
                            UIHierarchyItem hi = selectedItems.GetValue(0) as UIHierarchyItem;
                            project = hi?.Object as Project;
                            dte.ToolWindows.SolutionExplorer.Parent.Activate();
                            dte.ExecuteCommand("Project.RescanSolution");
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show((ex.Message ?? string.Empty) + Environment.NewLine +
                                "Rescanning solution automatically failed. Please rescan solution manually (Project -> Rescan Solution )", "Exception during solution scanning");
                            throw ex;
                        }
                    }));

                bool ProjectIsDirty()
                {
                    if (!project.Saved)
                    {
                        return true;
                    }

                    if (project.DTE?.Solution?.Saved == false)
                    {
                        return true;
                    }

                    foreach (ProjectItem item in project.ProjectItems)
                    {
                        if (item?.Saved == false)
                        {
                            return true;
                        }
                    }
                    return false;
                }
            }
        }
    }
}
