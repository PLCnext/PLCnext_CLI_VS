#region Copyright
///////////////////////////////////////////////////////////////////////////////
//
//  Copyright (c) Phoenix Contact GmbH & Co KG
//  This software is licensed under Apache-2.0
//
///////////////////////////////////////////////////////////////////////////////
#endregion

using Microsoft.VisualStudio.PlatformUI;

namespace PlcncliFeatures.PlcNextProject.ProjectCMakeFlagsEditor
{
    /// <summary>
    /// Interaction logic for CMakeFlagsEditorView.xaml
    /// </summary>
    public partial class CMakeFlagsEditorView : DialogWindow
    {
        private CMakeFlagsEditorViewModel viewModel; 
        public CMakeFlagsEditorView(CMakeFlagsEditorViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            this.viewModel = viewModel;
        }


        private void TextBox_GotFocus(object sender, System.Windows.RoutedEventArgs e)
        {
            viewModel.ClearIfExampleWasShown();
        }

        private void TextBox_LostFocus(object sender, System.Windows.RoutedEventArgs e)
        {
            viewModel.SetExampleIfEmpty();
        }
    }
}
