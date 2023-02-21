#region Copyright
///////////////////////////////////////////////////////////////////////////////
//
//  Copyright (c) Phoenix Contact GmbH & Co KG
//  This software is licensed under Apache-2.0
//
///////////////////////////////////////////////////////////////////////////////
#endregion

using Microsoft.VisualStudio.PlatformUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace PlcncliFeatures.GeneratePortComment
{
    public class GeneratePortCommentViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly string portComment = "//#port";
        private readonly string attributesComment = "//#attributes({0})";
        private readonly string nameComment = "//#name({0})";
        private readonly string iecdatatypeComment = "//#iecdatatype({0})";
        private string preview;
        private string name;

        public GeneratePortCommentViewModel(string line)
        {
            Line = line;
            PortAttributes = new Collection<SelectableLabelViewModel>
            {
                new SelectableLabelViewModel("Input", "The variable is defined as IN port."),
                new SelectableLabelViewModel("Output", "The variable is defined as OUT port."),
                new SelectableLabelViewModel("Retain", "The variable value is retained in case of a warm and hot restart (only initialized in case of a cold restart)."),
                new SelectableLabelViewModel("Opc", "The variable is visible for OPC UA."),
                new SelectableLabelViewModel("Ehmi", "The variable is visible for the PLCnext Engineer  HMI.( Note: This attribute is currently not implemented. Implementation is planned.)"),
                new SelectableLabelViewModel("ProfiCloud", "The variable is visible for Proficloud (for OUT ports only)."),
                new SelectableLabelViewModel("Redundant", "This attribute is relevant only for PLCnext Technology controllers with redundancy function.This variable is synchronized from PRIMARY controller to BACKUP controller. From FW 2022.0 LTS")
            };
            foreach (SelectableLabelViewModel attributeVM in PortAttributes)
            {
                attributeVM.PropertyChanged += UpdatePreview;
            }
            IECTypeAttributes = new Collection<SelectableLabelViewModel>()
            {
                new SelectableLabelViewModel("BYTE", "Use only for ports of type uint8"),
                new SelectableLabelViewModel("WORD", "Use only for ports of type uint16"),
                new SelectableLabelViewModel("DWORD", "Use only for ports of type uint32"),
                new SelectableLabelViewModel("LWORD", "Use only for ports of type uint64"),
            };
            foreach (SelectableLabelViewModel item in IECTypeAttributes)
            {
                item.PropertyChanged += UpdateIECAttribute;
            }
            UpdatePreview();
        }

        private void UpdateIECAttribute(object sender = null, PropertyChangedEventArgs e = null)
        {
            SelectableLabelViewModel element = sender as SelectableLabelViewModel;
            if (element.Selected && e.PropertyName == nameof(element.Selected))
            {
                foreach (SelectableLabelViewModel item in IECTypeAttributes.Where(x => x != element))
                {
                    item.Selected = false;
                }
                UpdatePreview();
            }
            if (!element.Selected && !IECTypeAttributes.Where(x => x.Selected).Any())
            {
                UpdatePreview();
            }
        }

        private void UpdatePreview(object sender = null, PropertyChangedEventArgs e = null)
        {
            string leadingWhitespaces = string.Concat(Line.TakeWhile(c => char.IsWhiteSpace(c)));
            string part1 = Environment.NewLine + leadingWhitespaces + portComment;
            string part3 = PortAttributes.Where(p => p.Selected).Any() 
                            ? Environment.NewLine + leadingWhitespaces + string.Format(attributesComment, string.Join("|", PortAttributes.Where(p => p.Selected).Select(p => p.Label)))
                            : string.Empty;
            string part2 = string.IsNullOrWhiteSpace(Name)
                            ? string.Empty
                            : Environment.NewLine + leadingWhitespaces + string.Format(nameComment, Name);
            string part4 = IECTypeAttributes.Where(x => x.Selected).Any()
                            ? Environment.NewLine + leadingWhitespaces + string.Format(iecdatatypeComment, IECTypeAttributes.Where(p => p.Selected).Select(p => p.Label).First())
                            : string.Empty;
            Preview = part1 + part2 + part3 + part4;
        }

        public IEnumerable<SelectableLabelViewModel> PortAttributes { get; }
        public IEnumerable<SelectableLabelViewModel> IECTypeAttributes { get; }

        public string Preview
        {
            get => preview;
            private set
            {
                preview = value;
                OnPropertyChanged();
            }
        }

        public string Name
        {
            get => name; set
            {
                if (!value.Equals(name))
                {
                    name = value;
                    UpdatePreview();
                }
            }
        }

        public string Line { get; }

        #region Commands

        public ICommand OkCommand { get; } = new DelegateCommand<DialogWindow>(OnOkButtonClicked);
        public ICommand CancelCommand { get; } = new DelegateCommand<DialogWindow>(OnCancelButtonClicked);
        public ICommand ClearSelectionCommand => new DelegateCommand(OnClearSelectionButtonClicked);

        private static void OnOkButtonClicked(DialogWindow window)
        {
            window.DialogResult = true;
            window.Close();
        }

        private static void OnCancelButtonClicked(DialogWindow window)
        {
            window.DialogResult = false;
            window.Close();
        }

        private void OnClearSelectionButtonClicked()
        {
            SelectableLabelViewModel selectedElement = IECTypeAttributes.Where(x => x.Selected).FirstOrDefault();
            if (selectedElement != null)
            {
                selectedElement.Selected = false;
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
        public void Dispose()
        {
            foreach (SelectableLabelViewModel attributeVM in PortAttributes)
            {
                attributeVM.PropertyChanged -= UpdatePreview;
            }
            foreach (SelectableLabelViewModel item in IECTypeAttributes)
            {
                item.PropertyChanged -= UpdateIECAttribute;
            }
        }
    }
}
