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

namespace PlcncliTemplateWizards.NewProjectInformationDialog
{
    /// <summary>
    /// Interaction logic for NewProjectInformationView.xaml
    /// </summary>
    public partial class NewProjectInformationView : DialogWindow
    {
        public NewProjectInformationView(Object dataContext)
        {
            InitializeComponent();
            this.DataContext = dataContext;
        }
    }
}
