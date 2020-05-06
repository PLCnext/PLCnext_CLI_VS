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
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using PlcncliServices.LocationService;
using IAsyncServiceProvider = Microsoft.VisualStudio.Shell.IAsyncServiceProvider;
using Task = System.Threading.Tasks.Task;

namespace PlcncliServices
{
    public class PlcncliLocationService
    {
        private readonly IAsyncServiceProvider _asyncServiceProvider;
        
        private PlcncliOptionPage optionPage = null;
        private readonly string plcncliFileName = "plcncli.exe";

        public PlcncliLocationService(IAsyncServiceProvider sp)
        {
            _asyncServiceProvider = sp;
        }

        public async Task InitializeAsync(CancellationToken cancellationToken)
        {
            await TaskScheduler.Default;

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            if (_asyncServiceProvider is Package package)
            {
                optionPage = package.GetDialogPage(typeof(PlcncliOptionPage)) as PlcncliOptionPage;

                OnOptionPagePropertyChanged(null, null);
                optionPage.PropertyChanged += OnOptionPagePropertyChanged;
            }
        }

        private void OnOptionPagePropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            Environment.SetEnvironmentVariable("plcncli_toollocation", GetLocation());
        }

        public string GetLocation()
        {
            return SearchPlcncliTool();
        }

        private string SearchPlcncliTool(bool secondTry = false, bool showMessages = true)
        {
            string toolLocation = string.Empty;

            if (CheckOption()) 
            {
                return toolLocation;
            }

            if (CheckPathVariable())
            {
                return toolLocation;
            }

            if (secondTry)
            {
                if(showMessages)
                    MessageBox.Show("PLCnCLI not found. PLCnext Technology Extension will not work properly. Set location in Tools->Options->PLCnext Technology.");
                return string.Empty;
            }

            if(showMessages)
                MessageBox.Show("PLCnCLI not found. Please enter correct location in Tools->Options->PLCnext Technology");
            if (_asyncServiceProvider is Package)
            {
                DTE2 dte = (DTE2)Package.GetGlobalService(typeof(DTE));
                if (dte != null)
                {
                    dte.ExecuteCommand("Tools.Options", "E0865D49-D384-4D95-89D5-A04B1D51EC43");
                    return SearchPlcncliTool(true);
                }
            }

            return string.Empty;

            bool CheckOption()
            {
                string location = optionPage.ToolLocation;
                if (!string.IsNullOrEmpty(location) && File.Exists(Path.Combine(location, plcncliFileName)))
                {
                    toolLocation = Path.Combine(location, plcncliFileName);
                    return true;
                }
                return false;
            }

            bool CheckPathVariable()
            {
                string pathVariable = Environment.GetEnvironmentVariable("PATH");
                if (pathVariable != null)
                {
                    string[] pathParts = pathVariable.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var path in pathParts)
                    {
                        DirectoryInfo fileInfo = new DirectoryInfo(path);
                        if (fileInfo.Exists)
                        {
                            var files = fileInfo.GetFiles();
                            foreach (FileInfo file in files)
                            {
                                if (file.Name.Equals(plcncliFileName))
                                {
                                    toolLocation = file.FullName;
                                    return true;
                                }
                            }
                        }
                    }
                }
                return false;
            }
        }
    }
}
