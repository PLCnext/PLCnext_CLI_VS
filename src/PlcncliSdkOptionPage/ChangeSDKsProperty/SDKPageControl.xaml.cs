#region Copyright
///////////////////////////////////////////////////////////////////////////////
//
//  Copyright (c) Phoenix Contact GmbH & Co KG
//  This software is licensed under Apache-2.0
//
///////////////////////////////////////////////////////////////////////////////
#endregion

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Windows.Controls;

namespace PlcncliSdkOptionPage.ChangeSDKsProperty
{
    /// <summary>
    /// Interaction logic for SDKPageControl.xaml
    /// </summary>
    public partial class SDKPageControl : UserControl
    {
        public SDKPageControl(SDKPageViewModel viewModel)
        {
            this.DataContext = viewModel;
            try
            {
                InitializeComponent();
            }
            catch (Exception e)
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                IVsActivityLog log = Package.GetGlobalService(typeof(SVsActivityLog)) as IVsActivityLog;
                if (log == null)
                    return;
                log.LogEntry((uint)__ACTIVITYLOG_ENTRYTYPE.ALE_ERROR, this.ToString(),
                    "Component initialization failed: " + e.Message);
            }
        }
    }
}
