#region Copyright
///////////////////////////////////////////////////////////////////////////////
//
//  Copyright (c) Phoenix Contact GmbH & Co KG
//  This software is licensed under Apache-2.0
//
///////////////////////////////////////////////////////////////////////////////
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using PlcncliServices.CommandResults;
using PlcncliServices.PLCnCLI;
using PlcncliCommonUtils;

namespace PlcncliFeatures.PlcNextProject.ProjectTargetsEditor
{
    public class ProjectTargetValueEditorModel
    {

        public ProjectTargetValueEditorModel(IPlcncliCommunication cliCommunication, string projectDirectory)
        {
            if (cliCommunication != null)
            {
                TargetsCommandResult targetsCommandResult =
                    cliCommunication.ExecuteCommand(Constants.Command_get_targets, null, typeof(TargetsCommandResult))
                        as TargetsCommandResult;
                InstalledTargets = targetsCommandResult.Targets;

                ProjectInformationCommandResult projectInfo = cliCommunication.ExecuteCommand(
                        Constants.Command_get_project_information, null, typeof(ProjectInformationCommandResult),
                        Constants.Option_get_project_information_no_include_detection,
                        Constants.Option_get_project_information_project, $"\"{projectDirectory}\"") as
                    ProjectInformationCommandResult;
                ProjectTargets = projectInfo.Targets;
                //TODO extract these commands somewhere after the viewModel.Showmodal call (and possibly asyncron), otherwise the ui seems to be too unresponsive
            }
        }

        public IEnumerable<TargetResult> TargetsToRemove { get; set; } = Enumerable.Empty<TargetResult>();

        public IEnumerable<TargetResult> TargetsToAdd { get; set; } = Enumerable.Empty<TargetResult>();

        public IEnumerable<TargetResult> ProjectTargets { get; }

        public IEnumerable<TargetResult> InstalledTargets { get; }
    }
}
