﻿#region Copyright
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
    /// Interaction logic for RemoveSdkDialog.xaml
    /// </summary>
    public partial class RemoveSdkDialog : DialogWindow
    {
        public RemoveSdkDialog(RemoveSdkViewModel viewModel)
        {
            DataContext = viewModel;
            InitializeComponent();
        }
    }
}
