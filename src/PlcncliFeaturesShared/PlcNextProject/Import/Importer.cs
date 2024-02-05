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
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.VCProjectEngine;
using PlcncliServices.CommandResults;
using PlcncliServices.PLCnCLI;
using Path = System.IO.Path;
using Task = System.Threading.Tasks.Task;
using Constants = PlcncliCommonUtils.Constants;
using PlcncliCommonUtils;

namespace PlcncliFeatures.PlcNextProject.Import
{
    internal class Importer
    {
        private static readonly Regex ReplaceParameterRegex = new Regex(@"\{\$(?<parameter>.*)\$\}", RegexOptions.Compiled);


        internal static void ImportProject(DTE2 dte, IPlcncliCommunication plcncliCommunication)
        {
            try
            {
                bool closed = SolutionSaveService.SaveAndCloseSolution(dte.Solution);
                if (!closed)
                    return;
            }
            catch (Exception exception)
            {
                MessageBox.Show($"Project import could not be started:\n{exception.Message}", "Import failed");
                return;
            }

            ThreadHelper.JoinableTaskFactory.Run(
                "Importing project",
                async (progress) =>
                {
                    IProjectTypeImporter projectTypeImporter;


                    if (plcncliCommunication == null)
                    {
                        MessageBox.Show("Could not import project because no plcncli communication found.");
                    }
                    string projectFilePath = string.Empty;
                    if (OpenImportWizard())
                    {
                        string projectDirectory = Path.GetDirectoryName(projectFilePath);

                        progress.Report(new ThreadedWaitDialogProgressData("Checking project version."));
                        if (!CheckProject())
                        {
                            return;
                        }

                        progress.Report(new ThreadedWaitDialogProgressData("Fetching project information."));
                        ProjectInformationCommandResult projectInformation = await GetProjectInformation();
                        if (projectInformation == null)
                        {
                            return;
                        }
                        string projectName = projectInformation.Name;
                        string projectType = projectInformation.Type;
                        projectTypeImporter = CommonProjectImporter.GetProjectTypeImporter(projectType);
                        IEnumerable<TargetResult> projectTargets = projectInformation.Targets;

                        await CreateVSProject(projectDirectory, projectName, projectTargets);

                        projectTypeImporter.ShowFinalMessage();
                    }

                    bool OpenImportWizard()
                    {
                        ImportDialogModel model = new ImportDialogModel();
                        ImportDialogViewModel viewModel = new ImportDialogViewModel(model);
                        ImportDialogView view = new ImportDialogView(viewModel);

                        view.ShowModal();
                        if (view.DialogResult == true)
                        {
                            projectFilePath = model.ProjectFilePath;
                            return true;
                        }
                        return false;
                    }

                    bool CheckProject()
                    {
                        //check projectversion is compatible to PLCnCLI
                        try
                        {
                            plcncliCommunication.ExecuteWithoutResult(Constants.Command_check_project, null,
                                Constants.Option_check_project_project, $"\"{Path.GetDirectoryName(projectFilePath)}\"");
                            return true;
                        }
                        catch (PlcncliException ex)
                        {
                            MessageBox.Show(ex.Message, "Project problem detected ", MessageBoxButton.OK, MessageBoxImage.Error);
                            return false;
                        }
                    }

                    async Task<ProjectInformationCommandResult> GetProjectInformation()
                    {
                        ProjectInformationCommandResult result = null;
                        await Task.Run(() =>
                        {
                            try
                            {
                                result = plcncliCommunication.ExecuteCommand(Constants.Command_get_project_information, null,
                                    typeof(ProjectInformationCommandResult), Constants.Option_get_project_information_project,
                                    $"\"{projectFilePath}\"") as ProjectInformationCommandResult;
                            }
                            catch (PlcncliException ex)
                            {
                                result = plcncliCommunication.ConvertToTypedCommandResult<ProjectInformationCommandResult>(ex.InfoMessages);
                                throw ex;
                            }
                        });
                        return result;
                    }

                    async Task CreateVSProject(string projectDirectory, string projectName, IEnumerable<TargetResult> projectTargets)
                    {

                        progress.Report(new ThreadedWaitDialogProgressData("Creating project files."));
                        bool projectFileCreated = await CreateVSProjectFile();
                        if (!projectFileCreated)
                            return;

                        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                        dte.Solution.Create(projectTypeImporter.GetSolutionDirectory(projectDirectory), projectName);
                        Project project = dte.Solution.AddFromFile(projectTypeImporter.GetProjectFilePath(projectDirectory, projectName));
                        projectTypeImporter.AddAdditionalProjects(dte.Solution, projectDirectory, projectName);

                        if (project == null)
                        {
                            MessageBox.Show("Something went wrong during creation of new project. Project was not found in solution.");
                            return;
                        }

                        progress.Report(new ThreadedWaitDialogProgressData("Creating project configurations."));
                        ProjectConfigurationManager.CreateConfigurationsForAllProjectTargets
                            (projectTargets.Select(t => t.GetNameFormattedForCommandLine()), project);

                        //**********delete intermediate**********
                        string intermediateFolder = Path.Combine(projectDirectory, "intermediate");
                        if (Directory.Exists(intermediateFolder))
                        {
                            Directory.Delete(intermediateFolder, true);
                        }

                        //**********add project items to project**********

                        IEnumerable<string> directories = Directory.GetDirectories(projectDirectory).Where(d => !d.EndsWith("bin"));

                        IEnumerable<string> projectFiles =
                            Directory.GetFiles(projectDirectory, "*.*pp", SearchOption.TopDirectoryOnly)
                            .Concat(Directory.GetFiles(projectDirectory, "*.txt", SearchOption.TopDirectoryOnly))
                            .Where(f => !f.EndsWith("UndefClang.hpp"))
                            .Concat(directories.SelectMany(d => Directory.GetFiles(d, "*.*pp", SearchOption.AllDirectories)
                                                                     .Concat(Directory.GetFiles(d, "*.txt", SearchOption.AllDirectories)))
                                               .Where(f => !f.EndsWith("UndefClang.hpp") && !f.EndsWith("ADD_DEPENDENT_LIBRARIES_HERE.txt"))
                            );

                        foreach (string file in projectFiles)
                        {
                            project.ProjectItems.AddFromFile(file);
                        }

                        progress.Report(new ThreadedWaitDialogProgressData("Generating intermediate code."));
                        GenerateCode();

                        progress.Report(new ThreadedWaitDialogProgressData("Setting includes and macros."));
                        SetIncludesAndMacros();

                        project.Save();

                        async Task<bool> CreateVSProjectFile()
                        {
                            string filePath = projectTypeImporter.GetProjectFilePath(projectDirectory, projectName);
                            
                            if (File.Exists(filePath))
                            {
                                MessageBox.Show($"Project creation failed because the file {filePath} already exists.");
                                return false;
                            }

                            string projectTypeKey = "projecttype";
                            string targetsFileNameKey = "targetsfilename";

                            Dictionary<string, string> replacementDictionary = new Dictionary<string, string>
                                    {
                                    { projectTypeKey, projectTypeImporter.ProjectType }
                                    };
                            replacementDictionary.Add(targetsFileNameKey, projectTypeImporter.GetTargetsFileName());
                            
                            string fileContent = await GetProjectFileTemplate();
                            Match replaceParameterMatch = ReplaceParameterRegex.Match(fileContent);

                            while (replaceParameterMatch.Success)
                            {
                                string parameter = replaceParameterMatch.Groups["parameter"].Value;
                                if (!replacementDictionary.ContainsKey(parameter))
                                {
                                    MessageBox.Show($"The parameter {parameter} could not be replaced in the project file.");
                                    replaceParameterMatch = replaceParameterMatch.NextMatch();
                                    continue;
                                }
                                fileContent = fileContent.Replace(replaceParameterMatch.Value, replacementDictionary[parameter]);

                                replaceParameterMatch = replaceParameterMatch.NextMatch();
                            }


                            try
                            {
                                File.WriteAllText(filePath, fileContent);

                                await projectTypeImporter.AddAdditionalFilesAsync(projectDirectory);
                                
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show(ex.Message + ex.StackTrace ?? string.Empty, $"Exception during writing of {filePath}");
                                return false;
                            }

                            return true;

                            async Task<string> GetProjectFileTemplate()
                            {
                                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"PlcncliFeatures.PlcNextProject.Import.ProjectTemplate.{projectTypeImporter.GetProjectTemplateName()}"))
                                using (StreamReader reader = new StreamReader(stream))
                                {
                                    return await reader.ReadToEndAsync();
                                }
                            }
                        }

                        void GenerateCode()
                        {
                            plcncliCommunication.ExecuteCommand(Constants.Command_generate_code, null, null, Constants.Option_generate_code_project, $"\"{projectDirectory}\"");
                        }

                        void SetIncludesAndMacros()
                        {
                            ProjectInformationCommandResult projectInformation = null;
                            try
                            {
                                projectInformation = plcncliCommunication.ExecuteCommand(Constants.Command_get_project_information, null,
                                    typeof(ProjectInformationCommandResult), Constants.Option_get_project_information_project, $"\"{projectDirectory}\"") as ProjectInformationCommandResult;
                            }
                            catch (PlcncliException ex)
                            {
                                projectInformation = plcncliCommunication.ConvertToTypedCommandResult<ProjectInformationCommandResult>(ex.InfoMessages);
                            }

                            CompilerSpecificationCommandResult compilerSpecsCommandResult = null;
                            try
                            {
                                compilerSpecsCommandResult =
                                    plcncliCommunication.ExecuteCommand(Constants.Command_get_compiler_specifications, null,
                                            typeof(CompilerSpecificationCommandResult), Constants.Option_get_compiler_specifications_project, $"\"{projectDirectory}\"") as
                                        CompilerSpecificationCommandResult;
                            }
                            catch (PlcncliException ex)
                            {
                                compilerSpecsCommandResult = plcncliCommunication.ConvertToTypedCommandResult<CompilerSpecificationCommandResult>(ex.InfoMessages);
                            }
                            VCProject vcProject = project.Object as VCProject;
                            ProjectIncludesManager.SetIncludesForNewProject(vcProject, compilerSpecsCommandResult, projectInformation);
                        }
                    }
                });
        }
    }
}
