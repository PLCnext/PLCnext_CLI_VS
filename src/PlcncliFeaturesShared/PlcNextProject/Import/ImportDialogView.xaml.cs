#region Copyright
///////////////////////////////////////////////////////////////////////////////
//
//  Copyright (c) Phoenix Contact GmbH & Co KG
//  This software is licensed under Apache-2.0
//
///////////////////////////////////////////////////////////////////////////////
#endregion

using System;
using Microsoft.VisualStudio.PlatformUI;

namespace PlcncliFeatures.PlcNextProject.Import
{
    /// <summary>
    /// Interaction logic for NewItemDialogView.xaml
    /// </summary>
    public partial class ImportDialogView : DialogWindow
    {
        public ImportDialogView(Object dataContext)
        {
            InitializeComponent();
            this.DataContext = dataContext;
        }
    }
}
