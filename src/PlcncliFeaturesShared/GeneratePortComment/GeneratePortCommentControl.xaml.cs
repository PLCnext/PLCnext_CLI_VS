#region Copyright
///////////////////////////////////////////////////////////////////////////////
//
//  Copyright (c) Phoenix Contact GmbH & Co KG
//  This software is licensed under Apache-2.0
//
///////////////////////////////////////////////////////////////////////////////
#endregion

using Microsoft.VisualStudio.PlatformUI;

namespace PlcncliFeatures.GeneratePortComment
{
    /// <summary>
    /// Interaction logic for GeneratePortCommentControl.xaml
    /// </summary>
    public partial class GeneratePortCommentControl : DialogWindow
    {
        public GeneratePortCommentControl(GeneratePortCommentViewModel viewModel)
        {
            DataContext = viewModel;
            InitializeComponent();
        }
    }
}
