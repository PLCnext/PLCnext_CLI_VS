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
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true, Namespace="http://www.phoenixcontact.com/schema/cliproject")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace="http://www.phoenixcontact.com/schema/cliproject", IsNullable=false)]
    public partial class ProjectSettings {
        
        private string idField;
        
        private string versionField;
        
        private string nameField;
        
        private string typeField;
        
        private bool generateDTArrayNameByTypeField;
        
        private string cSharpProjectPathField;
        
        private bool generateNamespacesField;
        
        private bool generateNamespacesFieldSpecified;
        
        private string[] targetField;
        
        private extension[] extensionField;
        
        public ProjectSettings() {
            this.versionField = "1.0";
            this.typeField = "project";
            this.generateDTArrayNameByTypeField = false;
        }
        
        /// <remarks/>
        public string Id {
            get {
                return this.idField;
            }
            set {
                this.idField = value;
            }
        }
        
        /// <remarks/>
        [System.ComponentModel.DefaultValueAttribute("1.0")]
        public string Version {
            get {
                return this.versionField;
            }
            set {
                this.versionField = value;
            }
        }
        
        /// <remarks/>
        public string Name {
            get {
                return this.nameField;
            }
            set {
                this.nameField = value;
            }
        }
        
        /// <remarks/>
        [System.ComponentModel.DefaultValueAttribute("project")]
        public string Type {
            get {
                return this.typeField;
            }
            set {
                this.typeField = value;
            }
        }
        
        /// <remarks/>
        [System.ComponentModel.DefaultValueAttribute(false)]
        public bool GenerateDTArrayNameByType {
            get {
                return this.generateDTArrayNameByTypeField;
            }
            set {
                this.generateDTArrayNameByTypeField = value;
            }
        }
        
        /// <remarks/>
        public string CSharpProjectPath {
            get {
                return this.cSharpProjectPathField;
            }
            set {
                this.cSharpProjectPathField = value;
            }
        }
        
        /// <remarks/>
        public bool GenerateNamespaces {
            get {
                return this.generateNamespacesField;
            }
            set {
                this.generateNamespacesField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool GenerateNamespacesSpecified {
            get {
                return this.generateNamespacesFieldSpecified;
            }
            set {
                this.generateNamespacesFieldSpecified = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("Target")]
        public string[] Target {
            get {
                return this.targetField;
            }
            set {
                this.targetField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("Extension")]
        public extension[] Extension {
            get {
                return this.extensionField;
            }
            set {
                this.extensionField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.7.3081.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://www.phoenixcontact.com/schema/cliproject")]
    public partial class extension {
        
        private System.Xml.XmlElement[] anyField;
        
        private string nameField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAnyElementAttribute()]
        public System.Xml.XmlElement[] Any {
            get {
                return this.anyField;
            }
            set {
                this.anyField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string name {
            get {
                return this.nameField;
            }
            set {
                this.nameField = value;
            }
        }
    }
}
