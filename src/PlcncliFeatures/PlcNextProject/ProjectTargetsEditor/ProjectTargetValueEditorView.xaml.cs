#region Copyright
///////////////////////////////////////////////////////////////////////////////
//
//  Copyright (c) Phoenix Contact GmbH & Co KG
//  This software is licensed under Apache-2.0
//
///////////////////////////////////////////////////////////////////////////////
#endregion

using Microsoft.VisualStudio.PlatformUI;

namespace PlcncliFeatures.PlcNextProject.ProjectTargetsEditor
{
    /// <summary>
    /// Interaction logic for ProjectTargetValueEditorView.xaml
    /// </summary>
    public partial class ProjectTargetValueEditorView : DialogWindow
    {
        public ProjectTargetValueEditorView(ProjectTargetValueEditorViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
