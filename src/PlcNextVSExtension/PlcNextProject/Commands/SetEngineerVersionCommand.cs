#region Copyright
///////////////////////////////////////////////////////////////////////////////
//
//  Copyright (c) Phoenix Contact GmbH & Co KG
//  This software is licensed under Apache-2.0
//
///////////////////////////////////////////////////////////////////////////////
#endregion

using EnvDTE;
using Microsoft.VisualStudio.Shell;
using PlcNextVSExtension.PlcNextProject.EngineerVersionEditor;
using System;
using System.ComponentModel.Design;
using System.IO;
using Task = System.Threading.Tasks.Task;

namespace PlcNextVSExtension.PlcNextProject.Commands
{

    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class SetEngineerVersionCommand : PlcNextCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0X1300;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("eba1dd59-aabe-4863-b7a6-e018ccba5c32");

        /// <summary>
        /// Initializes a new instance of the <see cref="SetEngineerVersionCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private SetEngineerVersionCommand(AsyncPackage package, OleMenuCommandService commandService) : base(package)
        {
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new OleMenuCommand(ExecuteCommand, ChangeHandler, QueryStatus, menuCommandID) { Visible = false };
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static SetEngineerVersionCommand Instance
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
            // Switch to the main thread - the call to AddCommand in SetEngineerVersionCommand's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new SetEngineerVersionCommand(package, commandService);
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
            ThreadHelper.ThrowIfNotOnUIThread();
            Project project = GetProject();
            if (project == null)
                return;

            SetEngineerVersionEditorViewModel viewModel = new SetEngineerVersionEditorViewModel(project);
            SetEngineerVersionEditorView view = new SetEngineerVersionEditorView(viewModel);
            view.ShowModal();
        }
    }
}
