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
using System.Linq;
using System.Windows;
using System.Xml;
using System.Xml.Serialization;

namespace PlcncliFeatures.PlcNextProject.ProjectConfigWindow
{
    internal class ConfigFileProvider
    {
        private static readonly string configFileName = "PLCnextSettings.xml";

        private static string GetConfigFilePath(string projectDirectory)
        {
            return Path.Combine(Path.GetDirectoryName(projectDirectory), configFileName);
        }

        public static bool IsConfiguredToSignWithPKCS12(ProjectConfiguration config)
        {
            return config.Sign
                        && !string.IsNullOrEmpty(config.Pkcs12)
                        && string.IsNullOrEmpty(config.PrivateKey)
                        && string.IsNullOrEmpty(config.PublicKey)
                        && (config.Certificates == null || !config.Certificates.Any());
        }

        public static bool IsConfiguredToSignWithPEMFiles(ProjectConfiguration config)
        {
            return config.Sign
                        && string.IsNullOrEmpty(config.Pkcs12)
                        && !string.IsNullOrEmpty(config.PrivateKey)
                        && !string.IsNullOrEmpty(config.PublicKey)
                        && config.Certificates != null 
                        && config.Certificates.Any();
        }

        public static ProjectConfiguration LoadFromConfig(string projectDirectory)
        {
            string configFilePath = GetConfigFilePath(projectDirectory);
            ProjectConfiguration configuration = new ProjectConfiguration();

            if (File.Exists(configFilePath))
            {
                try
                {
                    using (FileStream stream = File.OpenRead(configFilePath))
                    using(XmlReader reader = XmlReader.Create(stream)) 
                    {
                        XmlSerializer serializer = new XmlSerializer(typeof(ProjectConfiguration));
                        configuration = serializer.Deserialize(reader) as ProjectConfiguration;
                    }
                }
                catch (Exception e)
                {
                    _ = MessageBox.Show("Project configuration file could not be loaded." + e.Message);
                }
            }

            return configuration;
        }

        public static void WriteConfigFile(ProjectConfiguration config, string projectDirectory)
        {
            string configFilePath = GetConfigFilePath(projectDirectory);
            if (string.IsNullOrEmpty(config.LibraryDescription)
                && string.IsNullOrEmpty(config.LibraryVersion)
                && string.IsNullOrEmpty(config.EngineerVersion)
                && (config.ExcludedFiles == null ||config.ExcludedFiles.Length < 1)
                && !config.Sign
                && string.IsNullOrEmpty(config.Pkcs12)
                && string.IsNullOrEmpty(config.PrivateKey)
                && string.IsNullOrEmpty(config.PublicKey)
                && (config.Certificates == null || config.Certificates.Length == 0)
                && !config.Timestamp
                // TODO how to handle notimestamp
                && string.IsNullOrEmpty(config.TimestampConfiguration))
            {
                if (File.Exists(configFilePath))
                {
                    DeleteFile();
                }
            }
            else
            {
                WriteFile();
            }


            void DeleteFile()
            {
                File.Delete(configFilePath);
            }

            void WriteFile()
            {
                using (FileStream stream = File.OpenWrite(configFilePath))
                {
                    stream.SetLength(0);
                    XmlSerializer serializer = new XmlSerializer(typeof(ProjectConfiguration));
                    serializer.Serialize(stream, config);
                }
            }
        }
    }
}
