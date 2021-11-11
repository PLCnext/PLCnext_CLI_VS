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
using System.Threading;
using System.Windows.Forms;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using PlcncliServices.LocationService;
using IAsyncServiceProvider = Microsoft.VisualStudio.Shell.IAsyncServiceProvider;
using Task = System.Threading.Tasks.Task;

namespace PlcncliServices
{
    public class PlcncliLocationService
    {
        private readonly IAsyncServiceProvider _asyncServiceProvider;

        private PlcncliOptionPage optionPage = null;
        
        public PlcncliLocationService(IAsyncServiceProvider sp)
        {
            _asyncServiceProvider = sp;
        }

        public async Task InitializeAsync(CancellationToken cancellationToken)
        {
            if (Initialized)
                return;

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            if (_asyncServiceProvider is AsyncPackage package)
            {
                optionPage = package.GetDialogPage(typeof(PlcncliOptionPage)) as PlcncliOptionPage;

                OnOptionPagePropertyChanged(null, new PropertyChangedEventArgs(nameof(optionPage.ToolLocation)));
                optionPage.PropertyChanged += OnOptionPagePropertyChanged;
                Initialized = true;
            }
        }

        private void OnOptionPagePropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            if (!propertyChangedEventArgs.PropertyName.Equals(nameof(optionPage.ToolLocation)))
            { return; }
            string toolLocation = SearchPlcncliTool(true, false);
            if (!string.IsNullOrEmpty(toolLocation))
            {
                Environment.SetEnvironmentVariable("plcncli_toollocation", toolLocation);
            }
        }

        private bool Initialized { get; set; } = false;

        public string GetLocation()
        {
            return SearchPlcncliTool();
        }

        private string SearchPlcncliTool(bool isSecondTry = false, bool showMessages = true)
        {
            string toolLocation = ToolLocationFinder.SearchPlcncliTool(optionPage);
            if (!string.IsNullOrEmpty(toolLocation))
            {
                return toolLocation;
            }
            if (isSecondTry)
            {
                if (showMessages)
                {
                    _ = MessageBox.Show("PLCnCLI not found. PLCnext Technology Extension will not work properly. Set location in Tools->Options->PLCnext Technology.");
                }

                return string.Empty;
            }

            if (showMessages)
            {
                _ = MessageBox.Show("PLCnCLI not found. Please enter correct location in Tools->Options->PLCnext Technology");
            }

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
        }
    }
}