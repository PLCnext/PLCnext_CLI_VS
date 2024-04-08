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

namespace PlcncliTemplateWizards.NewProjectItemDialog
{
    public class NewItemModel
    {
        public NewItemModel(string itemType, string name, ProjectInformationCommandResult projectInformation)
        {
            ItemType = itemType;
            SelectedName = name;
            FetchProjectComponents(projectInformation);
        }

        public string ItemType { get; }

        public string SelectedNamespace { get; set; }

        public string SelectedName { get; set; }

        public IEnumerable<EntityResult> Components { get; private set; }
        public IEnumerable<EntityResult> Programs { get; private set; }

        public string SelectedComponent { get; set; }


        private void FetchProjectComponents(ProjectInformationCommandResult projectInformation)
        {
            if (projectInformation != null)
            {
                Components = projectInformation.Entities.Where(e => e.Type.Equals("component"));
                Programs = projectInformation.Entities.Where(e => e.Type.Equals("program"));
                SelectedNamespace = projectInformation.Namespace;
            }
        }
    }
}
