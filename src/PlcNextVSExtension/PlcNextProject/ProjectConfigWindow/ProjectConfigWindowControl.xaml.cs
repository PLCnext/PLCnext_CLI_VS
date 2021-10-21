#region Copyright
///////////////////////////////////////////////////////////////////////////////
//
//  Copyright (c) Phoenix Contact GmbH & Co KG
//  This software is licensed under Apache-2.0
//
///////////////////////////////////////////////////////////////////////////////
#endregion

using Microsoft.VisualStudio.PlatformUI;

namespace PlcNextVSExtension.PlcNextProject.ProjectConfigWindow
{
    /// <summary>
    /// Interaction logic for ProjectConfigWindowControl.
    /// </summary>
    public partial class ProjectConfigWindowControl : DialogWindow
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectConfigWindowControl"/> class.
        /// </summary>
        public ProjectConfigWindowControl(ProjectConfigWindowViewModel viewModel)
        {
            this.DataContext = viewModel;
            this.InitializeComponent();
        }

    }
}