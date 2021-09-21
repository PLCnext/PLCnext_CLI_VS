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
using System.Windows.Media;

namespace PlcncliSdkOptionPage.Common.Converter
{
    [ValueConversion(typeof(SdkState), typeof(Brush))]
    public class SdkStateToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            SdkState state = (SdkState)value;
            if (state == SdkState.unchanged)
                return Brushes.Black;
            if (state == SdkState.removed)
                return Brushes.Gray;
            return Brushes.Blue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            
            throw new NotImplementedException();
        }
    }
}
