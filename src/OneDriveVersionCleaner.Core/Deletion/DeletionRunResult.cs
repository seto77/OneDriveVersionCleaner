using OneDriveVersionCleaner.Core.Models;

namespace OneDriveVersionCleaner.Core.Deletion;

/// <summary>
/// What the service reported for one planned version.
/// </summary>
public sealed record VersionDeletionOutcome(
    FileEntry File,
    VersionEntry Version,
    DeleteResult Result)
{
    public bool IsSuccess => Result.IsSuccess;
}

/// <summary>
/// FR-DELETE-010 plus the local safety check: the cloud version list re-read after the run,
/// and the synced local copy compared against the sample taken before it.
/// <paramref name="RemainingPreviousVersionCount"/> is null when the re-read itself failed.
/// </summary>
public sealed record FileDeletionVerification(
    FileEntry File,
    int? RemainingPreviousVersionCount,
    IReadOnlyList<string> UnexpectedlyPresentVersionIds,
    LocalFileCheck LocalFile,
    string? Error)
{
    public bool IsVerified =>
        Error is null &&
        UnexpectedlyPresentVersionIds.Count == 0 &&
        LocalFile.IsSafe;
}

public sealed record DeletionRunResult(
    DateTimeOffset StartedAtUtc,
    DateTimeOffset CompletedAtUtc,
    IReadOnlyList<VersionDeletionOutcome> Outcomes,
    IReadOnlyList<FileDeletionVerification> Verifications,
    int NotAttemptedCount,
    bool WasStopped,
    string? AbortReason = null)
{
    public int AttemptedCount => Outcomes.Count;

    public int DeletedCount => CountByStatus(DeleteResultStatus.Deleted);

    public int AlreadyDeletedCount => CountByStatus(DeleteResultStatus.AlreadyDeleted);

    public int NotDeletableCount => CountByStatus(DeleteResultStatus.NotDeletable);

    public int AccessDeniedCount => CountByStatus(DeleteResultStatus.AccessDenied);

    public int TransientErrorCount => CountByStatus(DeleteResultStatus.TransientError);

    public int PermanentErrorCount => CountByStatus(DeleteResultStatus.PermanentError);

    public int FailureCount => Outcomes.Count(outcome => !outcome.IsSuccess);

    /// <summary>
    /// Files whose local copy changed while their history was being removed. Any entry here
    /// means the run stopped and needs manual inspection.
    /// </summary>
    public IReadOnlyList<FileDeletionVerification> LocalFileWarnings => Verifications
        .Where(verification => !verification.LocalFile.IsSafe)
        .ToArray();

    /// <summary>
    /// Only counts versions that were actually removed in this run. Sizes that the API never
    /// reported are excluded rather than treated as zero.
    /// </summary>
    public long KnownReclaimedBytes => Outcomes
        .Where(outcome => outcome.Result.Status == DeleteResultStatus.Deleted && outcome.Version.Size is >= 0)
        .Aggregate(0L, (total, outcome) => checked(total + outcome.Version.Size!.Value));

    public int UnknownReclaimedSizeCount => Outcomes.Count(
        outcome => outcome.Result.Status == DeleteResultStatus.Deleted && outcome.Version.Size is not >= 0);

    public TimeSpan Duration => CompletedAtUtc - StartedAtUtc;

    private int CountByStatus(DeleteResultStatus status) =>
        Outcomes.Count(outcome => outcome.Result.Status == status);
}
