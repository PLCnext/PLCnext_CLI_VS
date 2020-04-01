#region Copyright
///////////////////////////////////////////////////////////////////////////////
//
//  Copyright (c) Phoenix Contact GmbH & Co KG
//  This software is licensed under Apache-2.0
//
///////////////////////////////////////////////////////////////////////////////
#endregion

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace PlcncliServices.LocationService
{
    [Guid("E0865D49-D384-4D95-89D5-A04B1D51EC43")]
    public class PlcncliOptionPage : DialogPage, INotifyPropertyChanged
    {
        private string _toolLocation = "";

        [Category("PLCnCLI")]
        [DisplayName("plcncli folder")]
        [Description("path to a folder containing the plcncli.exe")]
        public string ToolLocation
        {
            get => _toolLocation;
            set
            {
                if (value == _toolLocation)
                    return;
                _toolLocation = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
