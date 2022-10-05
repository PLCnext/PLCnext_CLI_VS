#region Copyright
///////////////////////////////////////////////////////////////////////////////
//
//  Copyright (c) Phoenix Contact GmbH & Co KG
//  This software is licensed under Apache-2.0
//
///////////////////////////////////////////////////////////////////////////////
#endregion

using Microsoft.VisualStudio.PlatformUI;

namespace PlcncliFeatures.PlcNextProject.OnDocSaveService
{
    /// <summary>
    /// Interaction logic for UpdateIncludesDialogView.xaml
    /// </summary>
    public partial class UpdateIncludesDialogView : DialogWindow
    {
        public UpdateIncludesDialogView(UpdateIncludesViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
