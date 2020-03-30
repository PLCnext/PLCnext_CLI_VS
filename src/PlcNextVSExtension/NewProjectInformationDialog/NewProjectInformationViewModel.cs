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
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.VisualStudio.PlatformUI;
using PlcNextVSExtension.Properties;

namespace PlcNextVSExtension.NewProjectInformationDialog
{
    public class NewProjectInformationViewModel : INotifyPropertyChanged
    {
        private const string ProjectNamespaceKey = "Project name_space";
        private const string InitialComponentNameKey = "_Component name";
        private const string InitialProgramNameKey = "_Program name";

        private const string WarningNoTargetSelected = "Project will support no target! Select at least one target. ";

        private readonly NewProjectInformationModel _model;
        private string _warningMessage = WarningNoTargetSelected;

        #region Properties

        public AccessText TargetListLabel { get; } = new AccessText
        {
            Text = "Select one or more _targets which will be added to the project"
        };

        public string WindowTitle => "Configure your PlcNext Project";

        public string ButtonText => "OK";

        public string WarningMessage
        {
            get => _warningMessage;
            set 
            { 
                _warningMessage = value;
                OnPropertyChanged();
            }
        }

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
            if(e.PropertyName.Equals("Selected") && !Targets.Any(t => t.Selected))
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
            ProjectNameProperties.Add(new KVP(ProjectNamespaceKey, _model.ProjectNamespace));
            ProjectNameProperties.Add(new KVP(InitialComponentNameKey, _model.InitialComponentName));
            if (_model.ProjectType == Resources.ProjectType_PLM)
            {
                ProjectNameProperties.Add(new KVP(InitialProgramNameKey, _model.InitialProgramName));
            }
        }

        #region Commands

        public ICommand OkButtonClickCommand => new DelegateCommand<Window>(OnOkButtonClicked);

        private void OnOkButtonClicked(Window window)
        {
            string projectNamespace = ProjectNameProperties.Where(p => p.Name.Text.Equals(ProjectNamespaceKey))
                .Select(p => p.Value).SingleOrDefault();
            if (projectNamespace != null)
            {
                _model.ProjectNamespace = projectNamespace;
            }

            string componentName = ProjectNameProperties.Where(p => p.Name.Text.Equals(InitialComponentNameKey))
                .Select(p => p.Value).SingleOrDefault();
            if (componentName != null)
            {
                _model.InitialComponentName = componentName;
            }

            if (_model.ProjectType == Resources.ProjectType_PLM)
            {
                string programName = ProjectNameProperties.Where(p => p.Name.Text.Equals(InitialProgramNameKey))
                    .Select(p => p.Value).SingleOrDefault();
                if (programName != null)
                {
                    _model.InitialProgramName = programName;
                }
            }

            _model.ProjectTargets = Targets.Where(t => t.Selected).Select(t => t.Source);

            window.Close();
        }

        #endregion

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class KVP
    {
        public KVP(string name, string value)
        {
            Name = new AccessText { Text = name };
            Value = value;
        }
        public AccessText Name { get; }
        public string Value { get; set; }
    }
}
