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

namespace PlcncliTemplateWizards.NewProjectItemDialog
{
    /// <summary>
    /// Interaction logic for NewItemDialogView.xaml
    /// </summary>
    public partial class NewItemDialogView : DialogWindow
    {
        public NewItemDialogView(Object dataContext)
        {
            InitializeComponent();
            this.DataContext = dataContext;
        }
    }
}
