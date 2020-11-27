﻿#region Copyright
///////////////////////////////////////////////////////////////////////////////
//
//  Copyright (c) Phoenix Contact GmbH & Co KG
//  This software is licensed under Apache-2.0
//
///////////////////////////////////////////////////////////////////////////////
#endregion

using EnvDTE;
using Microsoft.VisualStudio.Shell;
using PlcNextVSExtension.PlcNextProject.ProjectCMakeFlagsEditor;
using System;
using System.ComponentModel.Design;
using System.IO;
using Task = System.Threading.Tasks.Task;

namespace PlcNextVSExtension.PlcNextProject.Commands
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class CMakeFlagsCommand : PlcNextCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0X1200;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("eba1dd59-aabe-4863-b7a6-e018ccba5c32");

        /// <summary>
        /// Initializes a new instance of the <see cref="CMakeFlagsCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private CMakeFlagsCommand(AsyncPackage package, OleMenuCommandService commandService) : base(package)
        {
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new OleMenuCommand(this.ExecuteCommand, this.ChangeHandler, this.QueryStatus, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static CMakeFlagsCommand Instance
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
            // Switch to the main thread - the call to AddCommand in CMakeFlagsCommand's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new CMakeFlagsCommand(package, commandService);
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
        private void ExecuteCommand(object sender, EventArgs e)
        {
            Project project = GetProject();
            if (project == null)
                return;
            string projectDirectory = Path.GetDirectoryName(project.FullName);

            CMakeFlagsEditorViewModel viewModel = new CMakeFlagsEditorViewModel(projectDirectory);
            CMakeFlagsEditorView view = new CMakeFlagsEditorView(viewModel);
            view.ShowModal();
        }
    }
}
