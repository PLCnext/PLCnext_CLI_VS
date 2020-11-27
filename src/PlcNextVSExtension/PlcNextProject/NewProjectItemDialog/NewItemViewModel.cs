#region Copyright
///////////////////////////////////////////////////////////////////////////////
//
//  Copyright (c) Phoenix Contact GmbH & Co KG
//  This software is licensed under Apache-2.0
//
///////////////////////////////////////////////////////////////////////////////
#endregion

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.VisualStudio.PlatformUI;
using PlcNextVSExtension.Properties;

namespace PlcNextVSExtension.PlcNextProject.NewProjectItemDialog
{
    public class NewItemViewModel
    {
        private readonly NewItemModel _model;
        private string _selectedComponent;
        private readonly string descriptionProgram = "Please select a namespace and the parent component for the new program.";
        private readonly string descriptionComponent = "Please select a namespace for the new component.";

        public NewItemViewModel(NewItemModel model)
        {
            _model = model;
            Namespace = _model.SelectedNamespace;
            Components = new ObservableCollection<string>(_model.Components);
            SelectedComponent = Components.FirstOrDefault() ?? String.Empty;

            IsProgramWizard = _model.ItemType.Equals(Resources.ItemType_program);
            Description = IsProgramWizard ? descriptionProgram : descriptionComponent;
            
        }

        public string ItemType => _model.ItemType;
        public bool IsProgramWizard { get; }

        public string Description { get; }

        public string NamespaceLabel => "Namespace";

        public string Namespace { get; set; }

        public string ComponentLabel => "Parent Component";

        public string SelectedComponent
        {
            get => _selectedComponent;
            set { _selectedComponent = value; }
        }

        public string ButtonText => "OK";

        public ICommand OkButtonClickCommand => new DelegateCommand<Window>(OnOkButtonClicked);

        private void OnOkButtonClicked(Window window)
        {
            _model.SelectedComponent = SelectedComponent;
            _model.SelectedNamespace = Namespace;
            window.DialogResult = true;
            window.Close();
        }

        public ObservableCollection<string> Components { get; }
        
    }
}
