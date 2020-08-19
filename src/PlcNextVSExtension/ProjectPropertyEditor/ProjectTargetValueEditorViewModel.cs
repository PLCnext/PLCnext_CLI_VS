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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.VisualStudio.PlatformUI;
using PlcncliServices.CommandResults;
using PlcNextVSExtension.NewProjectInformationDialog;

namespace PlcNextVSExtension.ProjectPropertyEditor
{
    public class ProjectTargetValueEditorViewModel
    {
        private readonly ProjectTargetValueEditorModel model;

        public ProjectTargetValueEditorViewModel(ProjectTargetValueEditorModel model)
        {
            this.model = model;

            AvailableTargets = new ObservableCollection<TargetViewModel>(model.InstalledTargets
                .Where(t => !model.ProjectTargets.Select(pt => pt.GetDisplayName()).Contains(t.GetDisplayName()))
                .Select(t => new TargetViewModel(t.GetDisplayName(), t)));
            SelectedTargets = new ObservableCollection<TargetViewModel>(model.ProjectTargets.Select(t => new TargetViewModel(t.GetDisplayName(), t, t.Available)));
        }

        public ObservableCollection<TargetViewModel> AvailableTargets { get; }
        public ObservableCollection<TargetViewModel> SelectedTargets { get; }

        #region Commands

        public ICommand AddButtonClickCommand => new DelegateCommand<IList>(OnAddButtonClicked);
        public ICommand RemoveButtonClickCommand => new DelegateCommand<IList>(OnRemoveButtonClicked);
        public ICommand CloseButtonClickCommand => new DelegateCommand<Window>(OnCloseButtonClicked);
        
        public void OnAddButtonClicked(IList selectedItems)
        {
            IEnumerable<TargetViewModel> targets =
                AvailableTargets.Where(t => selectedItems.Cast<TargetViewModel>().Select(item => item.DisplayName).Contains(t.DisplayName)).ToList();
            foreach (TargetViewModel target in targets)
            {
                SelectedTargets.Add(target);
                AvailableTargets.Remove(target);
            }
        }

        public void OnRemoveButtonClicked(IList selectedItems)
        {
            IEnumerable<TargetViewModel> targets =
                SelectedTargets.Where(t => selectedItems.Cast<TargetViewModel>().Select(item => item.DisplayName).Contains(t.DisplayName)).ToList();
            foreach (TargetViewModel target in targets)
            {
                if (target.Available != false)
                {
                    AvailableTargets.Add(target);
                }
                SelectedTargets.Remove(target);
            }
        }
        private void OnCloseButtonClicked(Window window)
        {
            model.TargetsToRemove = model.ProjectTargets.Where(t =>
                !SelectedTargets.Select(st => st.DisplayName).Contains(t.GetDisplayName()));

            model.TargetsToAdd = SelectedTargets
                .Where(t => !model.ProjectTargets.Select(pt => pt.GetDisplayName()).Contains(t.DisplayName))
                .Select(t => t.Source);

            window.DialogResult = true;
            window.Close();
        }

        #endregion
    }
}
