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
using PlcncliCommonUtils;
using PlcncliServices.CommandResults;
using PlcncliServices.PLCnCLI;

namespace PlcncliTemplateWizards.NewProjectInformationDialog
{
    public class NewProjectInformationModel : INotifyPropertyChanged
    {
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

        public NewProjectInformationModel(IPlcncliCommunication plcncliCommunication, string projectName,
            string projectType)
        {
            _plcncliCommunication = plcncliCommunication;
            _projectName = projectName;
            ProjectNamespace = _projectName;
            InitialComponentName = $"{FormatProjectName(_projectName)}Component";
            InitialProgramName = $"{FormatProjectName(_projectName)}Program";
            ProjectType = projectType;

            UpdateTargets();

            string FormatProjectName(string name)
            {
                if (!name.Contains('.'))
                {
                    return name;
                }
                return name.Substring(name.LastIndexOf('.') + 1);
            }
        }

        public void UpdateTargets()
        {
            var result = _plcncliCommunication.ExecuteCommand(Constants.Command_get_targets, null, typeof(TargetsCommandResult)) as TargetsCommandResult;
            if (result != null)
            {
                AllTargets = result.Targets;
            }
        }
    }
}
