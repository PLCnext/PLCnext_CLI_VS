#region Copyright
///////////////////////////////////////////////////////////////////////////////
//
//  Copyright (c) Phoenix Contact GmbH & Co KG
//  This software is licensed under Apache-2.0
//
///////////////////////////////////////////////////////////////////////////////
#endregion

using PlcncliSdkOptionPage.ChangeSDKsProperty;
using System;
using System.Globalization;
using System.Windows.Data;

namespace PlcncliSdkOptionPage.Common.Converter
{
    [ValueConversion(typeof(SdkState), typeof(bool))]
    public class SdkStateToFocusableConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            SdkState state = (SdkState)value;
            if(state == SdkState.removed)
            {
                return false;
            }
            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
