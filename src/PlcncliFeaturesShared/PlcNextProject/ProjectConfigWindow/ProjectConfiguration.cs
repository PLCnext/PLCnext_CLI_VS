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
using System.Text;

namespace PlcncliFeatures.PlcNextProject
{
    public interface IProjectConfiguration
    {
        string EngineerVersion { get; set; }
        string SolutionVersion { get; set; }
        string LibraryVersion { get; set; }
        string LibraryDescription { get; set; }
        string[] ExcludedFiles { get; set; }
        ProjectConfigurationLibraryInfo[] LibraryInfo { get; set; }
        bool Sign { get; set; }
        string Pkcs12 { get; set; }
        string PrivateKey { get; set; }
        string SigningCertificate { get; set; }
        string[] CertificateChain { get; set; }
        string TimestampConfiguration { get; set; }
        bool Timestamp { get; set; }
        bool NoTimestamp { get; set; }
        IProjectConfiguration GetSerializableConfiguration();
    }

    public partial class ProjectConfiguration : IProjectConfiguration
    {
        public IProjectConfiguration GetSerializableConfiguration()
        {
            return null;
        }
    }
}
