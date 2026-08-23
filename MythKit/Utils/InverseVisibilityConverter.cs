using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace MythKit.Utils
{
    public class InverseVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType == typeof(Visibility))
            {
                return (Visibility)value == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
            }
            throw new InvalidOperationException("The target must be a Visibility");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType == typeof(Visibility))
            {
                return (Visibility)value == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
            }
            throw new InvalidOperationException("The target must be a Visibility");
        }
    }
}
