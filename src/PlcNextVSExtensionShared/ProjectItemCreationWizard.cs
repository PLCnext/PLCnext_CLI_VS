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
using System.Xml.Linq;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TemplateWizard;
using PlcncliServices.CommandResults;
using PlcncliServices.PLCnCLI;
using PlcncliTemplateWizards.NewProjectItemDialog;
using Constants = PlcncliCommonUtils.Constants;
using Path = System.IO.Path;

namespace PlcncliTemplateWizards
{
    public class ProjectItemCreationWizard : IWizard
    {
        private readonly IPlcncliCommunication _plcncliCommunication;

        public ProjectItemCreationWizard()
        {
            _plcncliCommunication = Package.GetGlobalService(typeof(SPlcncliCommunication)) as IPlcncliCommunication;

        }
        public void RunStarted(object automationObject, Dictionary<string, string> replacementsDictionary, WizardRunKind runKind,
            object[] customParams)
        {
            string itemName = replacementsDictionary["$safeitemname$"];
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
                        string itemType = string.Empty;
                        GetWizardDataFromTemplate();

                        string projectDirectory = Path.GetDirectoryName(project.FullName);
                        ProjectInformationCommandResult projectInformation = null;
                        
                        ThreadHelper.JoinableTaskFactory.Run(
                            "Fetching project information",
                            async (progress) =>
                            {
                                projectInformation = _plcncliCommunication.ExecuteCommand(Constants.Command_get_project_information, null,
                                    typeof(ProjectInformationCommandResult),
                                    Constants.Option_get_project_information_no_include_detection,
                                    Constants.Option_get_project_information_project, $"\"{projectDirectory}\"") as ProjectInformationCommandResult;
                                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                            });
                        string projecttype = projectInformation.Type;
                        
                        if (projecttype == null || !validProjectTypes.Contains(projecttype))
                        {
                            MessageBox.Show(
                                $"This template is not available for the selected project. The template is available for the project types" +
                                $"{validProjectTypes.Aggregate(string.Empty, (s1, s2) => s1 + $"\n'{s2}'")}\nbut the selected project is of type\n'{projecttype}'.",
                                "Template not available for project type",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                            throw new WizardBackoutException();
                        }

                        NewItemModel model = new NewItemModel(itemType, itemName, projectInformation);
                        NewItemViewModel viewModel = new NewItemViewModel(model);
                        NewItemDialogView view = new NewItemDialogView(viewModel);

                        bool? result = view.ShowModal();
                        if (result != null && result == true)
                        {
                            itemName = model.SelectedName;
                            try
                            {
                                if (itemType.Equals(Constants.ItemType_program))
                                {
                                    _plcncliCommunication.ExecuteCommand(Constants.Command_new_program, null, null,
                                        Constants.Option_new_program_project, $"\"{projectDirectory}\"",
                                        Constants.Option_new_program_name, itemName,
                                        Constants.Option_new_program_component, model.SelectedComponent,
                                        Constants.Option_new_program_namespace, model.SelectedNamespace);
                                }
                                else if (itemType.Equals(Constants.ItemType_component))
                                {
                                    string command = Constants.Command_new_component;
                                    if (projecttype.Equals(Constants.ProjectType_ACF))
                                    {
                                        command = Constants.Command_new_acfcomponent;
                                    }
                                    _plcncliCommunication.ExecuteCommand(command, null, null,
                                        Constants.Option_new_component_project, $"\"{projectDirectory}\"",
                                        Constants.Option_new_component_name, itemName,
                                        Constants.Option_new_component_namespace, model.SelectedNamespace);
                                }

                                string[] itemFiles = Directory.GetFiles(Path.Combine(projectDirectory, "src"), $"{itemName}.*pp");
                                foreach (string itemFile in itemFiles)
                                {
                                    project.ProjectItems.AddFromFile(itemFile);
                                }

                                _plcncliCommunication.ExecuteCommand(Constants.Command_generate_code, null, null,
                                    Constants.Option_generate_code_project, $"\"{projectDirectory}\"");
                            }catch(PlcncliException e)
                            {
                                MessageBox.Show(e.Message, "Error occured", MessageBoxButton.OK, MessageBoxImage.Error);
                                throw new WizardBackoutException();
                            }
                        }
                        else
                        {
                            throw new WizardBackoutException();
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
