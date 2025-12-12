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

        public static bool IsConfiguredToSignWithPKCS12(IProjectConfiguration config)
        {
            return config.Sign
                        && !string.IsNullOrEmpty(config.Pkcs12)
                        && string.IsNullOrEmpty(config.PrivateKey)
                        && string.IsNullOrEmpty(config.SigningCertificate)
                        && (config.CertificateChain == null || !config.CertificateChain.Any());
        }

        public static bool IsConfiguredToSignWithPEMFiles(IProjectConfiguration config)
        {
            return config.Sign
                        && string.IsNullOrEmpty(config.Pkcs12)
                        && !string.IsNullOrEmpty(config.PrivateKey)
                        && !string.IsNullOrEmpty(config.SigningCertificate);
        }

        public static IProjectConfiguration CreateNewConfiguration()
        {
            return new ConvertedProjectConfiguration();
        }

        public static ConvertedProjectConfiguration LoadFromConfig(string projectDirectory)
        {
            string configFilePath = GetConfigFilePath(projectDirectory);

            if (File.Exists(configFilePath))
            {
                using (FileStream stream = File.OpenRead(configFilePath))
                using (XmlReader reader = XmlReader.Create(stream))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(ProjectConfiguration));
                    ProjectConfiguration configuration = serializer.Deserialize(reader) as ProjectConfiguration;
                    return new ConvertedProjectConfiguration(configuration);
                }
            }
            return new ConvertedProjectConfiguration();
        }

        public static void WriteConfigFile(IProjectConfiguration config, string projectDirectory)
        {
            string configFilePath = GetConfigFilePath(projectDirectory);
            if (string.IsNullOrEmpty(config.LibraryDescription)
                && string.IsNullOrEmpty(config.LibraryVersion)
                && string.IsNullOrEmpty(config.EngineerVersion)
                && (config.LibraryInfo == null || config.LibraryInfo.Count() == 0)
                && (config.ExcludedFiles == null ||config.ExcludedFiles.Count() < 1)
                && !config.Sign
                && string.IsNullOrEmpty(config.Pkcs12)
                && string.IsNullOrEmpty(config.PrivateKey)
                && string.IsNullOrEmpty(config.SigningCertificate)
                && (config.CertificateChain == null || config.CertificateChain.Length == 0)
                && !config.Timestamp
                && string.IsNullOrEmpty(config.TimestampConfiguration))
            {
                if (File.Exists(configFilePath))
                {
                    DeleteFile();
                }
            }
            else
            {
                if (config.ExcludedFiles != null && !config.ExcludedFiles.Any())
                {
                    config.ExcludedFiles = null;
                }
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
                    serializer.Serialize(stream, config.GetSerializableConfiguration());
                }
            }
        }
    }
}
