using OneDriveVersionCleaner.Core.Models;

namespace OneDriveVersionCleaner.Core.Retention;

public enum RetentionDisposition
{
    Keep,
    DeleteCandidate,
    DeferredByRunLimit,
}

public enum RetentionReason
{
    CurrentVersion,
    FileRecentlyModified,
    FileLastModifiedTimeUnknown,
    FileRetrievalFailed,
    HistoryBelowMinimumSize,
    FileBelowMinimumSize,
    FileSizeUnknown,
    VersionCountBelowMinimum,
    VersionCreationTimeUnknown,
    VersionSizeUnknown,
    InvalidVersionSize,
    VersionRetrievalFailed,
    ApiMarkedNotDeletable,
    KeptByLatestCount,
    KeptByAge,
    KeptByExplicitDate,
    EligibleForDeletion,
    MaximumDeletesPerRunReached,
}

public sealed record VersionRetentionDecision(
    FileEntry File,
    VersionEntry Version,
    RetentionDisposition Disposition,
    IReadOnlyList<RetentionReason> Reasons)
{
    public bool IsDeleteCandidate => Disposition == RetentionDisposition.DeleteCandidate;
}

public sealed record FileRetentionEvaluation(
    FileEntry File,
    IReadOnlyList<VersionRetentionDecision> Decisions)
{
    public IReadOnlyList<VersionRetentionDecision> DeleteCandidates =>
        Decisions.Where(decision => decision.IsDeleteCandidate).ToArray();
}

public sealed record RetentionEvaluationResult(
    IReadOnlyList<FileRetentionEvaluation> Files)
{
    public IReadOnlyList<VersionRetentionDecision> Decisions =>
        Files.SelectMany(file => file.Decisions).ToArray();

    public IReadOnlyList<VersionRetentionDecision> DeleteCandidates =>
        Decisions.Where(decision => decision.IsDeleteCandidate).ToArray();

    public int DeferredDeleteCount =>
        Decisions.Count(decision => decision.Disposition == RetentionDisposition.DeferredByRunLimit);

    public long KnownReclaimableBytes => DeleteCandidates.Aggregate(
        0L,
        (total, decision) => checked(total + decision.Version.Size!.Value));
}
