#region Copyright
///////////////////////////////////////////////////////////////////////////////
//
//  Copyright (c) Phoenix Contact GmbH & Co KG
//  This software is licensed under Apache-2.0
//
///////////////////////////////////////////////////////////////////////////////
#endregion

using Microsoft.VisualStudio.PlatformUI;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System;
using EnvDTE;
using Microsoft.VisualStudio.VCProjectEngine;
using Microsoft.VisualStudio.Shell;
using Window = System.Windows.Window;
using System.Windows.Media.Imaging;
using System.Drawing;
using System.Windows.Interop;

namespace PlcNextVSExtension.PlcNextProject.EngineerVersionEditor
{
    public class SetEngineerVersionEditorViewModel : INotifyPropertyChanged
    {
        private string engineerVersion;
        private string errorText;
        private readonly VCProject vcProject;

        public SetEngineerVersionEditorViewModel(Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            vcProject = project.Object as VCProject;
            engineerVersion = GetEngineerVersionFromProject();
        }

        public string EngineerVersion
        {
            get => engineerVersion;
            set
            {
                if (!CheckVersion(value))
                {
                    SetErrorMessage();
                }
                else
                {
                    ClearErrorMessage();
                }
                engineerVersion = value;
                OnPropertyChanged();
            }
        }

        public string ErrorText
        {
            get => errorText;
            private set
            {
                errorText = value;
                OnPropertyChanged();
            }
        }

        public BitmapSource ErrorImage => Imaging.CreateBitmapSourceFromHIcon(SystemIcons.Error.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

        #region private methods
        private void SetErrorMessage()
        {
            ErrorText = "Not a valid version! Please use format: major.minor[.build[.revision]]";
        }

        private void ClearErrorMessage()
        {
            ErrorText = string.Empty;
        }
        private bool CheckVersion(string value)
        {
            return System.Version.TryParse(value, out _);
        }

        private IVCRulePropertyStorage GetPLCnextRule()
        {
            IVCRulePropertyStorage plcnextCommonPropertiesRule = vcProject.ActiveConfiguration.Rules.Item(Constants.PLCnextRuleName);
            if (plcnextCommonPropertiesRule == null)
            {
                MessageBox.Show($"{Constants.PLCnextRuleName} rule was not found in configuration rules collection.");
            }
            return plcnextCommonPropertiesRule;
        }

        private string GetEngineerVersionFromProject()
        {
            IVCRulePropertyStorage plcnextCommonPropertiesRule = GetPLCnextRule();
            string result = plcnextCommonPropertiesRule.GetUnevaluatedPropertyValue(Constants.PLCnextEngineerVersion);
            if (result == null)
            {
                MessageBox.Show("'PLCnextEngineerVersion' property was not found in plcnextCommonProperties rule.");
            }
            return result;
        }

        private void SaveEngineerVersionInProject()
        {
            IVCRulePropertyStorage plcnextCommonPropertiesRule = GetPLCnextRule();
            plcnextCommonPropertiesRule.SetPropertyValue(Constants.PLCnextEngineerVersion, EngineerVersion);
        }
        #endregion

        #region Commands
        public ICommand CloseButtonClickCommand => new DelegateCommand<Window>(OnCloseButtonClicked);
        public ICommand CancelButtonClickCommand => new DelegateCommand<Window>(OnCancelButtonClicked);

        private void OnCloseButtonClicked(Window window)
        {
            window.DialogResult = true;
            SaveEngineerVersionInProject();
            window.Close();
        }

        private void OnCancelButtonClicked(Window window)
        {
            window.DialogResult = false;
            window.Close();
        }
        #endregion
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
