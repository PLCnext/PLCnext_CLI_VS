#region Copyright
///////////////////////////////////////////////////////////////////////////////
//
//  Copyright (c) Phoenix Contact GmbH & Co KG
//  This software is licensed under Apache-2.0
//
///////////////////////////////////////////////////////////////////////////////
#endregion

using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Forms.Integration;

namespace PlcncliSdkOptionPage.ChangeSDKsProperty
{
    [Guid("98F73EC3-E018-48BF-BA20-22A3E6ED3FD3")]
    public class SDKsOptionPage : DialogPage
    {
        private SdkPage SdkPage { get; set; }
        private bool ReinitializationNeeded { get; set; }

        protected override IWin32Window Window
        {
            get
            {
                SdkPage = new SdkPage();
                // convert wpf usercontrol to windows.forms.usercontrol
                ElementHost host = new ElementHost
                {
                    Child = SdkPage.PageControl
                };
                return host;
            }
        }
        protected override void OnClosed(EventArgs e)
        {
            ReinitializationNeeded = true;
            base.OnClosed(e);
        }

        protected override void OnApply(PageApplyEventArgs e)
        {
            SdkPage.ApplyChanges();
            base.OnApply(e);
        }

        protected override void OnActivate(CancelEventArgs e)
        {
            if (ReinitializationNeeded)
            {
                SdkPage.ReinitializeControl();
                ReinitializationNeeded = false;
            }

            base.OnActivate(e);
        }
    }
}
