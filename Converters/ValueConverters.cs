using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Anchor_PDF.Converters;

public class InverseBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool b ? !b : true;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool b ? !b : false;
    }
}

public class InverseBooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool b && b ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is Visibility v && v != Visibility.Visible;
    }
}

public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value != null ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class EqualityToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || parameter == null)
            return Visibility.Collapsed;

        return value.ToString() == parameter.ToString() ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class EqualityToBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || parameter == null)
            return false;

        return value.ToString() == parameter.ToString();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b && b && parameter != null && int.TryParse(parameter.ToString(), out int intVal))
            return intVal;

        return Binding.DoNothing;
    }
}

public class EnumToDisplayNameConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null)
        {
            return string.Empty;
        }

        if (value is Anchor_PDF.Models.PageSizePreset pageSize)
        {
            return pageSize switch
            {
                Anchor_PDF.Models.PageSizePreset.FitToImage => "Fit to Image",
                Anchor_PDF.Models.PageSizePreset.A4 => "A4",
                Anchor_PDF.Models.PageSizePreset.Letter => "Letter",
                _ => pageSize.ToString()
            };
        }

        if (value is Anchor_PDF.Models.SplitMode splitMode)
        {
            return splitMode switch
            {
                Anchor_PDF.Models.SplitMode.AllPages => "All Pages",
                Anchor_PDF.Models.SplitMode.CustomRange => "Custom Range",
                _ => splitMode.ToString()
            };
        }

        if (value is Anchor_PDF.Models.PageOrientationPreset orientation)
        {
            return orientation switch
            {
                Anchor_PDF.Models.PageOrientationPreset.Auto => "Auto",
                Anchor_PDF.Models.PageOrientationPreset.Portrait => "Portrait",
                Anchor_PDF.Models.PageOrientationPreset.Landscape => "Landscape",
                _ => orientation.ToString()
            };
        }

        string rawString = value.ToString() ?? string.Empty;
        return System.Text.RegularExpressions.Regex.Replace(rawString, "(?<=[a-z])(?=[A-Z])", " ");
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}


