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
using System.IO;
using System.Linq;
using System.Windows;
using System.Xml;
using System.Xml.Linq;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TemplateWizard;
using PlcNextVSExtension.CommandResults;
using PlcNextVSExtension.NewProjectItemDialog;
using PlcNextVSExtension.PLCnCLI;
using PlcNextVSExtension.Properties;
using Path = System.IO.Path;

namespace PlcNextVSExtension
{
    public class ProjectItemCreationWizard : IWizard
    {
        private readonly IPlcncliCommunication _plcncliCommunication;

        public ProjectItemCreationWizard()
        {
            _plcncliCommunication = new PlcncliProcessCommunication();

        }
        public void RunStarted(object automationObject, Dictionary<string, string> replacementsDictionary, WizardRunKind runKind,
            object[] customParams)
        {
            string itemName = replacementsDictionary["$safeitemname$"];
            //executed first
            ThreadHelper.ThrowIfNotOnUIThread();

            if (automationObject is DTE dte)
            {
                object activeProjects = dte.ActiveSolutionProjects;
                Array activeProjectsArray = (Array) activeProjects;
                if (activeProjectsArray.Length > 0)
                {
                    Project project = (Project) activeProjectsArray.GetValue(0);
                    if (project != null)
                    {
                        IEnumerable<string> validProjectTypes = Enumerable.Empty<string>();
                        string itemType = String.Empty;
                        GetWizardDataFromTemplate();

                        string projectDirectory = Path.GetDirectoryName(project.FullName);
                        ProjectInformationCommandResult projectInformation = _plcncliCommunication.ExecuteCommand(Resources.Command_get_project_information,
                            typeof(ProjectInformationCommandResult),
                            Resources.Option_get_project_information_no_include_detection,
                            Resources.Option_get_project_information_project, $"\"{projectDirectory}\"") as ProjectInformationCommandResult;
                        string projecttype = projectInformation.Type;
                        
                        if (projecttype == null || !validProjectTypes.Contains(projecttype))
                        {
                            MessageBox.Show(
                                $"This template is not available for the selected project. The template is available for the project types{validProjectTypes.Aggregate(string.Empty, (s1, s2) => s1 + $"\n'{s2}'")}\nbut the selected project is of type\n'{projecttype}'.");
                            return;
                        }

                        //TODO different dialog for component template -> no parent component, different description
                        //TODO different 'new' command

                        NewItemModel model = new NewItemModel(_plcncliCommunication, projectDirectory, itemType);
                        NewItemViewModel viewModel = new NewItemViewModel(model);
                        NewItemDialogView view = new NewItemDialogView(viewModel);

                        bool? result = view.ShowModal();
                        if (result != null && result == true)
                        {
                            if (itemType.Equals(Resources.ItemType_program))
                            {
                                _plcncliCommunication.ExecuteCommand(Resources.Command_new_program, null,
                                    Resources.Option_new_program_project, $"\"{projectDirectory}\"",
                                    Resources.Option_new_program_name, itemName,
                                    Resources.Option_new_program_component, model.SelectedComponent,
                                    Resources.Option_new_program_namespace, model.SelectedNamespace);
                            }else if (itemType.Equals(Resources.ItemType_component))
                            {
                                string command = Resources.Command_new_component;
                                if (projecttype.Equals("acfproject"))
                                {
                                    command = Resources.Command_new_acfcomponent;
                                }
                                _plcncliCommunication.ExecuteCommand(command, null,
                                    Resources.Option_new_component_project, $"\"{projectDirectory}\"",
                                    Resources.Option_new_component_name, itemName,
                                    Resources.Option_new_component_namespace, model.SelectedNamespace);
                            }

                            string[] itemFiles = Directory.GetFiles(Path.Combine(projectDirectory, "src"), $"{itemName}.*pp");
                            foreach (string itemFile in itemFiles)
                            {
                                project.ProjectItems.AddFromFile(itemFile);
                            }

                            _plcncliCommunication.ExecuteCommand(Resources.Command_generate_code, null,
                                Resources.Option_generate_code_project, $"\"{projectDirectory}\"");

                        }


                        void GetWizardDataFromTemplate()
                        {
                            string wizardData = replacementsDictionary["$wizarddata$"];

                            XDocument document = XDocument.Parse(wizardData);
                            XNamespace nspace = document.Root.GetDefaultNamespace();

                            validProjectTypes = document.Element(nspace + "Data").Element(nspace + "ValidProjectTypes")
                                .Descendants(nspace + "Type").Select(e => e.Value);

                            itemType =
                                document.Element(nspace + "Data").Element(nspace + "ItemType").Value;
                        }
                    }
                }
            }
        }

        public void ProjectFinishedGenerating(Project project)
        {
            throw new NotImplementedException();
        }

        public void ProjectItemFinishedGenerating(ProjectItem projectItem)
        {
            //executed 3.
        }

        public bool ShouldAddProjectItem(string filePath)
        {
            //executed 2.
            return false;
        }

        public void BeforeOpeningFile(ProjectItem projectItem)
        {
            //file is created then this is executed
            //executed 4.
        }

        public void RunFinished()
        {
            //executed 5.
        }
    }
}
