using OneDriveVersionCleaner.Core.Formatting;
using static OneDriveVersionCleaner.App.Localization.Localization;

namespace OneDriveVersionCleaner.App.Localization;

/// <summary>
/// Formats a byte count for the screen.
/// </summary>
/// <remarks>
/// <see cref="ByteSizeFormatter"/> deliberately has no wording of its own for a size the service
/// did not report, so that the exports and the audit log cannot start changing language with the
/// interface. This is where the interface supplies its own.
/// </remarks>
internal static class SizeText
{
    public static string Of(long bytes) => ByteSizeFormatter.Format(bytes);

    public static string Of(long? bytes) =>
        ByteSizeFormatter.Format(bytes, Loc(en: "Unknown", ja: "不明"));
}
