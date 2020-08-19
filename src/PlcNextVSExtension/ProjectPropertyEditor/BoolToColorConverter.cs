#region Copyright
///////////////////////////////////////////////////////////////////////////////
//
//  Copyright (c) Phoenix Contact GmbH & Co KG
//  This software is licensed under Apache-2.0
//
///////////////////////////////////////////////////////////////////////////////
#endregion

using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace PlcNextVSExtension.ProjectPropertyEditor
{
    [ValueConversion(typeof(bool?), typeof(Brush))]
    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool? isBlack = (bool?)value;
            if (isBlack == false)
                return Brushes.DarkRed;
            return Brushes.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
