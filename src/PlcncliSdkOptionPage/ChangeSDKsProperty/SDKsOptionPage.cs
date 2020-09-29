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
using System.Runtime.InteropServices;
using System.Windows;

namespace PlcncliSdkOptionPage.ChangeSDKsProperty
{
    [DesignerCategory("")]
    [Guid("98F73EC3-E018-48BF-BA20-22A3E6ED3FD3")]
    public class SDKsOptionPage : UIElementDialogPage
    {
        private SdkPage SdkPage { get; set; }
        private bool ReinitializationNeeded { get; set; }

        protected override UIElement Child
        {
            get
            {
                SdkPage = new SdkPage();
                return SdkPage.PageControl;
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
