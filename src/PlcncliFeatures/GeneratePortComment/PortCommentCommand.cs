#region Copyright
///////////////////////////////////////////////////////////////////////////////
//
//  Copyright (c) Phoenix Contact GmbH & Co KG
//  This software is licensed under Apache-2.0
//
///////////////////////////////////////////////////////////////////////////////
#endregion

using System;
using System.ComponentModel.Design;
using System.Windows;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace PlcncliFeatures.GeneratePortComment
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class PortCommentCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("d03b062f-5831-4deb-b619-beb902e75a3e");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="PortCommentCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private PortCommentCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static PortCommentCommand Instance
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
            // Switch to the main thread - the call to AddCommand in PortCommentCommand's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new PortCommentCommand(package, commandService);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (!(Package.GetGlobalService(typeof(DTE)) is DTE dte))
            {
                MessageBox.Show("Could not generate port comments because dte is null.");
                return;
            }
            Document activeDocument = dte.ActiveDocument;
            if (activeDocument == null)
            {
                MessageBox.Show("Could not generate port comments because there is no active document.");
                return;
            }
            if (!(activeDocument.Selection is TextSelection selection))
            {
                MessageBox.Show("Could not generate port comments because no selection was found.");
                return;
            }

            VirtualPoint activePoint = selection.ActivePoint;
            string line = activePoint.CreateEditPoint().GetLines(activePoint.Line, activePoint.Line + 1);

            GeneratePortCommentViewModel vm = new GeneratePortCommentViewModel(line);
            GeneratePortCommentControl dialog = new GeneratePortCommentControl(vm);
            bool? result = dialog.ShowModal();

            if (result == true)
            {
                selection.LineUp();
                selection.EndOfLine();
                selection.NewLine();
                selection.Insert(vm.Preview);
            }
            vm.Dispose();
        }
    }
}
