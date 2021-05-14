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
using System.Text.RegularExpressions;
using System.Windows;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TemplateWizard;
using Microsoft.VisualStudio.VCProjectEngine;
using PlcncliServices.CommandResults;
using PlcncliServices.PLCnCLI;
using PlcNextVSExtension.PlcNextProject.NewProjectInformationDialog;
using PlcNextVSExtension.Properties;

namespace PlcNextVSExtension.PlcNextProject
{
    class ProjectCreationWizard : IWizard
    {
        private readonly IPlcncliCommunication _plcncliCommunication;
        private string _projectDirectory;
        private string _componentName;
        private string _programName;
        private string _projectNamespace;
        private string _projectType;
        private IEnumerable<TargetResult> _projectTargets;
        private Project _project;
        private string _solutionDirectory;

        private static readonly Regex ProjectNameRegex = new Regex(@"^(?:[a-zA-Z][a-zA-Z0-9_]*\.)*[A-Z](?!.*__)[a-zA-Z0-9_]*$", RegexOptions.Compiled);

        public ProjectCreationWizard()
        {
            _plcncliCommunication = Package.GetGlobalService(typeof(SPlcncliCommunication)) as IPlcncliCommunication;

        }

        public void RunStarted(object automationObject, Dictionary<string, string> replacementsDictionary, WizardRunKind runKind,
            object[] customParams)
        {
            _projectDirectory = replacementsDictionary["$destinationdirectory$"];
            string projectName = replacementsDictionary["$projectname$"];
            _solutionDirectory = replacementsDictionary["$solutiondirectory$"];

            try
            {
                CheckProjectName();

                _projectType = Resources.ProjectType_PLM;
                if (customParams[0].ToString().EndsWith("PLCnextACFProject\\MyTemplate.vstemplate") ||
                    customParams[0].ToString().EndsWith("PLCnextACFProject/MyTemplate.vstemplate"))
                {
                    _projectType = Resources.ProjectType_ACF;
                }
                else if (customParams[0].ToString().EndsWith("ConsumableLibraryTemplate\\ProjectTemplate\\MyTemplate.vstemplate") ||
                         customParams[0].ToString().EndsWith("ConsumableLibraryTemplate/ProjectTemplate/MyTemplate.vstemplate"))
                {
                    _projectType = Resources.ProjectType_ConsumableLibrary;
                }

                NewProjectInformationModel model = new NewProjectInformationModel(_plcncliCommunication, projectName, _projectType);
                NewProjectInformationViewModel viewModel = new NewProjectInformationViewModel(model);
                NewProjectInformationView view = new NewProjectInformationView(viewModel);

                bool? result = view.ShowModal();
                if (result != true)
                {
                    throw new WizardCancelledException();
                }
                _componentName = model.InitialComponentName;
                _programName = model.InitialProgramName;
                _projectNamespace = model.ProjectNamespace;
                _projectTargets = model.ProjectTargets;

                void CheckProjectName()
                {
                    if (ProjectNameRegex.IsMatch(projectName))
                    {
                        return;
                    }

                    if (projectName.Length == 0)
                    {
                        throw new WizardBackoutException("Project name cannot be empty.");
                    }

                    if (Char.IsLower(projectName.First()))
                    {
                        throw new WizardBackoutException("Project name cannot start with lowercase character.");
                    }

                    throw new WizardBackoutException("Project name does not match pattern ^[A-Z](?!.*__)[a-zA-Z0-9_]*$");
                }
            }
            catch (WizardBackoutException e)
            {
                try
                {
                    DeleteProjectDirectory();
#pragma warning disable VSTHRD010 // Invoke single-threaded types on Main thread
                    DeleteSolutionFolderIfEmpty((DTE)automationObject);
#pragma warning restore VSTHRD010 // Invoke single-threaded types on Main thread
                }
                catch (Exception)
                { }
                MessageBox.Show($"{e.Message}\n\n Reopen 'New Project' dialog", "Project creation failed");
                throw e;
            }
            catch(WizardCancelledException e)
            {
                try
                {
                    DeleteProjectDirectory();
#pragma warning disable VSTHRD010 // Invoke single-threaded types on Main thread
                    DeleteSolutionFolderIfEmpty((DTE)automationObject);
#pragma warning restore VSTHRD010 // Invoke single-threaded types on Main thread
                }
                catch (Exception)
                { }
                throw e;
            }


            void DeleteProjectDirectory()
            {
                string parentDirectory = Directory.GetParent(_projectDirectory).FullName;
                Directory.Delete(_projectDirectory);
                Directory.Delete(parentDirectory);
            }
        }

        private void DeleteSolutionFolderIfEmpty(DTE dte)
        {
            //first check if solution is empty
            string[] solutionDirectoryEntries = Directory.GetFileSystemEntries(_solutionDirectory);
            if (solutionDirectoryEntries.Where(entry => entry != ".vs").Any())
            {
                return;//solution directory is not empty, do not delete!
            }

            ThreadHelper.ThrowIfNotOnUIThread();
            dte.Solution.Close();

            Directory.Delete(_solutionDirectory, true);
        }

        public void ProjectFinishedGenerating(Project project)
        {
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                GeneratePLCnCLIProject();
            }
            catch (Exception e)
            {
                try
                {
                    project.DTE.Solution.Remove(project);
                    DeleteSolutionFolderIfEmpty(project.DTE);
                }
                catch (Exception)
                {}

                throw e;
            }

            void GeneratePLCnCLIProject()
            {
                _project = project;
                VCProject p = _project.Object as VCProject;

                //**********create new plcncli project**********
                //**********get project type**********
                VCConfiguration configuration = p.ActiveConfiguration;
                IVCRulePropertyStorage plcnextRule = configuration.Rules.Item(Constants.PLCnextRuleName);
                if (plcnextRule == null)
                {
                    MessageBox.Show("PLCnextCommonProperties rule was not found in configuration rules collection.");
                }
                string projectType = plcnextRule.GetUnevaluatedPropertyValue("ProjectType_");

                string newProjectCommand = Resources.Command_new_plmproject;
                List<string> newProjectArguments = new List<string>
                {
                Resources.Option_new_project_output, $"\"{_projectDirectory}\"",
                Resources.Option_new_project_projectNamespace, _projectNamespace
                };

                if (!projectType.Equals(Resources.ProjectType_ConsumableLibrary))
                {
                    newProjectArguments.Add(Resources.Option_new_project_componentName);
                    newProjectArguments.Add(_componentName);

                    if (projectType.Equals(Resources.ProjectType_PLM))
                    {
                        newProjectArguments.Add(Resources.Option_new_project_programName);
                        newProjectArguments.Add(_programName);
                    }
                }

                if (projectType.Equals(Resources.ProjectType_ACF))
                {
                    newProjectCommand = Resources.Command_new_acfproject;
                }
                else if (projectType.Equals(Resources.ProjectType_ConsumableLibrary))
                {
                    newProjectCommand = Resources.Command_new_consumablelibrary;
                }

                _plcncliCommunication.ExecuteCommand(newProjectCommand, null, null, newProjectArguments.ToArray());


                //**********create configurations**********
                ProjectConfigurationManager.CreateConfigurationsForAllProjectTargets
                    (_projectTargets.Select(t => t.GetNameFormattedForCommandLine()), project);

                foreach (TargetResult target in _projectTargets)
                {
                    //**********set project target**********
                    _plcncliCommunication.ExecuteCommand(Resources.Command_set_target, null, null,
                        Resources.Option_set_target_add, Resources.Option_set_target_name, target.Name,
                        Resources.Option_set_target_version, target.Version, Resources.Option_set_target_project,
                        $"\"{_projectDirectory}\"");
                }


                //add project items to project
                IEnumerable<string> projectFiles =
                    Directory.GetFiles(_projectDirectory, "*.*pp", SearchOption.AllDirectories)
                    .Concat(Directory.GetFiles(_projectDirectory, "*.txt", SearchOption.AllDirectories))
                    .Where(f => !f.EndsWith("UndefClang.hpp"));

                foreach (string file in projectFiles)
                {
                    project.ProjectItems.AddFromFile(file);
                }

                //**********generate code**********
                _plcncliCommunication.ExecuteCommand(Resources.Command_generate_code, null, null, Resources.Option_generate_code_project, $"\"{_projectDirectory}\"");

                ProjectInformationCommandResult projectInformation = _plcncliCommunication.ExecuteCommand(Resources.Command_get_project_information, null,
                    typeof(ProjectInformationCommandResult), Resources.Option_get_project_information_project, $"\"{_projectDirectory}\"") as ProjectInformationCommandResult;

                CompilerSpecificationCommandResult compilerSpecsCommandResult =
                    _plcncliCommunication.ExecuteCommand(Resources.Command_get_compiler_specifications, null,
                            typeof(CompilerSpecificationCommandResult), Resources.Option_get_compiler_specifications_project, $"\"{_projectDirectory}\"") as
                        CompilerSpecificationCommandResult;

                ProjectIncludesManager.SetIncludesForNewProject(p, compilerSpecsCommandResult, projectInformation);
            }
        }

        

        public void ProjectItemFinishedGenerating(ProjectItem projectItem)
        {
        }

        public bool ShouldAddProjectItem(string filePath)
        {
            return true;
        }

        public void BeforeOpeningFile(ProjectItem projectItem)
        {
        }

        public void RunFinished()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            //remove UndefClang
            ProjectItem itemToRemove = null;
            foreach (var item in _project.ProjectItems)
            {
                if (!(item is ProjectItem projectItem) || !projectItem.Name.EndsWith("UndefClang.hpp")) continue;
                itemToRemove = projectItem;
                break;
            }
            itemToRemove?.Remove();
        }
    }
}
