#region Copyright
///////////////////////////////////////////////////////////////////////////////
//
//  Copyright (c) Phoenix Contact GmbH & Co KG
//  This software is licensed under Apache-2.0
//
///////////////////////////////////////////////////////////////////////////////
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.VisualStudio.PlatformUI;

namespace PlcNextVSExtension.PlcNextProject.NewProjectItemDialog
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
