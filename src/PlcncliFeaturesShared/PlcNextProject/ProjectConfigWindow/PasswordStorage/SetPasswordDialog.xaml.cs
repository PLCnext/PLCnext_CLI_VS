#region Copyright
///////////////////////////////////////////////////////////////////////////////
//
//  Copyright (c) Phoenix Contact GmbH & Co KG
//  This software is licensed under Apache-2.0
//
///////////////////////////////////////////////////////////////////////////////
#endregion

using Microsoft.VisualStudio.PlatformUI;
using System;
using System.ComponentModel;
using System.Windows;

namespace PlcncliFeatures.PlcNextProject.ProjectConfigWindow
{
    /// <summary>
    /// Interaction logic for SetPasswordDialog.xaml
    /// </summary>
    public partial class SetPasswordDialog : DialogWindow
    {

        internal SetPasswordDialog(SetPasswordViewModel viewModel)
        {
            this.DataContext = viewModel;
            InitializeComponent();
            PasswordBox.Password = viewModel.Password;
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            SetPasswordViewModel viewModel = this.DataContext as SetPasswordViewModel;
            if (viewModel != null)
            {
                viewModel.Password = (sender as System.Windows.Controls.PasswordBox).Password;
            }
        }
    }
}
