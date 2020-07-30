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

namespace PlcncliServices.GeneratePortComment
{
    public class GeneratePortCommentViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly string portComment = "//#port";
        private readonly string attributesComment = "//#attributes({0})";
        private readonly string nameComment = "//#name({0})";
        private string preview;
        private string name;

        public GeneratePortCommentViewModel(string line)
        {
            Line = line;
            PortAttributes = new Collection<PortAttributeViewModel>
            {
                new PortAttributeViewModel("Input"),
                new PortAttributeViewModel("Output"),
                new PortAttributeViewModel("ReadOnly"),
                new PortAttributeViewModel("Retain"),
                new PortAttributeViewModel("Opc"),
                new PortAttributeViewModel("Ehmi"),
                new PortAttributeViewModel("ProfiCloud"),
                new PortAttributeViewModel("Archive"),
            };
            foreach (PortAttributeViewModel attributeVM in PortAttributes)
            {
                attributeVM.PropertyChanged += UpdatePreview;
            }
            UpdatePreview();
        }

        private void UpdatePreview(object sender = null, PropertyChangedEventArgs e = null)
        {
            string leadingWhitespaces = string.Concat(Line.TakeWhile(c => char.IsWhiteSpace(c)));
            string part1 = "\n" + leadingWhitespaces + portComment;
            string part2 = PortAttributes.Where(p => p.Selected).Any() 
                            ? "\n" + leadingWhitespaces + string.Format(attributesComment, string.Join("|",PortAttributes.Where(p => p.Selected).Select(p => p.Label))) 
                            : string.Empty;
            string part3 = string.IsNullOrWhiteSpace(Name)
                            ? string.Empty
                            : "\n" + leadingWhitespaces + string.Format(nameComment, Name);

            Preview = part1  + part2 + part3; 
        }

        public IEnumerable<PortAttributeViewModel> PortAttributes { get; }

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

        public ICommand OkCommand { get; } = new DelegateCommand<DialogWindow>(OnOkButtonClicked);

        private static void OnOkButtonClicked(DialogWindow window)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
            window.DialogResult = true;
            window.Close();
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
        public void Dispose()
        {
            foreach (PortAttributeViewModel attributeVM in PortAttributes)
            {
                attributeVM.PropertyChanged -= UpdatePreview;
            }
        }
    }

    public class PortAttributeViewModel : INotifyPropertyChanged
    {
        private bool selected = false;

        public PortAttributeViewModel(string label)
        {
            Label = label;
        }
        public string Label { get; }
        public bool Selected
        {
            get => selected;
            set
            {
                selected = value;
                OnPropertyChanged();
            }
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
