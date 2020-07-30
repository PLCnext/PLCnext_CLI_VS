﻿#region Copyright
///////////////////////////////////////////////////////////////////////////////
//
//  Copyright (c) Phoenix Contact GmbH & Co KG
//  This software is licensed under Apache-2.0
//
///////////////////////////////////////////////////////////////////////////////
#endregion

using Microsoft.VisualStudio.PlatformUI;

namespace PlcncliSdkOptionPage.ChangeSDKsProperty
{
    /// <summary>
    /// Interaction logic for InstallSdkDialog.xaml
    /// </summary>
    public partial class InstallSdkDialog : DialogWindow
    {
        public InstallSdkDialog(InstallSdkViewModel viewModel)
        {
            this.DataContext = viewModel;
            InitializeComponent();
        }
    }
}
