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
using PlcNextVSExtension.CommandResults;
using PlcNextVSExtension.NewProjectInformationDialog;
using PlcNextVSExtension.PLCnCLI;
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
            _plcncliCommunication = new PlcncliProcessCommunication();

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

            _plcncliCommunication.ExecuteCommand(newProjectCommand, null, newProjectArguments.ToArray());

            
            foreach (TargetResult target in _projectTargets)
            {
                //**********create configurations**********
                project.ConfigurationManager.AddConfigurationRow($"Release {target.GetDisplayName()}", "Release all projecttargets", true);
                project.ConfigurationManager.AddConfigurationRow($"Debug {target.GetDisplayName()}", "Debug all projecttargets", true);

                //**********set project target**********
                _plcncliCommunication.ExecuteCommand(Resources.Command_set_target, null,
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
            _plcncliCommunication.ExecuteCommand(Resources.Command_generate_code, null, Resources.Option_generate_code_project, $"\"{_projectDirectory}\"");
            
            ProjectInformationCommandResult projectInformation = _plcncliCommunication.ExecuteCommand(Resources.Command_get_project_information,
                typeof(ProjectInformationCommandResult), Resources.Option_get_project_information_project, $"\"{_projectDirectory}\"") as ProjectInformationCommandResult;

            CompilerSpecificationCommandResult compilerSpecsCommandResult =
                _plcncliCommunication.ExecuteCommand(Resources.Command_get_compiler_specifications,
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

        //public void RunFinished()
        //{
        //    ThreadHelper.ThrowIfNotOnUIThread();
        //    DTE2 dte = (DTE2)Package.GetGlobalService(typeof(DTE));

        //    VCProject p = dte.Solution.Projects.Item(1).Object as VCProject;
        //    //Project dteProject = dte.Solution.Projects.Item(1);

        //    ProjectInformationCommandResult projectInformation = _plcncliCommunication.ExecuteCommand(Resources.Command_get_project-information,
        //        typeof(ProjectInformationCommandResult), Resources.Option_get_project-information_project, $"\"{_projectDirectory}\"") as ProjectInformationCommandResult;

        //    CompilerSpecificationCommandResult compilerSpecsCommandResult =
        //        _plcncliCommunication.ExecuteCommand(Resources.Command_get_compiler-specifications,
        //                typeof(CompilerSpecificationCommandResult), Resources.Option_get_compiler-specifications_project, $"\"{_projectDirectory}\"") as
        //            CompilerSpecificationCommandResult;


        //        IEnumerable<CompilerMacroResult> macros = compilerSpecsCommandResult?.Specifications.FirstOrDefault()
        //            ?.CompilerMacros.Where(m => !m.Name.StartsWith("__has_include("));
        //        if (macros == null || !macros.Any()) return;

        //        foreach (VCConfiguration2 config in p.Configurations)
        //        {
        //            IVCRulePropertyStorage rule = config.Rules.Item("ConfigurationDirectories");
        //            string propKey = "IncludePath";
        //            string includes = string.Join(";", projectInformation.IncludePaths.Select(path => path.PathValue));

        //            rule.SetPropertyValue(propKey, includes);

        //            IVCRulePropertyStorage clRule = config.Rules.Item("CL");
        //            string key = "PreprocessorDefinitions";
        //            string macro = string.Join(";",
        //                macros.Select(m => m.Name + (m.Value != null ? "=" + m.Value : "")));
        //            clRule.SetPropertyValue(key, macro);



        //        //IEnumerator enumerator = config.Rules.GetEnumerator();
        //            //while (enumerator.MoveNext())
        //            //{
        //            //    if(enumerator.Current is IVCRulePropertyStorage2 storage)
        //            //    {
        //            //        Debug.WriteLine("****Found a rule with the following name:");
        //            //        Debug.WriteLine(storage.Name);

        //            //    }
        //            //}

        //            //var projectEngine = config.VCProjectEngine as VCProjectEngine;
        //            //if (projectEngine == null)
        //            //{
        //            //    Debug.WriteLine("Project engine is null");
        //            //    return;
        //            //}

        //            //var x = p.GetVCService();

        //            //helpercode to find correct rule and/or property name
        //            //foreach (IVCRulePropertyStorage2 r in config.Rules)
        //            //{
        //            //    try
        //            //    {
        //            //        Debug.WriteLine(r.GetUnevaluatedPropertyValue("ClCompile.ClangMode"));
        //            //        Debug.WriteLine("Success");
        //            //        Debug.WriteLine(r.Name);

        //            //    }
        //            //    catch (Exception)
        //            //    { }
        //            //}


        //            //IVCProjectBuildService x = projectEngine.;
        //            //IVCPropertyStorage propStorage = x.

        //            // way to set the 'Project public include paths'
        //            //IVCRulePropertyStorage rule = config.Rules.Item("CL");
        //            //string propKey = "ProjectPublicIncludePath";



        //        }


        //    //// INFOS 
        //    ////IVSProject4 .ContainsFileEndingWith -> check for plcncli project file ?
        //    ////IVsAddProjectItemDlg -> access to add project item dialog

        //    //var xyz = Package.GetGlobalService(typeof(IVsSolution));

        //    //if (xyz != null)
        //    //{
        //    //    if (xyz is IVsSolution sln)
        //    //    {
        //    //        int returnCode = sln.GetProjectOfUniqueName(dteProject.UniqueName, out var hierarchy);
        //    //        if (returnCode == 0)
        //    //        {
        //    //            Debug.WriteLine("Project kind: " + dteProject.Kind);
        //    //        }
        //    //    }
        //    //}


        //    ////            if (dte.Solution.Projects.Item(1) is Project project)
        //    ////            {
        //    ////                foreach (Property property in project.Properties)
        //    ////                {
        //    ////                    if (property != null)
        //    ////                    {
        //    ////                        Debug.WriteLine("Name: "+property.Name);
        //    ////                        try
        //    ////                        {
        //    ////                            Debug.WriteLine("Value: "+property.Value);
        //    ////                        }
        //    ////                        catch(Exception)
        //    ////                        { }
        //    ////                    }
        //    ////                    else
        //    ////                        Debug.WriteLine("--------property was null");
        //    ////                }
        //    ////            }

        //    ////var dte = Package.GetGlobalService(typeof(_DTE)) as DTE2;


        //}
    }
}
