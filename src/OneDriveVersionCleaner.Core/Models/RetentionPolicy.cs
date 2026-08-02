namespace OneDriveVersionCleaner.Core.Models;

/// <summary>
/// Safe retention settings. Count-based and time-based rules are combined with OR:
/// satisfying either rule keeps the version.
/// </summary>
public sealed record RetentionPolicy
{
    public int KeepLatestVersions { get; init; } = 10;

    /// <summary>
    /// How far back from the file's own last modification its versions are kept.
    /// The window is anchored to the file, not to the current time, so a file that stopped
    /// changing long ago keeps the versions around its final edit. Zero disables the rule.
    /// </summary>
    public int KeepDays { get; init; } = 30;

    public DateTimeOffset? KeepCreatedOnOrAfter { get; init; }

    public int ProtectRecentlyModifiedFileDays { get; init; } = 7;

    /// <summary>
    /// Files whose known historical total is below this are left alone entirely.
    /// Zero disables the rule.
    /// </summary>
    public long MinimumHistoryBytes { get; init; } = 10L * 1024 * 1024;

    /// <summary>
    /// Files smaller than this are left alone entirely. A small file cannot hold much history
    /// however often it was edited, so it is not worth a request, let alone a deletion.
    /// Zero disables the rule.
    /// </summary>
    public long MinimumCurrentFileBytes { get; init; } = 50L * 1024 * 1024;

    public int MinimumPreviousVersionCount { get; init; }
}
