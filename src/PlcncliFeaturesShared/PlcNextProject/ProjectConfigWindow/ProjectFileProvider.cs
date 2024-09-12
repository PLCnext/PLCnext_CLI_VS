#region Copyright
///////////////////////////////////////////////////////////////////////////////
//
//  Copyright (c) Phoenix Contact GmbH & Co KG
//  This software is licensed under Apache-2.0
//
///////////////////////////////////////////////////////////////////////////////
#endregion

using System;
using System.IO;
using System.Threading;
using System.Xml;
using System.Xml.Serialization;

namespace PlcncliFeatures.PlcNextProject.ProjectConfigWindow
{
    internal class ProjectFileProvider
    {
        private string projectFilePath;
        private ProjectSettings value;
        private readonly string projectFileName = "plcnext.proj";

        public ProjectFileProvider(string projectDirectory)
        {
            projectFilePath = Path.Combine(Path.GetDirectoryName(projectDirectory), projectFileName);
            ReadProjectFile();
        }

        private void ReadProjectFile()
        {
            try
            {
                using (Stream fileStream = File.OpenRead(projectFilePath))
                using (XmlReader reader = XmlReader.Create(fileStream))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(ProjectSettings));
                    value = (ProjectSettings)serializer.Deserialize(reader);
                }
            }
            catch (Exception)
            {
                value = new ProjectSettings();
            }
        }

        public void WriteProjectFile()
        {
            using (Stream fileStream = File.OpenWrite(projectFilePath))
            {
                fileStream.SetLength(0);
                XmlSerializer serializer = new XmlSerializer(typeof(ProjectSettings));
                serializer.Serialize(fileStream, value);
            }
        }

        public void SetGenerateNamespaces(bool generateNamespaces)
        {
            value.GenerateNamespaces = generateNamespaces;
            value.GenerateNamespacesSpecified = true;
        }

    }
}
