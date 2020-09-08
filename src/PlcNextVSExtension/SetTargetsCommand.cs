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
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.VCProjectEngine;
using PlcncliServices.CommandResults;
using PlcncliServices.PLCnCLI;
using PlcNextVSExtension.ProjectPropertyEditor;
using PlcNextVSExtension.Properties;
using Path = System.IO.Path;
using Task = System.Threading.Tasks.Task;

namespace PlcNextVSExtension
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class SetTargetsCommand
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
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="SetTargetsCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private SetTargetsCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new OleMenuCommand(this.SetTargets, this.ChangeHandler, this.QueryStatus, menuCommandID);
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
        /// Gets the service provider from the owner package.
        /// </summary>
        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD010:Invoke single-threaded types on Main thread", Justification = "Checked in GetProject()")]
        private void QueryStatus(object sender, EventArgs e)
        {
            if (sender is OleMenuCommand cmd)
            {
                Project project = GetProject();
                VCProject p = project.Object as VCProject;
                VCConfiguration configuration = p.ActiveConfiguration;
                IVCRulePropertyStorage plcnextRule = configuration.Rules.Item("PLCnextCommonProperties");
                if(plcnextRule != null)
                { 
                    string projectType = plcnextRule.GetUnevaluatedPropertyValue("ProjectType_");
                    if (!string.IsNullOrEmpty(projectType))
                    {
                        cmd.Visible = true;
                        return;
                    }
                }
                

                cmd.Visible = false;
            }
        }

        private void ChangeHandler(object sender, EventArgs e)
        {

        }

        private Project GetProject()
        { 
            ThreadHelper.ThrowIfNotOnUIThread();

            //get project location
            IServiceProvider serviceProvider = package;
            DTE2 dte = (DTE2)serviceProvider.GetService(typeof(DTE));
            var sel = dte.ActiveWindow.Selection;
            Array selectedItems = (Array)dte.ToolWindows.SolutionExplorer.SelectedItems;
            if (selectedItems == null || selectedItems.Length > 1)
                return null;
            UIHierarchyItem x = selectedItems.GetValue(0) as UIHierarchyItem;
            return x.Object as Project;
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
                if (serviceProvider.GetService(typeof(SPlcncliCommunication)) is IPlcncliCommunication cliCommunication)
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
                        projectInformationBefore = cliCommunication.ConvertToTypedCommandResult<ProjectInformationCommandResult>(ex.InfoMessages);
                    }

                    CompilerSpecificationCommandResult compilerSpecsBefore = null;
                    try
                    { 
                        compilerSpecsBefore = cliCommunication.ExecuteCommand(Resources.Command_get_compiler_specifications, null,
                            typeof(CompilerSpecificationCommandResult), Resources.Option_get_compiler_specifications_project,
                            $"\"{projectDirectory}\"") as CompilerSpecificationCommandResult;
                    }
                    catch(PlcncliException ex)
                    {
                        compilerSpecsBefore = cliCommunication.ConvertToTypedCommandResult<CompilerSpecificationCommandResult>(ex.InfoMessages);
                    }

                    IEnumerable<CompilerMacroResult> macrosBefore = compilerSpecsBefore?.Specifications.FirstOrDefault()
                        ?.CompilerMacros.Where(m => !m.Name.StartsWith("__has_include("));

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

                    ProjectInformationCommandResult projectInformationAfter = null;
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
                    CompilerSpecificationCommandResult compilerSpecsAfter = null;
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
                    IEnumerable<CompilerMacroResult> macrosAfter = compilerSpecsAfter?.Specifications.FirstOrDefault()
                        ?.CompilerMacros.Where(m => !m.Name.StartsWith("__has_include("));


                    VCProject p = project.Object as VCProject;
                    foreach (VCConfiguration2 config in p.Configurations)
                    {
                        IVCRulePropertyStorage rule = config.Rules.Item("ConfigurationDirectories");
                        string propKey = "IncludePath";

                        string currentIncludes = rule.GetUnevaluatedPropertyValue(propKey);
                        IEnumerable<string> currentIncludeEnum = currentIncludes.Split(';');
                        IEnumerable<string> includesEnum = currentIncludeEnum.Where(value =>
                                !projectInformationBefore.IncludePaths.Select(path => path.PathValue).Contains(value))
                            .Concat(projectInformationAfter.IncludePaths.Select(path => path.PathValue));

                        string includes = string.Join(";", includesEnum);

                        rule.SetPropertyValue(propKey, includes);

                        
                        IVCRulePropertyStorage clRule = config.Rules.Item("CL");
                        string macroKey = "PreprocessorDefinitions";
                        string currentMacros = clRule.GetUnevaluatedPropertyValue(macroKey);
                        IEnumerable<(string, string)> currentMacroEnum = currentMacros.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(m => m.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries))
                            .Select(m => (m[0], m.Length == 2 ? m[1] : string.Empty));

                        if (macrosBefore == null && macrosAfter == null)
                            continue;

                        if (macrosAfter == null)
                            macrosAfter = Enumerable.Empty< CompilerMacroResult>();

                        IEnumerable<(string, string)> macroEnum = macrosAfter.Select(m => (m.Name, m.Value));

                        if (macrosBefore != null)
                        { 
                         macroEnum = macroEnum.Concat(currentMacroEnum.Where(value =>
                            !macrosBefore.Where(m => m.Name.Equals(value.Item1) && m.Value.Equals(value.Item2)).Any()));
                        }
                        string macros = string.Join(";", macroEnum.Select(m => m.Item1 + (m.Item2 != null ? "=" + m.Item2 : "")));

                        clRule.SetPropertyValue(macroKey, macros);
                    }
                }
            }
        }
    }
}
