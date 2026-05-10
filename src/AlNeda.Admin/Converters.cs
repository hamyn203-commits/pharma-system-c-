using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace AlNeda.Admin.Converters;

public class StringToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return string.IsNullOrWhiteSpace(value as string) ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool b && b ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool b && b ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value == null ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class StatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var status = (value as string ?? "").ToLower();
        return status switch
        {
            "delivered" or "active" or "paid" or "completed" => "#FF55D66B",
            "pending" or "in_review" or "partial" => "#FFD28A2D",
            "cancelled" or "blocked" or "rejected" => "#FFFF5C5C",
            "in_store" or "with_driver" or "on_the_way" => "#FF316DCA",
            _ => "#FF8B949E"
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class StatusToBgConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var status = (value as string ?? "").ToLower();
        return status switch
        {
            "delivered" or "active" or "paid" or "completed" => "#FF1F3B2A",
            "pending" or "in_review" or "partial" => "#FF3B2D1A",
            "cancelled" or "blocked" or "rejected" => "#FF3D1A1A",
            "in_store" or "with_driver" or "on_the_way" => "#FF1A2D3D",
            _ => "#FF21262D"
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class BalanceColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is decimal d)
        {
            if (d > 0) return "#FFFF5C5C";
            if (d < 0) return "#FF55D66B";
        }
        return "#FFFFFFFF";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b && !b;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b && !b;
}

public class StringEmptyToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return string.IsNullOrWhiteSpace(value as string) ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class BalanceBgConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is decimal d)
        {
            if (d > 0) return "#FF3D1A1A";
            if (d < 0) return "#FF1F3B2A";
        }
        return "#FF21262D";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class BoolToStatusBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool isActive = value is bool b && b;
        return isActive ? "#FF10B981" : "#FF6B7280"; // Green vs Gray
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class BoolToStatusTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool isActive = value is bool b && b;
        return isActive ? "نشط" : "معطل";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class BoolToOpacityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b && b ? 1.0 : 0.4;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class BoolToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b && b ? "إخفاء الفلاتر" : "إظهار الفلاتر";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class PositiveNegativeToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is decimal d)
        {
            if (d > 0) return "#FF10B981";
            if (d < 0) return "#FFEF4444";
        }
        return "#FFFFFFFF";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class ProfitMarginToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is decimal d)
        {
            if (d >= 20) return "#FF1F3B2A";
            if (d >= 10) return "#FF3B2D1A";
            return "#FF3D1A1A";
        }
        return "#FF21262D";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class TrendToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var trend = (value as string ?? "").ToLower();
        return trend switch
        {
            "up" => "#FF1F3B2A",
            "down" => "#FF3D1A1A",
            _ => "#FF3B2D1A"
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class TrendToForegroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var trend = (value as string ?? "").ToLower();
        return trend switch
        {
            "up" => "#FF55D66B",
            "down" => "#FFFF5C5C",
            _ => "#FFD28A2D"
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class PositiveIntegerRule : ValidationRule
{
    public override ValidationResult Validate(object value, CultureInfo cultureInfo)
    {
        if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            return new ValidationResult(true, null);

        if (!int.TryParse(value.ToString(), out int result))
            return new ValidationResult(false, "الكمية يجب أن تكون رقماً صحيحاً");

        if (result < 0)
            return new ValidationResult(false, "الكمية لا يمكن أن تكون سالبة");

        return ValidationResult.ValidResult;
    }
}

public class PositiveDecimalRule : ValidationRule
{
    public override ValidationResult Validate(object value, CultureInfo cultureInfo)
    {
        if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            return new ValidationResult(true, null);

        if (!decimal.TryParse(value.ToString(), out decimal result))
            return new ValidationResult(false, "السعر يجب أن يكون رقماً");

        if (result < 0)
            return new ValidationResult(false, "السعر لا يمكن أن يكون سالباً");

        return ValidationResult.ValidResult;
    }
}
