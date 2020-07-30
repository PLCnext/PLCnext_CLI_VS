#region Copyright
///////////////////////////////////////////////////////////////////////////////
//
//  Copyright (c) Phoenix Contact GmbH & Co KG
//  This software is licensed under Apache-2.0
//
///////////////////////////////////////////////////////////////////////////////
#endregion

using Microsoft.Build.Tasks;
using Microsoft.VisualStudio.PlatformUI;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace PlcncliSdkOptionPage.ChangeSDKsProperty
{
    public class SDKPageViewModel : INotifyPropertyChanged
    {
        private bool sdkIsSelected;

        private readonly SDKPageModel model;
        

        public SDKPageViewModel(SDKPageModel model)
        {
            this.model = model;
            Initialize();
        }

        public void Initialize()
        {
            SdkList = new ObservableCollection<SdkViewModel>(model.Sdks);
            OnPropertyChanged("SdkList");
            foreach (SdkViewModel sdk in SdkList)
            {
                sdk.ParentViewModel = this;
            }
        }
        public ObservableCollection<SdkViewModel> SdkList { get; private set; }

        public bool SdkIsSelected 
        { 
            get => sdkIsSelected;
            private set
            {
                if (value != sdkIsSelected)
                {
                    sdkIsSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        public void CheckSelectedState()
        {
            foreach (SdkViewModel childModel in SdkList)
            {
                if (childModel.IsSelected)
                {
                    SdkIsSelected = true;
                    return;
                }
            }
            SdkIsSelected = false;
        }

        
        #region Commands

        public ICommand AddCommand => new DelegateCommand(OnAddButtonClicked);


        private void OnAddButtonClicked()
        {
            AddSdkViewModel viewModel = new AddSdkViewModel();
            AddSdkDialog addDialog = new AddSdkDialog(viewModel);
            addDialog.ShowModal();
            if(addDialog.DialogResult == true)
            {
                if (!string.IsNullOrEmpty(viewModel.SdkRootPath))
                {
                    //an sdk is added to the list 

                    //check if path already in list
                    SdkViewModel existingSdk = SdkList.SingleOrDefault(sdk => sdk.Path.Equals(viewModel.SdkRootPath));
                    if (existingSdk != null)
                    {
                        switch (existingSdk.SdkState)
                        {
                            case SdkState.unchanged:
                                //do nothing because sdk is already in list of sdks
                                MessageBox.Show($"The current list of sdk locations already contains the entry\n'{viewModel.SdkRootPath}'");
                                break;
                            case SdkState.removed:
                                model.SdkChangesCollector.AddSdk(viewModel.SdkRootPath);
                                existingSdk.SdkState = SdkState.unchanged;
                                break;
                            case SdkState.added:
                                //do nothing because sdk is already in list of sdks
                                MessageBox.Show($"The location\n'{viewModel.SdkRootPath}'\nis already in the list of sdks to be added.\nChanges will be applied, when the 'Options' dialog is closed.");
                                break;
                            case SdkState.installed:
                                //user wants to install an sdk in same location where he/she wants to add an sdk into
                                MessageBox.Show($"The location\n'{viewModel.SdkRootPath}'\nis already used as destination for an sdk to be installed.\nChanges will be applied, when the 'Options' dialog is closed.");
                                break;
                            default:
                                break;
                        }
                    }
                    else
                    {
                        SdkList.Add(new SdkViewModel(viewModel.SdkRootPath, SdkState.added) { ParentViewModel = this }) ;
                        model.SdkChangesCollector.AddSdk(viewModel.SdkRootPath);
                    }
                }
            }
        }

        public ICommand InstallCommand => new DelegateCommand(OnInstallButtonClicked);

        private void OnInstallButtonClicked()
        {
            InstallSdkViewModel viewModel = new InstallSdkViewModel();
            InstallSdkDialog installDialog = new InstallSdkDialog(viewModel);
            installDialog.ShowModal();
            if(installDialog.DialogResult == true)
            {
                if (!string.IsNullOrEmpty(viewModel.ArchiveFilePath))
                {
                    // an install is requested by the user

                    //check if destination path already in list
                    SdkViewModel existingSdk = SdkList.SingleOrDefault(sdk => sdk.Path.Equals(viewModel.SdkDestination));
                    if (existingSdk != null)
                    {
                        if(existingSdk.SdkState == SdkState.removed)
                            MessageBox.Show($"The destination\n'{viewModel.SdkDestination}'\nis in the list of sdks to be removed.\nApply the changes by pressing 'OK' in the 'Options' dialog.\nAfterwards the location can be used as destination for a newly installed sdk.");
                        else
                            MessageBox.Show($"The destination\n'{viewModel.SdkDestination}'\nis already in the list of sdks.\nRemove the entry before using it as installation destination.");
                    }
                    else
                    {
                        SdkList.Add(new SdkViewModel(viewModel.SdkDestination, SdkState.installed) { ParentViewModel = this});
                        model.SdkChangesCollector.InstallSdk(viewModel.ArchiveFilePath, viewModel.SdkDestination, viewModel.Force);
                    }
                }
            }
        }

        public ICommand RemoveCommand => new DelegateCommand(OnRemoveButtonClicked);

        private void OnRemoveButtonClicked()
        {
            Collection<SdkViewModel> sdksToRemove = new Collection<SdkViewModel>();
            foreach(SdkViewModel sdk in SdkList)
            {
                if(sdk.IsSelected)
                {
                    model.SdkChangesCollector.RemoveSdk(sdk.Path);
                    switch (sdk.SdkState)
                    {
                        case SdkState.unchanged:
                            sdk.SdkState = SdkState.removed;
                            break;
                        case SdkState.removed:
                            break;
                        case SdkState.added:
                            sdksToRemove.Add(sdk);
                            break;
                        case SdkState.installed:
                            sdksToRemove.Add(sdk);
                            break;
                        default:
                            break;
                    }
                }
            }
            foreach (SdkViewModel sdk in sdksToRemove)
            {
                SdkList.Remove(sdk);
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
