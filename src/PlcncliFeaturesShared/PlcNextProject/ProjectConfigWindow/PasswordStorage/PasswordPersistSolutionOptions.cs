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
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;

namespace PlcncliFeatures.PlcNextProject.ProjectConfigWindow
{
    internal struct PasswordPersistModel
    {
        private Dictionary<string, PasswordPersistModelEntry> entries;

        public void AddEntry(string project, PasswordPersistFileType fileType, string password)
        {
            if (this.entries == null) 
            {
                this.entries = new Dictionary<string, PasswordPersistModelEntry>();
            }

            if (!this.entries.TryGetValue(project ?? string.Empty, out PasswordPersistModelEntry entry))
            {
                entry = new PasswordPersistModelEntry();
            }


            if (fileType == PasswordPersistFileType.PKCS12)
            {
                entry.PasswordForPKCS12 = password;
            }
            else
            {
                entry.PasswordForPEMKeyFile = password;
            }
            this.entries[project ?? string.Empty] = entry;
        }

        public bool GetEntry(string projectName, PasswordPersistFileType fileType, out string password)
        {
            password = string.Empty;
            if (this.entries != null)
            {
                if (this.entries.TryGetValue(projectName, out PasswordPersistModelEntry entry))
                {
                    if (fileType == PasswordPersistFileType.PKCS12 && !string.IsNullOrEmpty(entry.PasswordForPKCS12))
                    {
                        password = entry.PasswordForPKCS12 ?? string.Empty;
                        return true;
                    }
                    if (fileType == PasswordPersistFileType.PEMKeyFile && !string.IsNullOrEmpty(entry.PasswordForPEMKeyFile))
                    {
                        password = entry.PasswordForPEMKeyFile ?? string.Empty;
                        return true;
                    }
                }
            }
            return false;
        }

        public IReadOnlyDictionary<string, PasswordPersistModelEntry> Entries => this.entries;
    }
    internal struct PasswordPersistModelEntry
    {
        public string PasswordForPKCS12 { get; set; }

        public string PasswordForPEMKeyFile { get; set; }
    }

    internal enum PasswordPersistFileType
    {
        PKCS12, PEMKeyFile
    }

    internal class PasswordPersistSolutionOptions : PersistSolutionOptions<PasswordPersistModel>
    {
        private const string RootElementName = "PrivateKeyPassword";
        private const string EntryElementName = "Entry";
        private const string ProjectElementName = "Project";
        private const string Passwordpkcs12ElementName = "PasswordPKCS12";
        private const string PasswordprivateKeyElementName = "PasswordPrivateKey";

        public PasswordPersistSolutionOptions()
        {
        }

        public PasswordPersistSolutionOptions(PasswordPersistModel model)
            : base(model)
        {
        }

        protected override PasswordPersistModel ReadData(Stream stream)
        {
            XDocument document = XDocument.Load(stream);

            XElement rootElement = document.Element(RootElementName);

            PasswordPersistModel model = new PasswordPersistModel();

            XElement[] entries = rootElement?.Elements(EntryElementName).ToArray() ?? Array.Empty<XElement>();
            foreach (XElement entry in entries)
            {
                string projectName = entry.Element(ProjectElementName)?.Value ?? string.Empty;
                string encryptedPKCS12Password = entry.Element(Passwordpkcs12ElementName)?.Value ?? string.Empty;
                string encryptedPEMKeyFilePassword = entry.Element(PasswordprivateKeyElementName)?.Value ?? string.Empty;
                
                if (UnprotectPassword(encryptedPKCS12Password, out string passwordPKCS12))
                {
                    model.AddEntry(projectName, PasswordPersistFileType.PKCS12, passwordPKCS12);
                }
                if (UnprotectPassword(encryptedPEMKeyFilePassword, out string passwordPEMKeyFile))
                {
                    model.AddEntry(projectName, PasswordPersistFileType.PEMKeyFile, passwordPEMKeyFile);
                }
            }
            return model;
        }

        protected override void WriteData(Stream stream, PasswordPersistModel data)
        {
            List<XElement> entries = new List<XElement>();
            if (data.Entries != null)
            {
                foreach (KeyValuePair<string, PasswordPersistModelEntry> entry in data.Entries.OrderBy(x => x.Key))
                {
                    if (!ProtectPassword(entry.Value.PasswordForPKCS12 ?? string.Empty, out string encryptedPKCS12Password))
                    {
                        //Skip complete entry if password cannot be protected.
                        continue;
                    }
                    if (!ProtectPassword(entry.Value.PasswordForPEMKeyFile ?? string.Empty, out string encryptedPEMKeyFilePassword))
                    {
                        //Skip complete entry if password cannot be protected.
                        continue;
                    }

                    entries.Add(new XElement(EntryElementName,
                                    new XElement(ProjectElementName, entry.Key ?? string.Empty),
                                    new XElement(Passwordpkcs12ElementName, encryptedPKCS12Password),
                                    new XElement(PasswordprivateKeyElementName, encryptedPEMKeyFilePassword)));
                }
            }

            XDocument document = new XDocument(
                new XElement(RootElementName, entries.ToArray()));

            document.Save(stream);
        }

        private static bool ProtectPassword(string password, out string encryptedPassword)
        {
            try
            {
                byte[] protectedBytes = ProtectedData.Protect(
                    Encoding.UTF8.GetBytes(password),
                    null,
                    DataProtectionScope.CurrentUser);

                encryptedPassword = Convert.ToBase64String(protectedBytes);
                return true;
            }
            catch (Exception) { }

            encryptedPassword = string.Empty;
            return false;
        }

        private static bool UnprotectPassword(string encryptedPassword, out string password)
        {
            try
            {
                byte[] unprotectedBytes = ProtectedData.Unprotect(
                    Convert.FromBase64String(encryptedPassword),
                    null,
                    DataProtectionScope.CurrentUser);

                password = Encoding.UTF8.GetString(unprotectedBytes);
                return true;
            }
            catch (Exception) { }

            password = string.Empty;
            return false;
        }
    }
}
