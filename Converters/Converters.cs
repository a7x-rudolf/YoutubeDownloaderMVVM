using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using YoutubeDownloaderMVVM;

namespace YoutubeDownloaderMVVM.Converters
{
    /// <summary>String kosong -> Collapsed, string terisi -> Visible. Pakai ConverterParameter="Invert" untuk membalik.</summary>
    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            bool hasValue = value is string s && !string.IsNullOrWhiteSpace(s);
            bool invert = string.Equals(parameter as string, "Invert", StringComparison.OrdinalIgnoreCase);
            if (invert) hasValue = !hasValue;
            return hasValue ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    /// <summary>Membalik boolean, berguna untuk IsEnabled = !IsBusy.</summary>
    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is bool b ? !b : value ?? false;

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is bool b ? !b : value ?? false;
    }

    /// <summary>Bool -> Visibility. Pakai ConverterParameter="Invert" untuk membalik.</summary>
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            bool b = value is bool v && v;
            bool invert = string.Equals(parameter as string, "Invert", StringComparison.OrdinalIgnoreCase);
            if (invert) b = !b;
            return b ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    /// <summary>Jumlah item collection -> Visibility (0 = Collapsed).</summary>
    public class CountToVisibilityConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            int count = value is int i ? i : 0;
            bool invert = string.Equals(parameter?.ToString(), "Invert", StringComparison.OrdinalIgnoreCase);
            bool visible = count > 0;
            if (invert) visible = !visible;
            return visible ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }


    /// <summary>Mengurangi nilai double dengan parameter numerik; dipakai untuk lebar konten di dalam ScrollViewer.</summary>
    public class DoubleSubtractConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is double width && double.TryParse(parameter?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var subtract))
                return Math.Max(0, width - subtract);

            return value ?? 0d;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    /// <summary>StatusKind -> warna aksen (dipakai untuk banner status & ikon).</summary>
    public class StatusKindToBrushConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var brush = value is StatusKind kind
                ? kind switch
                {
                    StatusKind.Success => "#2FBF71",
                    StatusKind.Warning => "#F5A623",
                    StatusKind.Error => "#EB5757",
                    _ => "#FF0000"
                }
                : "#FF0000";

            return new System.Windows.Media.BrushConverter().ConvertFromString(brush)!;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    /// <summary>StatusKind -> nama PackIcon Material Design yang relevan.</summary>
    public class StatusKindToIconConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is StatusKind kind
                ? kind switch
                {
                    StatusKind.Success => "CheckCircleOutline",
                    StatusKind.Warning => "AlertCircleOutline",
                    StatusKind.Error => "CloseCircleOutline",
                    _ => "InformationOutline"
                }
                : "InformationOutline";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
