#region Copyright
///////////////////////////////////////////////////////////////////////////////
//
//  Copyright (c) Phoenix Contact GmbH & Co KG
//  This software is licensed under Apache-2.0
//
///////////////////////////////////////////////////////////////////////////////
#endregion

using System.Collections.Generic;
using System.Linq;
using PlcncliServices.CommandResults;
using PlcncliServices.PLCnCLI;
using PlcNextVSExtension.Properties;

namespace PlcNextVSExtension.NewProjectItemDialog
{
    public class NewItemModel
    {
        private readonly IPlcncliCommunication _plcncliCommunication;
        private readonly string _projectDirectory;

        public NewItemModel(IPlcncliCommunication plcncliCommunication, string projectDirectory, string itemType)
        {
            _plcncliCommunication = plcncliCommunication;
            _projectDirectory = projectDirectory;
            ItemType = itemType;
            FetchProjectComponents();
        }

        public string ItemType { get; }

        public string SelectedNamespace { get; set; }

        public IEnumerable<string> Components { get; private set; }

        public string SelectedComponent { get; set; }


        private void FetchProjectComponents()
        {
            ProjectInformationCommandResult projectInformation = _plcncliCommunication.ExecuteCommand(Resources.Command_get_project_information, null,
                typeof(ProjectInformationCommandResult), Resources.Option_get_project_information_no_include_detection,
                Resources.Option_get_project_information_project, $"\"{_projectDirectory}\"") as ProjectInformationCommandResult;
            if (projectInformation != null)
            {
                Components = projectInformation.Entities.Where(e => e.Type.Equals("component"))
                    .Select(e => $"{e.Namespace}::{e.Name}");
                SelectedNamespace = projectInformation.Namespace;
            }
        }
    }
}
