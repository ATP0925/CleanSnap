using System.Globalization;

namespace CleanSnap.Converters;

/// <summary>
/// Converts a byte count (long, int, double) into a human-readable format (B, KB, MB, GB).
/// </summary>
public class BytesToReadableConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null) return "0 B";

        long bytes = value switch
        {
            long l => l,
            int i => i,
            double d => (long)d,
            float f => (long)f,
            _ => 0
        };

        return bytes switch
        {
            < 1024 => $"{bytes} B",
            < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
            < 1024L * 1024 * 1024 => $"{bytes / (1024.0 * 1024):F2} MB",
            _ => $"{bytes / (1024.0 * 1024 * 1024):F2} GB"
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw excitingNotImplemented();
    }

    private static NotImplementedException excitingNotImplemented() => new();
}

