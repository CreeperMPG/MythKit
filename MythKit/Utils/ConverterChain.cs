using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Markup;

namespace MythKit.Utils
{
    [ContentProperty("Converters")]
    public class ConverterChain : IValueConverter
    {
        private readonly Collection<IValueConverter> _converters = new Collection<IValueConverter>();
        public Collection<IValueConverter> Converters => _converters;
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                foreach (var converter in Converters)
                {
                    value = converter.Convert(value, targetType, parameter, culture);
                }
                return value;
            }
            catch
            {
                throw new InvalidOperationException("An error occurred while converting the value using the converter chain.");
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
