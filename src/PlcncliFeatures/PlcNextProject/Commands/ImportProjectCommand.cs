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
using System.ComponentModel.Design;
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
using MessageBox = System.Windows.MessageBox;
using Path = System.IO.Path;
using Task = System.Threading.Tasks.Task;
using Constants = PlcncliCommonUtils.Constants;
using PlcncliCommonUtils;
using PlcncliFeatures.PlcNextProject.Import;

namespace PlcncliFeatures.PlcNextProject.Commands
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class ImportProjectCommand
    {
        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;
        private readonly IPlcncliCommunication _plcncliCommunication;
        private static readonly Regex ReplaceParameterRegex = new Regex(@"\{\$(?<parameter>.*)\$\}", RegexOptions.Compiled);

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        public IAsyncServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0010;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("d03b062f-5831-4deb-b619-beb902e75a3e");

        /// <summary>
        /// Initializes a new instance of the <see cref="ImportProjectCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private ImportProjectCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));

            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));
            _plcncliCommunication = Package.GetGlobalService(typeof(SPlcncliCommunication)) as IPlcncliCommunication;

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new OleMenuCommand(this.Import, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static ImportProjectCommand Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread - the call to AddCommand in ImportProjectCommand's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new ImportProjectCommand(package, commandService);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void Import(object sender, EventArgs e)
        {
            DTE2 dte = Package.GetGlobalService(typeof(DTE)) as DTE2;

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

                    if (_plcncliCommunication == null)
                    {
                        MessageBox.Show("Could not import project because no plcncli communication found.");
                    }
                    string projectFilePath = string.Empty;
                    if (OpenImportWizard())
                    {
                        string projectDirectory = Path.GetDirectoryName(projectFilePath);

                        progress.Report(new ThreadedWaitDialogProgressData("Fetching project information."));
                        ProjectInformationCommandResult projectInformation = await GetProjectInformation();
                        if (projectInformation == null)
                        {
                            return;
                        }
                        string projectName = projectInformation.Name;
                        string projectType = projectInformation.Type;
                        IEnumerable<TargetResult> projectTargets = projectInformation.Targets;

                        await CreateVSProject(projectType, projectDirectory, projectName, projectTargets);

                        MessageBox.Show("If the imported project has source folders different from the standard 'src', they have to be set manually in the project properties.",
                                        "Successfully imported project", MessageBoxButton.OK, MessageBoxImage.Information);
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

                    async Task<ProjectInformationCommandResult> GetProjectInformation()
                    {
                        ProjectInformationCommandResult result = null;
                        await Task.Run(() =>
                        {
                            try
                            {
                                result = _plcncliCommunication.ExecuteCommand(Constants.Command_get_project_information, null,
                                    typeof(ProjectInformationCommandResult), Constants.Option_get_project_information_project,
                                    $"\"{projectFilePath}\"") as ProjectInformationCommandResult;
                            }
                            catch (PlcncliException ex)
                            {
                                result = _plcncliCommunication.ConvertToTypedCommandResult<ProjectInformationCommandResult>(ex.InfoMessages);
                                throw ex;
                            }
                        });
                        return result;
                    }

                    async Task CreateVSProject(string projectType, string projectDirectory, string projectName, IEnumerable<TargetResult> projectTargets)
                    {

                        progress.Report(new ThreadedWaitDialogProgressData("Creating project files."));
                        bool projectFileCreated = await CreateVSProjectFile();
                        if (!projectFileCreated)
                            return;

                        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                        dte.Solution.Create(projectDirectory, projectName);
                        dte.Solution.AddFromFile($"{Path.Combine(projectDirectory, projectName)}.vcxproj");

                        Project project = null;
                        foreach (Project proj in dte.Solution.Projects)
                        {
                            if (proj.Name.Equals(projectName))
                            {
                                project = proj;
                                break;
                            }
                        }
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
                                               .Where(f => !f.EndsWith("UndefClang.hpp"))
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
                            string filePath = Path.Combine(projectDirectory, $"{projectName}.vcxproj");
                            if (File.Exists(filePath))
                            {
                                MessageBox.Show($"Project creation failed because the file {filePath} already exists.");
                                return false;
                            }

                            string additionalPropertiesKey = "additionalproperties";
                            string projectTypeKey = "projecttype";
                            string targetsFileNameKey = "targetsfilename";

                            Dictionary<string, string> replacementDictionary = new Dictionary<string, string>
                                    {
                                    { additionalPropertiesKey, string.Empty },
                                    { projectTypeKey, projectType }
                                    };
                            if (projectType == Constants.ProjectType_PLM)
                            {
                                replacementDictionary[additionalPropertiesKey] = "<PLCnCLIGenerateDT>true</PLCnCLIGenerateDT>";
                            }

                            if (projectType == Constants.ProjectType_ConsumableLibrary)
                            {
                                replacementDictionary.Add(targetsFileNameKey, "PLCnCLIBuild.targets");
                            }
                            else
                            {
                                replacementDictionary.Add(targetsFileNameKey, "PLCnCLI.targets");
                            }

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
                                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("PlcncliFeatures.PlcNextProject.Import.ProjectTemplate.UndefClang.hpp"))
                                using (StreamReader reader = new StreamReader(stream))
                                {
                                    string content = await reader.ReadToEndAsync();
                                    File.WriteAllText(Path.Combine(projectDirectory, "UndefClang.hpp"), content);
                                }
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show(ex.Message + ex.StackTrace ?? string.Empty, $"Exception during writing of {filePath}");
                                return false;
                            }

                            return true;

                            async Task<string> GetProjectFileTemplate()
                            {
                                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("PlcncliFeatures.PlcNextProject.Import.ProjectTemplate.PLCnextImportTemplate.vcxproj"))
                                using (StreamReader reader = new StreamReader(stream))
                                {
                                    return await reader.ReadToEndAsync();
                                }
                            }
                        }

                        void GenerateCode()
                        {
                            _plcncliCommunication.ExecuteCommand(Constants.Command_generate_code, null, null, Constants.Option_generate_code_project, $"\"{projectDirectory}\"");
                        }

                        void SetIncludesAndMacros()
                        {
                            ProjectInformationCommandResult projectInformation = _plcncliCommunication.ExecuteCommand(Constants.Command_get_project_information, null,
                                typeof(ProjectInformationCommandResult), Constants.Option_get_project_information_project, $"\"{projectDirectory}\"") as ProjectInformationCommandResult;

                            CompilerSpecificationCommandResult compilerSpecsCommandResult =
                                _plcncliCommunication.ExecuteCommand(Constants.Command_get_compiler_specifications, null,
                                        typeof(CompilerSpecificationCommandResult), Constants.Option_get_compiler_specifications_project, $"\"{projectDirectory}\"") as
                                    CompilerSpecificationCommandResult;

                            VCProject vcProject = project.Object as VCProject;
                            ProjectIncludesManager.SetIncludesForNewProject(vcProject, compilerSpecsCommandResult, projectInformation);
                        }
                    }
                });
        }
    }
}
