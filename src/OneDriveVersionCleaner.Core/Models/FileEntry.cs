namespace OneDriveVersionCleaner.Core.Models;

/// <summary>
/// A OneDrive file and the versions discovered for it.
/// </summary>
public sealed record FileEntry
{
    public string DriveId { get; init; } = string.Empty;

    public string ItemId { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string CloudPath { get; init; } = string.Empty;

    public string LocalPath { get; init; } = string.Empty;

    public string WebUrl { get; init; } = string.Empty;

    public long? CurrentSize { get; init; }

    public DateTimeOffset? LastModified { get; init; }

    public string? ModifiedBy { get; init; }

    public string? FileType { get; init; }

    public RetrievalStatus RetrievalStatus { get; init; } = RetrievalStatus.Success;

    public string? Error { get; init; }

    public IReadOnlyList<VersionEntry> Versions { get; init; } = [];

    public int PreviousVersionCount => Versions.Count(version => !version.IsCurrent);

    public int UnknownPreviousVersionSizeCount => Versions.Count(
        version => !version.IsCurrent && version.Size is not >= 0);

    public long KnownPreviousVersionTotalSize => Versions
        .Where(version => !version.IsCurrent && version.Size is >= 0)
        .Aggregate(0L, (total, version) => checked(total + version.Size!.Value));

    /// <summary>
    /// The exact historical size, or null when at least one historical size is unknown.
    /// </summary>
    public long? PreviousVersionTotalSize => UnknownPreviousVersionSizeCount == 0
        ? KnownPreviousVersionTotalSize
        : null;
}
