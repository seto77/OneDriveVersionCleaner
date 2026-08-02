using OneDriveVersionCleaner.Core.Models;

namespace OneDriveVersionCleaner.Core.Deletion;

/// <summary>
/// Turns the versions a user picked into an ordered removal plan.
/// </summary>
public sealed class VersionDeletionPlanner
{
    /// <summary>
    /// Builds the plan for <paramref name="selection"/>. Everything selected is planned: the
    /// selection is the user's, and the confirmation dialog states its size before anything is
    /// touched. Within a file the oldest version goes first, so an interrupted run leaves the
    /// newest history intact (FR-DELETE-004).
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// A current version, or a version the API already marked undeletable, reached the
    /// selection. That is a defect upstream, so the run is refused rather than trimmed.
    /// </exception>
    public VersionDeletionPlan Create(IEnumerable<PlannedVersionDeletion> selection)
    {
        ArgumentNullException.ThrowIfNull(selection);

        var seen = new HashSet<(string ItemId, string VersionId)>();
        var candidates = new List<PlannedVersionDeletion>();
        foreach (var candidate in selection)
        {
            ArgumentNullException.ThrowIfNull(candidate);
            Guard(candidate.File, candidate.Version);
            if (seen.Add((candidate.File.ItemId, candidate.Version.VersionId)))
            {
                candidates.Add(candidate);
            }
        }

        var files = candidates
            .GroupBy(candidate => candidate.File)
            .Select(group => new PlannedFileDeletion(
                group.Key,
                group
                    .Select(candidate => candidate.Version)
                    .OrderBy(version => version.Created ?? DateTimeOffset.MaxValue)
                    .ThenBy(version => version.VersionId, StringComparer.Ordinal)
                    .ToArray()))
            .OrderBy(file => file.File.CloudPath, StringComparer.OrdinalIgnoreCase)
            .ThenBy(file => file.File.ItemId, StringComparer.Ordinal)
            .ToArray();

        return new VersionDeletionPlan(files);
    }

    private static void Guard(FileEntry file, VersionEntry version)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(version);

        if (version.IsCurrent)
        {
            throw new InvalidOperationException(
                $"The current version of '{file.CloudPath}' cannot be scheduled for deletion.");
        }

        if (version.CanDelete == false)
        {
            throw new InvalidOperationException(
                $"Version '{version.VersionId}' of '{file.CloudPath}' is marked as not deletable.");
        }
    }
}
