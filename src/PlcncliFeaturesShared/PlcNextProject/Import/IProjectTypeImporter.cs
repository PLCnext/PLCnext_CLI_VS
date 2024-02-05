#region Copyright
///////////////////////////////////////////////////////////////////////////////
//
//  Copyright (c) Phoenix Contact GmbH & Co KG
//  This software is licensed under Apache-2.0
//
///////////////////////////////////////////////////////////////////////////////
#endregion

using EnvDTE;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;

using Constants = PlcncliCommonUtils.Constants;

namespace PlcncliFeatures.PlcNextProject.Import
{
    internal interface IProjectTypeImporter
    {
        string ProjectType { get; }
        void ShowFinalMessage();
        string GetProjectFilePath(string projectDirectory, string projectName);
        string GetSolutionDirectory(string projectDirectory);
        string GetTargetsFileName();
        Task AddAdditionalFilesAsync(string projectDirectory);
        void AddAdditionalProjects(Solution solution, string projectDirectory, string projectName);
        string GetProjectTemplateName();
    }

    internal class SharedNativeProjectImporter : CommonProjectImporter, IProjectTypeImporter
    {
        internal SharedNativeProjectImporter() : base(Constants.ProjectType_SN)
        {
        }

        public override void ShowFinalMessage()
        {
            //do not show any message
        }

        public override string GetProjectFilePath(string projectDirectory, string projectName)
        {
            return Path.Combine(projectDirectory, $"{projectName}Cpp.vcxproj");
        }

        public override string GetSolutionDirectory(string projectDirectory)
        {
            return Path.GetDirectoryName(projectDirectory);
        }

        public override Task AddAdditionalFilesAsync(string projectDirectory)
        {
            // do not add any additional files
            return Task.CompletedTask;
        }

        public override void AddAdditionalProjects(Solution solution, string projectDirectory, string projectName)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
            string file = Path.Combine(Path.GetDirectoryName(projectDirectory), $"{projectName}CSharp", $"{projectName}CSharp.csproj");
            try
            {
                solution.AddFromFile(file);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + ex.StackTrace ?? string.Empty, $"Exception during adding of project file {file}");
            }
        }

        public override string GetProjectTemplateName()
        {
            return "SNLtemplate.vcxproj";
        }
    }

    internal class ConsumableProjectImporter : CommonProjectImporter, IProjectTypeImporter
    {
        internal ConsumableProjectImporter() : base(Constants.ProjectType_ConsumableLibrary)
        {
        }

        public override string GetTargetsFileName()
        {
            return "PLCnCLIBuild.targets";
        }
    }

    internal class CommonProjectImporter : IProjectTypeImporter
    {
        internal CommonProjectImporter(string projectType)
        {
            ProjectType = projectType;
        }

        public string ProjectType { get; }

        public virtual void ShowFinalMessage()
        {
            MessageBox.Show("If the imported project has source folders different from the standard 'src', they have to be set manually in" +
                                " the project properties.", "Successfully imported project", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public virtual string GetProjectTemplateName()
        {
            return "PLCnextImportTemplate.vcxproj";
        }

        internal static IProjectTypeImporter GetProjectTypeImporter(string projectType)
        {
            switch (projectType)
            {
                case Constants.ProjectType_SN:
                    return new SharedNativeProjectImporter();
                case Constants.ProjectType_ConsumableLibrary:
                    return new ConsumableProjectImporter();
                default:
                    return new CommonProjectImporter(projectType);
            }
        }

        public virtual string GetProjectFilePath(string projectDirectory, string projectName)
        {
            return Path.Combine(projectDirectory, $"{projectName}.vcxproj");
        }

        public virtual string GetSolutionDirectory(string projectDirectory)
        {
            return projectDirectory;
        }

        public virtual string GetTargetsFileName()
        {
            return "PLCnCLI.targets";
        }

        public virtual async Task AddAdditionalFilesAsync(string projectDirectory)
        {
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("PlcncliFeatures.PlcNextProject.Import.ProjectTemplate.UndefClang.hpp"))
            using (StreamReader reader = new StreamReader(stream))
            {
                string content = await reader.ReadToEndAsync();
                File.WriteAllText(Path.Combine(projectDirectory, "UndefClang.hpp"), content);
            }
        }

        public virtual void AddAdditionalProjects(Solution solution, string projectDirectory, string projectName)
        {
        }
    }
}
