#region Copyright
///////////////////////////////////////////////////////////////////////////////
//
//  Copyright (c) Phoenix Contact GmbH & Co KG
//  This software is licensed under Apache-2.0
//
///////////////////////////////////////////////////////////////////////////////
#endregion

using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using Microsoft.VisualStudio.PlatformUI;
using PlcncliCommonUtils;

namespace PlcncliTemplateWizards.NewProjectItemDialog
{
    public class NewItemViewModel : INotifyPropertyChanged, IDataErrorInfo
    {
        private readonly NewItemModel _model;
        private string _selectedComponent;
        private string name;
        private string @namespace;
        private readonly string descriptionProgram = "Program Properties";
        private readonly string descriptionComponent = "Component Properties";
        private readonly string ComponentErrorText = "Component name does not match pattern ^[A-Z](?!.*__)[a-zA-Z0-9_]*$";
        private readonly string ProgramErrorText = "Program name does not match pattern ^[A-Z](?!.*__)[a-zA-Z0-9_]*$";
        private readonly string NamespaceErrorText = "Namespace does not match pattern ^(?:[a-zA-Z][a-zA-Z0-9_]*\\.)*[a-zA-Z](?!.*__)[a-zA-Z0-9_]*$";
        private readonly string ExistsErrorText = "An entity with this name exists already";
        private readonly string LengthErrorText = "Name must have a length between 2 and 128 characters.";
        private readonly string NameAndNamespaceEqualErrorText = "Name and namespace should not be the same.";

        private static readonly Regex ComponentNameRegex = new Regex(@"^[A-Z](?!.*__)[a-zA-Z0-9_]*$", RegexOptions.Compiled);
        private static readonly Regex ProgramNameRegex = new Regex(@"^[A-Z](?!.*__)[a-zA-Z0-9_]*$", RegexOptions.Compiled);
        private static readonly Regex ProjectNamespaceRegex = new Regex(@"^(?:[a-zA-Z][a-zA-Z0-9_]*\.)*[a-zA-Z](?!.*__)[a-zA-Z0-9_]*$", RegexOptions.Compiled);

        public NewItemViewModel(NewItemModel model)
        {
            _model = model;
            IsProgramWizard = _model.ItemType.Equals(Constants.ItemType_program);
            Namespace = _model.SelectedNamespace;
            Name = _model.SelectedName;
            Components = new ObservableCollection<string>(_model.Components.Select(e => $"{e.Namespace}::{e.Name}"));
            SelectedComponent = Components.FirstOrDefault() ?? String.Empty;

            Description = IsProgramWizard ? descriptionProgram : descriptionComponent;            
        }

        public string ItemType => _model.ItemType;
        public bool IsProgramWizard { get; }

        public string Description { get; }

        public string NameLabel => "Name";

        public string Name
        {
            get => name;
            set
            { 
                name = value; 
                OnPropertyChanged(nameof(Namespace)); //to force validation on both fields when changing the name
            }
        }

        public string NamespaceLabel => "Namespace";

        public string Namespace
        {
            get => @namespace;
            set
            {
                @namespace = value;
                OnPropertyChanged(nameof(Name)); //to force validation on both fields when changing the namespace
            }
        }

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
            _model.SelectedName = Name;
            _model.SelectedComponent = SelectedComponent;
            _model.SelectedNamespace = Namespace;
            window.DialogResult = true;
            window.Close();
        }

        public ObservableCollection<string> Components { get; }

        #region IDataErrorInfo
        public string Error => IsProgramWizard ? ProgramErrorText : ComponentErrorText;

        public string this[string columnName]
        {
            get
            {
                if (columnName == nameof(Name))
                {
                    
                    if (!(IsProgramWizard ? ProgramNameRegex : ComponentNameRegex).IsMatch(name))
                    {
                        return IsProgramWizard ? ProgramErrorText : ComponentErrorText;
                    }
                    if(_model.Components.Concat(_model.Programs).Where(entity => entity.Name.Equals(name, StringComparison.OrdinalIgnoreCase)).Any())
                    {
                        return ExistsErrorText;
                    }
                    if (!string.IsNullOrEmpty(name) && (name.Length < 2 || name.Length > 128))
                    {
                        return LengthErrorText;
                    }
                    if (name.Equals(Namespace, StringComparison.OrdinalIgnoreCase))
                    {
                        return NameAndNamespaceEqualErrorText;
                    }

                }
                if(columnName == nameof(Namespace)) 
                {
                    if(!ProjectNamespaceRegex.IsMatch(Namespace))
                    {
                        return NamespaceErrorText;
                    }
                    if (name.Equals(Namespace, StringComparison.OrdinalIgnoreCase))
                    {
                        return NameAndNamespaceEqualErrorText;
                    }
                }
                return string.Empty;
            }
        }
        #endregion

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
