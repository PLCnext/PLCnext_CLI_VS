#region Copyright
///////////////////////////////////////////////////////////////////////////////
//
//  Copyright (c) Phoenix Contact GmbH & Co KG
//  This software is licensed under Apache-2.0
//
///////////////////////////////////////////////////////////////////////////////
#endregion

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using Microsoft.VisualStudio.Shell;

namespace PlcncliServices.LocationService
{
    [Guid("E0865D49-D384-4D95-89D5-A04B1D51EC43")]
    public class PlcncliOptionPage : DialogPage, INotifyPropertyChanged
    {
        private string _toolLocation;
        private readonly string _toolLocationFilePath = "C:\\ProgramData\\PHOENIX CONTACT\\PLCnCLI\\PATHS.xml";
        private bool _askIncludesUpdate = true;
        private bool _updateIncludes = true;

        private void TryFindToolLocationFile()
        {
            try
            {
                XDocument pathsDocument = XDocument.Load(_toolLocationFilePath);
                ToolLocation = pathsDocument.Element("Product").Elements().First().Element("Path").Attribute("Value").Value;
            }
            catch (Exception)
            {
                //if something went wrong while trying to read tool location from file just use default value of _toolLocation
            }
        }

        [Category("PLCnCLI")]
        [DisplayName("PLCnCLI Folder")]
        [Description("Path to a folder containing the plcncli.exe. If this path is not a valid path to a PLCnCLI the 'PATH' environment variable will be used to find the PLCnCLI.")]
        public string ToolLocation
        {
            get
            {
                if (string.IsNullOrEmpty(_toolLocation))
                {
                    _toolLocation = "C:\\Program Files\\PHOENIX CONTACT\\PLCnCLI";
                    TryFindToolLocationFile();
                }
                return _toolLocation;
            }
            set
            {
                if (value == _toolLocation)
                    return;
                _toolLocation = value;
                OnPropertyChanged();
            }
        }

        [Category("PLCnCLI")]
        [DisplayName("Ask for includes-update every time CMakeLists.txt is saved")]
        [Description("If set to false, user will not be asked and option 'Update includes' will be used.")]
        public bool AskIncludesUpdate
        {
            get => _askIncludesUpdate;
            set
            {
                _askIncludesUpdate = value;
                OnPropertyChanged();
            }
        }
        [Category("PLCnCLI")]
        [DisplayName("Update includes")]
        [Description("This option is only used if not asked every time.")]
        public bool UpdateIncludes
        {
            get => _updateIncludes;
            set
            {
                _updateIncludes = value;
                OnPropertyChanged();
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
