using System.Globalization;
using System.Windows.Data;

namespace Anchor_PDF.Converters;

[ValueConversion(typeof(bool), typeof(bool))]
public class InverseBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool booleanValue ? !booleanValue : true;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool booleanValue ? !booleanValue : false;
    }
}
