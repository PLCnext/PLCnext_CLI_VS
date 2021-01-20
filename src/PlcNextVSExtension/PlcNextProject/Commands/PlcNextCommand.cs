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
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.VCProjectEngine;
using System;

namespace PlcNextVSExtension.PlcNextProject.Commands
{
    internal abstract class PlcNextCommand
    {
        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        protected readonly AsyncPackage package;

        public PlcNextCommand(AsyncPackage package)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        protected Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD010:Invoke single-threaded types on Main thread", Justification = "Checked in GetProject()")]
        protected void QueryStatus(object sender, EventArgs e)
        {
            if (sender is OleMenuCommand cmd)
            {
                try
                {
                    Project project = GetProject();
                    VCProject p = project.Object as VCProject;
                    VCConfiguration configuration = p.ActiveConfiguration;
                    IVCRulePropertyStorage plcnextRule = configuration.Rules.Item("PLCnextCommonProperties");
                    string projectType = plcnextRule.GetUnevaluatedPropertyValue("ProjectType_");
                    if (!string.IsNullOrEmpty(projectType))
                    {
                        cmd.Visible = true;
                        return;
                    }
                }
                catch (NullReferenceException)
                {
                    //cmd visibility will be set to false
                }
                cmd.Visible = false;
            }
        }

        protected Project GetProject()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            //get project location
            IServiceProvider serviceProvider = package;
            DTE2 dte = (DTE2)serviceProvider.GetService(typeof(DTE));
            if (dte == null)
                return null;
            Array selectedItems = (Array)dte.ToolWindows.SolutionExplorer.SelectedItems;
            if (selectedItems == null || selectedItems.Length > 1)
                return null;
            UIHierarchyItem x = selectedItems.GetValue(0) as UIHierarchyItem;
            return x.Object as Project;
        }
    }
}
