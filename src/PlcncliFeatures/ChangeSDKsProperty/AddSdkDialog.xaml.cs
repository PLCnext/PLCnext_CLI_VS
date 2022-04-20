#region Copyright
///////////////////////////////////////////////////////////////////////////////
//
//  Copyright (c) Phoenix Contact GmbH & Co KG
//  This software is licensed under Apache-2.0
//
///////////////////////////////////////////////////////////////////////////////
#endregion

using Microsoft.VisualStudio.PlatformUI;

namespace PlcncliFeatures.ChangeSDKsProperty
{
    /// <summary>
    /// Interaction logic for AddSdkDialog.xaml
    /// </summary>
    public partial class AddSdkDialog : DialogWindow
    {
        internal AddSdkDialog(AddSdkViewModel viewModel)
        {
            this.DataContext = viewModel;
            InitializeComponent();
        }
    }
}
