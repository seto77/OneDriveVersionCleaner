using System.Globalization;

namespace OneDriveVersionCleaner.Core.Formatting;

public static class ByteSizeFormatter
{
    private static readonly string[] Units = ["B", "KB", "MB", "GB", "TB"];

    public static string Format(
        long bytes,
        int maximumDecimalPlaces = 1,
        IFormatProvider? formatProvider = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(bytes);
        ArgumentOutOfRangeException.ThrowIfNegative(maximumDecimalPlaces);

        if (maximumDecimalPlaces > 6)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumDecimalPlaces),
                maximumDecimalPlaces,
                "At most six decimal places are supported.");
        }

        var value = (decimal)bytes;
        var unitIndex = 0;
        while (value >= 1024m && unitIndex < Units.Length - 1)
        {
            value /= 1024m;
            unitIndex++;
        }

        var format = maximumDecimalPlaces == 0
            ? "0"
            : $"0.{new string('#', maximumDecimalPlaces)}";

        return $"{value.ToString(format, formatProvider ?? CultureInfo.CurrentCulture)} {Units[unitIndex]}";
    }

    /// <summary>
    /// Formats a size that may be missing. <c>unknownText</c> stands in for one the service did
    /// not report, and has no default on purpose: what a reader should see depends on the
    /// interface language, and what a file should contain does not, so the caller decides.
    /// </summary>
    public static string Format(
        long? bytes,
        string unknownText,
        int maximumDecimalPlaces = 1,
        IFormatProvider? formatProvider = null) =>
        bytes.HasValue
            ? Format(bytes.Value, maximumDecimalPlaces, formatProvider)
            : unknownText;
}
