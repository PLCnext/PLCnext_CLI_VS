﻿#region Copyright
///////////////////////////////////////////////////////////////////////////////
//
//  Copyright (c) Phoenix Contact GmbH & Co KG
//  This software is licensed under Apache-2.0
//
///////////////////////////////////////////////////////////////////////////////
#endregion

using System;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace PlcncliFeatures.PlcNextProject.ProjectConfigWindow
{
    internal class PasswordService
    {
        private IVsSolutionPersistence solutionPersistence;
        private const string PasswordServicePersistenceKey = "PLCnextSigningPassword"; 

        public PasswordService(IVsSolutionPersistence vsSolutionPersistence)
        {
            solutionPersistence = vsSolutionPersistence;
        }

        public void ProvidePasswordPersistence(string projectName, PasswordPersistFileType fileType)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            LoadPassword(projectName, fileType, out string password);
            SetPasswordViewModel viewModel = new SetPasswordViewModel(password: password, titleText: "Set Password", okButtonText: "_Save");
            SetPasswordDialog passwordDialog = new SetPasswordDialog(viewModel);
            bool? dialogResult = passwordDialog.ShowModal();

            if (dialogResult == true)
            {
                SavePassword(projectName, fileType, viewModel.Password);
            }
        }

        public string GetProjectPassword(string projectName, PasswordPersistFileType fileType)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            LoadPassword(projectName, fileType, out string password);
            if (string.IsNullOrEmpty(password))
            {
                GetPasswordFromUIPrompt(out password);
            }
            return password;
        }

        private void LoadPassword(string projectName, PasswordPersistFileType fileType, out string password)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            password = string.Empty;
            
            if (solutionPersistence != null)
            {
                try
                {
                    PasswordPersistSolutionOptions options = new PasswordPersistSolutionOptions();
                    
                    solutionPersistence.LoadPackageUserOpts(options, PasswordServicePersistenceKey);

                    options.Data.GetEntry(projectName, fileType, out password);
                }
                catch (Exception) { }
            }
        }

        private void SavePassword(string projectName, PasswordPersistFileType fileType, string password)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (solutionPersistence != null)
            {
                try
                {
                    PasswordPersistSolutionOptions options = new PasswordPersistSolutionOptions();
                    solutionPersistence.LoadPackageUserOpts(options, PasswordServicePersistenceKey);
                    PasswordPersistModel persistModel = options.Data;
                    persistModel.AddEntry(projectName, fileType, password);
                    
                    options = new PasswordPersistSolutionOptions(persistModel);
                    int result = solutionPersistence.SavePackageUserOpts(options, PasswordServicePersistenceKey);
                }
                catch (Exception) { }
            }
        }

        private void GetPasswordFromUIPrompt(out string password)
        {
            password = string.Empty;
            SetPasswordViewModel viewModel = new SetPasswordViewModel(
                null,
                titleText: "Signing library requires password",
                okButtonText: "Submit",
                additionalInformation: "The password will not be persisted.\n" +
                                       "To save a password go to\n" +
                                       "Project -> PLCnext Technology -> Project Configuration"
                );
            SetPasswordDialog passwordDialog = new SetPasswordDialog(viewModel);
            bool? dialogResult = passwordDialog.ShowModal();

            if (dialogResult == true)
            {
                password = viewModel.Password;
            }
        }
    }
}