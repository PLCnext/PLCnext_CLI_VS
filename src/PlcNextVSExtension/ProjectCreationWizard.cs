#region Copyright
///////////////////////////////////////////////////////////////////////////////
//
//  Copyright (c) Phoenix Contact GmbH & Co KG
//  This software is licensed under Apache-2.0
//
///////////////////////////////////////////////////////////////////////////////
#endregion

using System.Collections.Generic;
using System.IO;
using System.Linq;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TemplateWizard;
using Microsoft.VisualStudio.VCProjectEngine;
using PlcncliServices.CommandResults;
using PlcncliServices.PLCnCLI;
using PlcNextVSExtension.NewProjectInformationDialog;
using PlcNextVSExtension.Properties;

namespace PlcNextVSExtension
{
    class ProjectCreationWizard : IWizard
    {
        private IPlcncliCommunication _plcncliCommunication;
        private string _projectDirectory;
        private string _componentName;
        private string _programName;
        private string _projectNamespace;
        private string _projectType;
        private IEnumerable<TargetResult> _projectTargets;
        private Project _project;
        public ProjectCreationWizard()
        {
            _plcncliCommunication = Package.GetGlobalService(typeof(SPlcncliCommunication)) as IPlcncliCommunication;

        }

        public void RunStarted(object automationObject, Dictionary<string, string> replacementsDictionary, WizardRunKind runKind,
            object[] customParams)
        {
            _projectDirectory = replacementsDictionary["$destinationdirectory$"];
            string projectName = replacementsDictionary["$projectname$"];

            _projectType = Resources.ProjectType_PLM;
            if (customParams[0].ToString().EndsWith("PLCnextACFProject\\MyTemplate.vstemplate") ||
                customParams[0].ToString().EndsWith("PLCnextACFProject/MyTemplate.vstemplate"))
            {
                _projectType = Resources.ProjectType_ACF;
            }

            NewProjectInformationModel model = new NewProjectInformationModel(_plcncliCommunication, _projectDirectory, projectName, _projectType);
            NewProjectInformationViewModel viewModel = new NewProjectInformationViewModel(model);
            NewProjectInformationView view = new NewProjectInformationView(viewModel);

            view.ShowModal();

            _componentName = model.InitialComponentName;
            _programName = model.InitialProgramName;
            _projectNamespace = model.ProjectNamespace;
            _projectTargets = model.ProjectTargets;

        }

        public void ProjectFinishedGenerating(Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            _project = project;
            VCProject p = _project.Object as VCProject;

            //**********create new plcncli project**********
            //**********get project type**********
            VCConfiguration configuration = p.ActiveConfiguration;
            IVCRulePropertyStorage plcnextRule = configuration.Rules.Item("PLCnextCommonProperties");
            string projectType = plcnextRule.GetUnevaluatedPropertyValue("ProjectType_");

            string newProjectCommand = Resources.Command_new_plmproject;
            List<string> newProjectArguments = new List<string>
            {
                Resources.Option_new_project_output, $"\"{_projectDirectory}\"",
                Resources.Option_new_project_componentName, _componentName,
                Resources.Option_new_project_projectNamespace, _projectNamespace
            };

            if (projectType.Equals(Resources.ProjectType_PLM))
            {
                newProjectArguments.Add(Resources.Option_new_project_programName);
                newProjectArguments.Add(_programName);
            }

            if (projectType.Equals(Resources.ProjectType_ACF))
            {
                newProjectCommand = Resources.Command_new_acfproject;
            }

            _plcncliCommunication.ExecuteCommand(newProjectCommand, null, null, newProjectArguments.ToArray());

            
            foreach (TargetResult target in _projectTargets)
            {
                //**********create configurations**********
                //disabled, for the moment the tool only supports build all
                //project.ConfigurationManager.AddConfigurationRow($"Release {target.GetDisplayName()}", "Release - all Targets", false);
                //project.ConfigurationManager.AddConfigurationRow($"Debug {target.GetDisplayName()}", "Debug - all Targets", false);

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


            IEnumerable<CompilerMacroResult> macros = compilerSpecsCommandResult?.Specifications.FirstOrDefault()
                ?.CompilerMacros.Where(m => !m.Name.StartsWith("__has_include("));
            if (macros == null || !macros.Any()) return;

            foreach (VCConfiguration2 config in p.Configurations)
            {
                IVCRulePropertyStorage rule = config.Rules.Item("ConfigurationDirectories");
                string propKey = "IncludePath";
                string includes = string.Join(";", projectInformation.IncludePaths.Select(path => path.PathValue));

                rule.SetPropertyValue(propKey, includes);
                
                IVCRulePropertyStorage clRule = config.Rules.Item("CL");
                string key = "PreprocessorDefinitions";
                string macro = string.Join(";",
                    macros.Select(m => m.Name + (m.Value != null ? "=" + m.Value : "")));
                clRule.SetPropertyValue(key, macro);
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
