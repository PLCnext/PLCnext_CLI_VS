#region Copyright
///////////////////////////////////////////////////////////////////////////////
//
//  Copyright (c) Phoenix Contact GmbH & Co KG
//  This software is licensed under Apache-2.0
//
///////////////////////////////////////////////////////////////////////////////
#endregion

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Microsoft.VisualStudio.PlatformUI;
using PlcncliCommonUtils;

namespace PlcncliTemplateWizards.NewProjectInformationDialog
{
    public class NewProjectInformationViewModel : INotifyPropertyChanged
    {
        private const string ProjectNamespaceKey = "Project name_space";
        private const string InitialComponentNameKey = "_Component name";
        private const string InitialProgramNameKey = "_Program name";

        private const string WarningNoTargetSelected = "Project will support no target! Select at least one target. ";

        private readonly NewProjectInformationModel _model;
        private string _warningMessage = WarningNoTargetSelected;
        private string errorText = string.Empty;

        private static readonly Regex ComponentNameRegex = new Regex(@"^[A-Z](?!.*__)[a-zA-Z0-9_]*$", RegexOptions.Compiled);
        private static readonly Regex ProgramNameRegex = new Regex(@"^[A-Z](?!.*__)[a-zA-Z0-9_]*$", RegexOptions.Compiled);
        private static readonly Regex ProjectNamespaceRegex = new Regex(@"^(?:[a-zA-Z][a-zA-Z0-9_]*\.)*[a-zA-Z](?!.*__)[a-zA-Z0-9_]*$", RegexOptions.Compiled);

        bool showComponentError = false;
        bool showProgramError = false;
        bool showNamespaceError = false;
        bool showComponentNamespaceEqualError = false;
        private readonly string NamespaceErrorText = "Namespace does not match pattern ^(?:[a-zA-Z][a-zA-Z0-9_]*\\.)*[a-zA-Z](?!.*__)[a-zA-Z0-9_]*$";
        private readonly string ComponentErrorText = "Component name does not match pattern ^[A-Z](?!.*__)[a-zA-Z0-9_]*$";
        private readonly string ProgramErrorText = "Program name does not match pattern ^[A-Z](?!.*__)[a-zA-Z0-9_]*$";
        private readonly string ComponentNamespaceEqualErrorText = "Component name and namespace should not be the same";

        #region Properties

        public AccessText TargetListLabel { get; } = new AccessText
        {
            Text = "Select one or more _targets which will be added to the project"
        };

        public string WindowTitle => "Configure your PlcNext Project";

        public string ButtonText => "_OK";

        public string WarningMessage
        {
            get => _warningMessage;
            set
            {
                _warningMessage = value;
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


        public ObservableCollection<TargetViewModel> Targets { get; } = new ObservableCollection<TargetViewModel>();

        public ObservableCollection<KVP> ProjectNameProperties { get; } = new ObservableCollection<KVP>();
        #endregion

        public NewProjectInformationViewModel(NewProjectInformationModel model)
        {
            _model = model;
            InitializeTargets();
            SetProjectNameProperties();
        }

        private void InitializeTargets()
        {
            _model.AllTargets.ToList().ForEach(t => Targets.AddIfNotExists(t));
            foreach (TargetViewModel target in Targets)
            {
                target.PropertyChanged += TargetOnPropertyChanged;
            }
        }

        private void TargetOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("Selected") && !Targets.Any(t => t.Selected))
            {
                WarningMessage = WarningNoTargetSelected;
            }
            else
            {
                WarningMessage = "";
            }
        }

        private void SetProjectNameProperties()
        {
            ProjectNameProperties.Add(new KVP(ProjectNamespaceKey, _model.ProjectNamespace, ProjectNamespaceRegex, ErrorType.Namespace, this));
            if (_model.ProjectType != Constants.ProjectType_ConsumableLibrary)
            {
                ProjectNameProperties.Add(new KVP(InitialComponentNameKey, _model.InitialComponentName, ComponentNameRegex, ErrorType.Component, this));
                if (_model.ProjectType == Constants.ProjectType_PLM)
                {
                    ProjectNameProperties.Add(new KVP(InitialProgramNameKey, _model.InitialProgramName, ProgramNameRegex, ErrorType.Program, this));
                }
            }
        }

        #region Commands

        public ICommand OkButtonClickCommand => new DelegateCommand<Window>(OnOkButtonClicked);

        private void OnOkButtonClicked(Window window)
        {
            //first check if at least one target is selected
            if (!Targets.Any(t => t.Selected))
            {
                MessageBoxResult messageBoxResult =
                    MessageBox.Show("No target was selected. Do you really want to create a project, which supports no target?",
                        "No target selected", MessageBoxButton.OKCancel);
                if (messageBoxResult == MessageBoxResult.Cancel)
                    return;
            }

            string projectNamespace = ProjectNameProperties.Where(p => p.Name.Text.Equals(ProjectNamespaceKey))
                .Select(p => p.Value).SingleOrDefault();
            if (projectNamespace != null)
            {
                _model.ProjectNamespace = projectNamespace;
            }

            if (_model.ProjectType != Constants.ProjectType_ConsumableLibrary)
            {
                string componentName = ProjectNameProperties.Where(p => p.Name.Text.Equals(InitialComponentNameKey))
                    .Select(p => p.Value).SingleOrDefault();
                if (componentName != null)
                {
                    _model.InitialComponentName = componentName;
                }

                if (_model.ProjectType == Constants.ProjectType_PLM)
                {
                    string programName = ProjectNameProperties.Where(p => p.Name.Text.Equals(InitialProgramNameKey))
                        .Select(p => p.Value).SingleOrDefault();
                    if (programName != null)
                    {
                        _model.InitialProgramName = programName;
                    }
                }
            }

            _model.ProjectTargets = Targets.Where(t => t.Selected).Select(t => t.Source);
            window.DialogResult = true;
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

        internal void ShowError(ErrorType errortype)
        {
            switch (errortype)
            {
                case ErrorType.Namespace:
                    showNamespaceError = true;
                    break;
                case ErrorType.Component:
                    showComponentError = true;
                    break;
                case ErrorType.Program:
                    showProgramError = true;
                    break;
                case ErrorType.ComponentNamespace:
                    showComponentNamespaceEqualError = true;
                    break;
                default:
                    break;
            }
            UpdateErrorText();
        }
        internal void RemoveError(ErrorType errorType)
        {
            switch (errorType)
            {
                case ErrorType.Namespace:
                    showNamespaceError = false;
                    break;
                case ErrorType.Component:
                    showComponentError = false;
                    break;
                case ErrorType.Program:
                    showProgramError = false;
                    break;
                case ErrorType.ComponentNamespace:
                    showComponentNamespaceEqualError = false;
                    break;
                default:
                    break;
            }
            UpdateErrorText();
        }

        private void UpdateErrorText()
        {
            if (showNamespaceError)
            {
                ErrorText = NamespaceErrorText;
                return;
            }
            if (showComponentError)
            {
                ErrorText = ComponentErrorText;
                return;
            }
            if (showProgramError)
            {
                ErrorText = ProgramErrorText;
                return;
            }
            if (showComponentNamespaceEqualError)
            {
                ErrorText = ComponentNamespaceEqualErrorText;
                return;
            }
            ErrorText = string.Empty;
        }

        internal enum ErrorType
        {
            Namespace,
            Component,
            Program,
            ComponentNamespace
        }

        internal void ValidateNames()
        {
            string ns = ProjectNameProperties.Where(x => x.Name.Text == ProjectNamespaceKey).FirstOrDefault()?.Value;
            if ( ns ==
                ProjectNameProperties.Where(y => y.Name.Text == InitialComponentNameKey).FirstOrDefault()?.Value
                && ns!= null)
            {
                ShowError(ErrorType.ComponentNamespace);
            }
            else
            {
                RemoveError(ErrorType.ComponentNamespace);
            }
        }
    }

    public class KVP
    {
        private string _value;
        private readonly NewProjectInformationViewModel.ErrorType errorType;
        private readonly NewProjectInformationViewModel vm;
        private readonly Regex validNameRegex;

        internal KVP(string name, string value, Regex validNameRegex, 
            NewProjectInformationViewModel.ErrorType errorType, NewProjectInformationViewModel vm)
        {
            this.vm = vm;
            this.errorType = errorType;
            this.validNameRegex = validNameRegex;
            Name = new AccessText { Text = name };
            Value = value;
        }
        public AccessText Name { get; }
        public string Value
        {
            get => _value; 
            set
            {
                _value = value;
                Validate(_value);
            }
        }

        private void Validate(string value)
        {
            vm.ValidateNames();
            if (validNameRegex.IsMatch(value))
            {
                vm.RemoveError(errorType);
                return;
            }
            vm.ShowError(errorType);
        }
    }
}
