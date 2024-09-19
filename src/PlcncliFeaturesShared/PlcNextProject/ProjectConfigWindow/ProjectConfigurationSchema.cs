﻿#region Copyright
///////////////////////////////////////////////////////////////////////////////
//
//  Copyright (c) Phoenix Contact GmbH & Co KG
//  This software is licensed under Apache-2.0
//
///////////////////////////////////////////////////////////////////////////////
#endregion

//------------------------------------------------------------------------------
// <auto-generated>
//     Dieser Code wurde von einem Tool generiert.
//     Laufzeitversion:4.0.30319.42000
//
//     Änderungen an dieser Datei können falsches Verhalten verursachen und gehen verloren, wenn
//     der Code erneut generiert wird.
// </auto-generated>
//------------------------------------------------------------------------------

// 
// This source code was auto-generated by xsd, Version=4.7.3081.0.
// 
namespace PlcncliFeatures.PlcNextProject {
    using System.Xml.Serialization;
    
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.7.3081.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true, Namespace="http://www.phoenixcontact.com/schema/projectconfiguration")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace="http://www.phoenixcontact.com/schema/projectconfiguration", IsNullable=false)]
    public partial class ProjectConfiguration {
        
        private string engineerVersionField;
        
        private string solutionVersionField;
        
        private string libraryVersionField;
        
        private string libraryDescriptionField;
        
        private string[] excludedFilesField;
        
        private bool signField;
        
        private string pkcs12Field;
        
        private string privateKeyField;
        
        private string publicKeyField;
        
        private string[] certificatesField;
        
        private string timestampConfigurationField;
        
        private bool timestampField;
        
        private bool noTimestampField;
        
        public ProjectConfiguration() {
            this.signField = false;
            this.timestampField = false;
            this.noTimestampField = false;
        }
        
        /// <remarks/>
        public string EngineerVersion {
            get {
                return this.engineerVersionField;
            }
            set {
                this.engineerVersionField = value;
            }
        }
        
        /// <remarks/>
        public string SolutionVersion {
            get {
                return this.solutionVersionField;
            }
            set {
                this.solutionVersionField = value;
            }
        }
        
        /// <remarks/>
        public string LibraryVersion {
            get {
                return this.libraryVersionField;
            }
            set {
                this.libraryVersionField = value;
            }
        }
        
        /// <remarks/>
        public string LibraryDescription {
            get {
                return this.libraryDescriptionField;
            }
            set {
                this.libraryDescriptionField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("File", IsNullable=false)]
        public string[] ExcludedFiles {
            get {
                return this.excludedFilesField;
            }
            set {
                this.excludedFilesField = value;
            }
        }
        
        /// <remarks/>
        [System.ComponentModel.DefaultValueAttribute(false)]
        public bool Sign {
            get {
                return this.signField;
            }
            set {
                this.signField = value;
            }
        }
        
        /// <remarks/>
        public string Pkcs12 {
            get {
                return this.pkcs12Field;
            }
            set {
                this.pkcs12Field = value;
            }
        }
        
        /// <remarks/>
        public string PrivateKey {
            get {
                return this.privateKeyField;
            }
            set {
                this.privateKeyField = value;
            }
        }
        
        /// <remarks/>
        public string PublicKey {
            get {
                return this.publicKeyField;
            }
            set {
                this.publicKeyField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("File", IsNullable=false)]
        public string[] Certificates {
            get {
                return this.certificatesField;
            }
            set {
                this.certificatesField = value;
            }
        }
        
        /// <remarks/>
        public string TimestampConfiguration {
            get {
                return this.timestampConfigurationField;
            }
            set {
                this.timestampConfigurationField = value;
            }
        }
        
        /// <remarks/>
        [System.ComponentModel.DefaultValueAttribute(false)]
        public bool Timestamp {
            get {
                return this.timestampField;
            }
            set {
                this.timestampField = value;
            }
        }
        
        /// <remarks/>
        [System.ComponentModel.DefaultValueAttribute(false)]
        public bool NoTimestamp {
            get {
                return this.noTimestampField;
            }
            set {
                this.noTimestampField = value;
            }
        }
    }
}