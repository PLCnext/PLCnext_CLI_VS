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
using Microsoft.VisualStudio.VCProjectEngine;
using PlcncliCommonUtils;
using System;
using System.ComponentModel.Design;
using System.IO;
using System.Threading.Tasks;

namespace PlcncliFeatures.PlcNextProject.Commands
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal class UpdateIncludesCommand : PlcNextCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0012;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("d03b062f-5831-4deb-b619-beb902e75a3e");

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateIncludesCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private UpdateIncludesCommand(AsyncPackage package, OleMenuCommandService commandService) : base(package)
        {
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new OleMenuCommand(this.Execute, this.ChangeHandler, this.QueryStatus, menuCommandID) { Visible = false };
            commandService.AddCommand(menuItem);
        }

        private void ChangeHandler(object sender, EventArgs e)
        {
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static UpdateIncludesCommand Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async System.Threading.Tasks.Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread - the call to AddCommand in UpdateIncludesCommand's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync((typeof(IMenuCommandService))) as OleMenuCommandService;
            Instance = new UpdateIncludesCommand(package, commandService);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            Project project = GetProject();
            if (project == null || project.Object as VCProject == null)
                return;
            
            ProjectIncludesManager.UpdateIncludesAndMacrosInBackground(Path.GetDirectoryName(project.FullName),
                                                                       null, null, project.Object as VCProject);
        }
    }
}
