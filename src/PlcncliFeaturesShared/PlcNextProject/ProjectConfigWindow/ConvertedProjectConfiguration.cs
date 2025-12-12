#region Copyright
///////////////////////////////////////////////////////////////////////////////
//
//  Copyright (c) Phoenix Contact GmbH & Co KG
//  This software is licensed under Apache-2.0
//
///////////////////////////////////////////////////////////////////////////////
#endregion

using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace PlcncliFeatures.PlcNextProject.ProjectConfigWindow
{
    

    public class ConvertedProjectConfiguration : IProjectConfiguration
    {
        private ProjectConfiguration Value { get; }
        public IProjectConfiguration GetSerializableConfiguration()
        {
            return ConvertConfigToNewestVersion(Value);
        }
        internal ConvertedProjectConfiguration(ProjectConfiguration value)
        {
            
            CheckPreconditions(value);
            Value = ConvertConfigToNewestVersion(value);
        }

        public ConvertedProjectConfiguration()
        {
            Value = new ProjectConfiguration();
            CheckPreconditions(Value);
        }

        private static void CheckPreconditions(ProjectConfiguration config)
        {
            if (config == null) return;

            if (!string.IsNullOrEmpty(config.SolutionVersion)
                && !string.IsNullOrEmpty(config.EngineerVersion))
            {
                throw new ConfigurationException(string.Format(CultureInfo.InvariantCulture, "{0} and {1} cannot be used together", 
                                                                nameof(config.SolutionVersion), nameof(config.EngineerVersion)));
            }

            if (!string.IsNullOrEmpty(config.PublicKey)
                && !string.IsNullOrEmpty(config.SigningCertificate))
            {
                throw new ConfigurationException(string.Format(CultureInfo.InvariantCulture, "{0} and {1} cannot be used together",
                                                                nameof(config.PublicKey), nameof(config.SigningCertificate)));
            }

            if (config.CertificateChain != null && config.CertificateChain.Any()
                && config.Certificates != null && config.Certificates.Any())
            {
                throw new ConfigurationException(string.Format(CultureInfo.InvariantCulture, "{0} and {1} cannot be used together", 
                                                                nameof(config.CertificateChain), nameof(config.Certificates)));
            }

        }

        private static ProjectConfiguration ConvertConfigToNewestVersion(ProjectConfiguration config)
        {
            if (config == null) { return null; }

            if (string.IsNullOrEmpty(config.SigningCertificate) && !string.IsNullOrEmpty(config.PublicKey))
            {
                config.SigningCertificate = config.PublicKey;
               
            }

            if (config.Certificates != null && config.Certificates.Any()
                && (config.CertificateChain == null || !config.CertificateChain.Any()))
            {
                config.CertificateChain = config.Certificates;
                
            }

            config.PublicKey = null;
            config.Certificates = null;

            return config;
        }



        public string EngineerVersion
        {
            get => Value.EngineerVersion;
            set
            {
                if (!string.IsNullOrEmpty(Value.SolutionVersion))
                {
                    throw new ConfigurationException(string.Format(CultureInfo.InvariantCulture, "{0} and {1} cannot be used together",
                                                                nameof(SolutionVersion), nameof(EngineerVersion)));
                }
                Value.EngineerVersion = value;
            }
        }

        public string SolutionVersion
        {
            get => Value.SolutionVersion;
            set
            {
                if (!string.IsNullOrEmpty(Value.EngineerVersion))
                {
                    throw new ConfigurationException(string.Format(CultureInfo.InvariantCulture, "{0} and {1} cannot be used together",
                                                                nameof(SolutionVersion), nameof(EngineerVersion)));
                }
                Value.SolutionVersion = value;
            }
        }

        public string LibraryVersion
        {
            get => Value.LibraryVersion;
            set { Value.LibraryVersion = value; }
        }

        public string LibraryDescription
        {
            get => Value.LibraryDescription;
            set { Value.LibraryDescription = value; }
        }

        public string[] ExcludedFiles
        {
            get => Value.ExcludedFiles;
            set => Value.ExcludedFiles = value;
        }

        public ProjectConfigurationLibraryInfo[] LibraryInfo
        {
            get => Value.LibraryInfo;
            set => Value.LibraryInfo = value;
        }

        #region SigningProperties

        public bool Sign
        {
            get => Value.Sign; 
            set => Value.Sign = value;
        }
        public string Pkcs12
        {
            get => Value.Pkcs12;
            set => Value.Pkcs12 = value;
        }
        public string PrivateKey
        {
            get => Value.PrivateKey;
            set => Value.PrivateKey = value;
        }

        public string PublicKey => null;

        public string SigningCertificate
        {
            get => Value.SigningCertificate ?? Value.PublicKey;
            set
            {
                if (Value.PublicKey != null)
                {
                    throw new ConfigurationException($"{nameof(Value.PublicKey)} and " +
                        $"{nameof(Value.SigningCertificate)} cannot both have a value.");
                }
                Value.SigningCertificate = value;
            }
        }

        public string[] Certificates => null; 

        public string[] CertificateChain
        {
            get => Value.CertificateChain ?? Value.Certificates ?? new string[] { };
            set
            {
                if (Value.Certificates != null && Value.Certificates.Any())
                {
                    throw new ConfigurationException($"{nameof(Value.Certificates)} and " +
                        $"{nameof(Value.CertificateChain)} cannot both have a value.");
                }
                Value.CertificateChain = value?.ToArray();
            }
        }

        public string TimestampConfiguration
        {
            get => Value.TimestampConfiguration;
            set => Value.TimestampConfiguration = value;
        }

        public bool Timestamp
        {
            get => Value.Timestamp;
            set => Value.Timestamp = value;
        }

        public bool NoTimestamp
        {
            get => Value.NoTimestamp;
            set => Value.NoTimestamp = value;
        }

        #endregion
    }
}
