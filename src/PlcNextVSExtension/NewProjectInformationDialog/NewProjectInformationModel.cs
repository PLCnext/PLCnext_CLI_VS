#region Copyright
///////////////////////////////////////////////////////////////////////////////
//
//  Copyright (c) Phoenix Contact GmbH & Co KG
//  This software is licensed under Apache-2.0
//
///////////////////////////////////////////////////////////////////////////////
#endregion

using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using PlcNextVSExtension.CommandResults;
using PlcNextVSExtension.Properties;

namespace PlcNextVSExtension.NewProjectInformationDialog
{
    public class NewProjectInformationModel : INotifyPropertyChanged
    {
        private readonly string _projectDirectory;
        private readonly string _projectName;
        private string _projectNamespace;
        private string _initialComponentName;
        private string _initialProgramName;
        private IEnumerable<TargetResult> _allTargets = new List<TargetResult>();
        private readonly IPlcncliCommunication _plcncliCommunication;

        public string ProjectNamespace
        {
            get => _projectNamespace;
            set { _projectNamespace = value; OnPropertyChanged(); }
        }

        public string InitialComponentName
        {
            get => _initialComponentName;
            set { _initialComponentName = value; OnPropertyChanged(); }
        }

        public string InitialProgramName
        {
            get => _initialProgramName;
            set { _initialProgramName = value; OnPropertyChanged(); }
        }
        public IEnumerable<TargetResult> AllTargets
        {
            get => _allTargets;
            set { _allTargets = value; OnPropertyChanged(); }
        }

        public IEnumerable<TargetResult> ProjectTargets { get; set; } = Enumerable.Empty<TargetResult>();

        public string ProjectType { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public NewProjectInformationModel(IPlcncliCommunication plcncliCommunication, string projectDirectory, string projectName,
            string projectType)
        {
            _plcncliCommunication = plcncliCommunication;
            _projectDirectory = projectDirectory;
            _projectName = projectName;
            ProjectNamespace = _projectName;
            InitialComponentName = $"{_projectName}Component";
            InitialProgramName = $"{_projectName}Program";
            ProjectType = projectType;

            UpdateTargets();
        }

        public void UpdateTargets()
        {
            var result = _plcncliCommunication.ExecuteCommand(Resources.Command_get_targets, typeof(TargetsCommandResult)) as TargetsCommandResult;
            if (result != null) AllTargets = result.Targets;
        }

    }
}
