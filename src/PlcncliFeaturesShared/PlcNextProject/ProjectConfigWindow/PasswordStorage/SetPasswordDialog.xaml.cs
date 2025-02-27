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
            viewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            SetPasswordViewModel viewModel = this.DataContext as SetPasswordViewModel;
            if (viewModel != null)
            { 
                if (e.PropertyName == nameof(viewModel.Password)
                    && PasswordBox.Password != viewModel.Password)
                {
                    PasswordBox.Password = viewModel.Password;
                }
            }
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            SetPasswordViewModel viewModel = this.DataContext as SetPasswordViewModel;
            if (viewModel != null)
            {
                viewModel.Password = (sender as System.Windows.Controls.PasswordBox).Password;
            }
        }

        private void Button_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            SetPasswordViewModel viewModel = this.DataContext as SetPasswordViewModel;
            if (viewModel != null)
            {
                viewModel.ShowPassword = true;
            }
        }
        private void Button_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            SetPasswordViewModel viewModel = this.DataContext as SetPasswordViewModel;
            if (viewModel != null)
            {
                viewModel.ShowPassword = false;
            }
        }
    }
}
