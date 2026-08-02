using OneDriveVersionCleaner.Core.Deletion;
using OneDriveVersionCleaner.Core.Models;

namespace OneDriveVersionCleaner.Core.Tests;

internal static class TestData
{
    public static readonly DateTimeOffset Now =
        new(2026, 8, 1, 12, 0, 0, TimeSpan.Zero);

    public static LocalFileCheck UnchangedLocalFile()
    {
        var sample = new LocalFileFingerprint(@"C:\OneDrive\report.xlsx", true, Now, 1_000, null);
        return new LocalFileCheck(sample, sample);
    }

    public static LocalFileCheck ChangedLocalFile()
    {
        var before = new LocalFileFingerprint(@"C:\OneDrive\report.xlsx", true, Now, 1_000, null);
        return new LocalFileCheck(before, before with { LastWriteTimeUtc = Now.AddMinutes(1) });
    }

    public static VersionEntry Version(
        string id,
        int daysAgo,
        long? size = 100,
        bool isCurrent = false,
        bool? canDelete = null) => new()
        {
            VersionId = id,
            Created = Now.AddDays(-daysAgo),
            Size = size,
            IsCurrent = isCurrent,
            CanDelete = canDelete,
        };

    public static FileEntry File(
        params VersionEntry[] versions) => new()
        {
            DriveId = "drive-1",
            ItemId = "item-1",
            Name = "report.xlsx",
            CloudPath = "Research/report.xlsx",
            LocalPath = @"C:\OneDrive\Research\report.xlsx",
            CurrentSize = 1_000,
            LastModified = Now.AddDays(-100),
            Versions = versions,
        };

    public static RetentionPolicy Policy(
        int keepLatest = 0,
        int keepDays = 0,
        int protectDays = 0,
        long minimumHistoryBytes = 0,
        long minimumFileBytes = 0,
        int minimumVersionCount = 0) => new()
        {
            KeepLatestVersions = keepLatest,
            KeepDays = keepDays,
            ProtectRecentlyModifiedFileDays = protectDays,
            MinimumHistoryBytes = minimumHistoryBytes,
            MinimumCurrentFileBytes = minimumFileBytes,
            MinimumPreviousVersionCount = minimumVersionCount,
        };
}
