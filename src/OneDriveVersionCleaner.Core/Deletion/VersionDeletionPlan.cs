using OneDriveVersionCleaner.Core.Models;

namespace OneDriveVersionCleaner.Core.Deletion;

/// <summary>
/// One version scheduled for removal, kept together with the file that owns it.
/// </summary>
public sealed record PlannedVersionDeletion(FileEntry File, VersionEntry Version);

/// <summary>
/// The versions scheduled for removal from a single file, oldest first.
/// </summary>
public sealed record PlannedFileDeletion(FileEntry File, IReadOnlyList<VersionEntry> Versions)
{
    public int VersionCount => Versions.Count;

    public long KnownReclaimableBytes => Versions
        .Where(version => version.Size is >= 0)
        .Aggregate(0L, (total, version) => checked(total + version.Size!.Value));

    public int UnknownSizeCount => Versions.Count(version => version.Size is not >= 0);
}

/// <summary>
/// An immutable, reviewable list of the versions a run will move to the recycle bin.
/// Files stay grouped so that a caller resolves each file once, and versions run oldest
/// first within a file.
/// </summary>
public sealed record VersionDeletionPlan(
    IReadOnlyList<PlannedFileDeletion> Files,
    int DeferredVersionCount)
{
    public IReadOnlyList<PlannedVersionDeletion> Versions => Files
        .SelectMany(file => file.Versions.Select(version => new PlannedVersionDeletion(file.File, version)))
        .ToArray();

    public int FileCount => Files.Count;

    public int VersionCount => Files.Sum(file => file.VersionCount);

    public long KnownReclaimableBytes => Files.Aggregate(
        0L,
        (total, file) => checked(total + file.KnownReclaimableBytes));

    public int UnknownSizeCount => Files.Sum(file => file.UnknownSizeCount);

    public bool IsEmpty => VersionCount == 0;
}
