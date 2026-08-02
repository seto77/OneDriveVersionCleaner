using OneDriveVersionCleaner.Core.Models;

namespace OneDriveVersionCleaner.Core.Aggregation;

public sealed record FileScanAggregate(
    FileEntry File,
    long? CurrentSizeBytes,
    int PreviousVersionCount,
    long KnownPreviousVersionBytes,
    int UnknownPreviousVersionSizeCount,
    long? PreviousVersionTotalBytes,
    long? EstimatedAllVersionsTotalBytes,
    int DeleteCandidateCount,
    long KnownReclaimableBytes,
    int UnknownDeleteCandidateSizeCount,
    DateTimeOffset? OldestPreviousVersionCreated,
    DateTimeOffset? LatestPreviousVersionCreated,
    int ErrorCount)
{
    public bool IsHistorySizeComplete => UnknownPreviousVersionSizeCount == 0;

    public bool IsReclaimableSizeComplete => UnknownDeleteCandidateSizeCount == 0;
}

/// <summary>
/// A recursive roll-up: a folder includes files in all descendant folders.
/// The empty path represents the selected cloud root.
/// </summary>
public sealed record FolderScanAggregate(
    string CloudFolderPath,
    int FileCount,
    int FilesWithPreviousVersions,
    int PreviousVersionCount,
    long KnownPreviousVersionBytes,
    int UnknownPreviousVersionSizeCount,
    int DeleteCandidateCount,
    long KnownReclaimableBytes,
    int UnknownDeleteCandidateSizeCount,
    int ErrorCount);

public sealed record ScanSummary(
    int TargetFileCount,
    int FilesWithPreviousVersions,
    int PreviousVersionCount,
    long KnownPreviousVersionBytes,
    int UnknownPreviousVersionSizeCount,
    int DeleteCandidateCount,
    long KnownReclaimableBytes,
    int UnknownDeleteCandidateSizeCount,
    int ErrorCount,
    TimeSpan? ScanDuration)
{
    public bool IsHistorySizeComplete => UnknownPreviousVersionSizeCount == 0;

    public bool IsReclaimableSizeComplete => UnknownDeleteCandidateSizeCount == 0;
}

public sealed record ScanAggregate(
    IReadOnlyList<FileScanAggregate> Files,
    IReadOnlyList<FolderScanAggregate> Folders,
    ScanSummary Summary);
