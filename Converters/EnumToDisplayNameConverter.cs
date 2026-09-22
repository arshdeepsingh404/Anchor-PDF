using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Data;
using Anchor_PDF.Models;

namespace Anchor_PDF.Converters;

[ValueConversion(typeof(Enum), typeof(string))]
public class EnumToDisplayNameConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null)
        {
            return string.Empty;
        }

        if (value is PageSizePreset pageSize)
        {
            return pageSize switch
            {
                PageSizePreset.FitToImage => "Fit to Image",
                PageSizePreset.A4 => "A4",
                PageSizePreset.Letter => "Letter",
                _ => pageSize.ToString()
            };
        }

        if (value is SplitMode splitMode)
        {
            return splitMode switch
            {
                SplitMode.AllPages => "All Pages",
                SplitMode.CustomRange => "Custom Range",
                _ => splitMode.ToString()
            };
        }

        if (value is PageOrientationPreset orientation)
        {
            return orientation switch
            {
                PageOrientationPreset.Auto => "Auto",
                PageOrientationPreset.Portrait => "Portrait",
                PageOrientationPreset.Landscape => "Landscape",
                _ => orientation.ToString()
            };
        }

        string rawString = value.ToString() ?? string.Empty;
        return Regex.Replace(rawString, "(?<=[a-z])(?=[A-Z])", " ");
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
