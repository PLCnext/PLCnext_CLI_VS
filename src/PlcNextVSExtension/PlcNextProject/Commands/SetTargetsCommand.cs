﻿#region Copyright
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
using PlcNextVSExtension.PlcNextProject.ProjectTargetsEditor;
using PlcNextVSExtension.Properties;
using Path = System.IO.Path;
using Task = System.Threading.Tasks.Task;

namespace PlcNextVSExtension.PlcNextProject.Commands
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class SetTargetsCommand : PlcNextCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("eba1dd59-aabe-4863-b7a6-e018ccba5c32");

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

            //show dialog
            ProjectTargetValueEditorModel model = new ProjectTargetValueEditorModel(serviceProvider, projectDirectory);
            ProjectTargetValueEditorViewModel viewModel = new ProjectTargetValueEditorViewModel(model);
            ProjectTargetValueEditorView view = new ProjectTargetValueEditorView(viewModel);
            view.ShowModal();

            if (view.DialogResult == true)
            {
                if (!(serviceProvider.GetService(typeof(SPlcncliCommunication)) is IPlcncliCommunication cliCommunication))
                {
                    MessageBox.Show("Could not set project targets because no plcncli communication found.");
                    return;
                }

                VCProject p = project.Object as VCProject;
                bool needProjectInformation = false;
                bool needCompilerInformation = false;

                var (includesSaved, macrosSaved) = ProjectIncludesManager.CheckSavedIncludesAndMacros(p);
                needProjectInformation = !includesSaved;
                needCompilerInformation = !macrosSaved;
                
                IEnumerable<string> includesBefore = null;
                IEnumerable<CompilerMacroResult> macrosBefore = Enumerable.Empty<CompilerMacroResult>();
                CompilerSpecificationCommandResult compilerSpecsAfter = null;
                ProjectInformationCommandResult projectInformationAfter = null;
                

                IVsTaskStatusCenterService taskCenter = Package.GetGlobalService(typeof(SVsTaskStatusCenterService)) as IVsTaskStatusCenterService;
                ITaskHandler taskHandler = taskCenter.PreRegister(
                    new TaskHandlerOptions() { Title = $"Setting project targets" },
                    new TaskProgressData());
                Task task = Task.Run(async () =>
                {
                    try
                    {
                        if (needProjectInformation)
                        {
                            ProjectInformationCommandResult projectInformationBefore = null;
                            try
                            {
                                projectInformationBefore = cliCommunication.ExecuteCommand(Resources.Command_get_project_information, null,
                                    typeof(ProjectInformationCommandResult), Resources.Option_get_project_information_project,
                                    $"\"{projectDirectory}\"") as ProjectInformationCommandResult;
                            }
                            catch (PlcncliException ex)
                            {
                                try
                                {
                                    projectInformationBefore = cliCommunication.ConvertToTypedCommandResult<ProjectInformationCommandResult>(ex.InfoMessages);
                                }
                                catch (PlcncliException) {}
                            }

                            includesBefore = projectInformationBefore?.IncludePaths.Select(x => x.PathValue);
                            if (includesBefore == null)
                            {
                                includesBefore = Enumerable.Empty<string>();
                            }
                        }

                        if (needCompilerInformation)
                        {
                            CompilerSpecificationCommandResult compilerSpecsBefore = null;
                            try
                            {
                                compilerSpecsBefore = cliCommunication.ExecuteCommand(Resources.Command_get_compiler_specifications, null,
                                    typeof(CompilerSpecificationCommandResult), Resources.Option_get_compiler_specifications_project,
                                    $"\"{projectDirectory}\"") as CompilerSpecificationCommandResult;
                            }
                            catch (PlcncliException ex)
                            {
                                compilerSpecsBefore = cliCommunication.ConvertToTypedCommandResult<CompilerSpecificationCommandResult>(ex.InfoMessages);
                            }
                            macrosBefore = compilerSpecsBefore?.Specifications.FirstOrDefault()
                                ?.CompilerMacros.Where(m => !m.Name.StartsWith("__has_include("))??Enumerable.Empty<CompilerMacroResult>();
                        }

                        SetTargets();

                        try
                        {
                            projectInformationAfter = cliCommunication.ExecuteCommand(Resources.Command_get_project_information, null,
                                typeof(ProjectInformationCommandResult), Resources.Option_get_project_information_project,
                                $"\"{projectDirectory}\"") as ProjectInformationCommandResult;
                        }
                        catch (PlcncliException ex)
                        {
                            projectInformationAfter = cliCommunication.ConvertToTypedCommandResult<ProjectInformationCommandResult>(ex.InfoMessages);
                        }
                        try
                        {
                            compilerSpecsAfter = cliCommunication.ExecuteCommand(Resources.Command_get_compiler_specifications, null,
                             typeof(CompilerSpecificationCommandResult), Resources.Option_get_compiler_specifications_project,
                             $"\"{projectDirectory}\"") as CompilerSpecificationCommandResult;
                        }
                        catch (PlcncliException ex)
                        {
                            compilerSpecsAfter = cliCommunication.ConvertToTypedCommandResult<CompilerSpecificationCommandResult>(ex.InfoMessages);
                        }

                        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                        ProjectConfigurationManager.CreateConfigurationsForAllProjectTargets
                            (projectInformationAfter?.Targets.Select(t => t.GetNameFormattedForCommandLine()), project);


                        ProjectIncludesManager.UpdateIncludesAndMacrosForExistingProject(p, macrosBefore, 
                                                compilerSpecsAfter, includesBefore, projectInformationAfter);

                        p.Save();

                        void SetTargets()
                        {
                            foreach (TargetResult target in model.TargetsToAdd)
                            {
                                cliCommunication.ExecuteCommand(Resources.Command_set_target, null, null,
                                Resources.Option_set_target_add, Resources.Option_set_target_project,
                                $"\"{projectDirectory}\"",
                                Resources.Option_set_target_name, target.Name, Resources.Option_set_target_version,
                                $"\"{target.LongVersion}\"");
                            }

                            foreach (TargetResult target in model.TargetsToRemove)
                            {
                                cliCommunication.ExecuteCommand(Resources.Command_set_target, null, null,
                                    Resources.Option_set_target_remove, Resources.Option_set_target_project,
                                    $"\"{projectDirectory}\"",
                                    Resources.Option_set_target_name, target.Name, Resources.Option_set_target_version,
                                    $"\"{target.LongVersion}\"");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message+ex.StackTrace??string.Empty, "Exception during setting of targets");
                        throw ex;
                    }
                });
                taskHandler.RegisterTask(task);
            }
        }
    }
}
