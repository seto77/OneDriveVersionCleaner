using OneDriveVersionCleaner.Core.Models;

namespace OneDriveVersionCleaner.Core.Retention;

/// <summary>
/// Evaluates deletion candidates conservatively. Missing values never become delete candidates.
/// </summary>
public sealed class RetentionPolicyEvaluator
{
    public RetentionEvaluationResult Evaluate(
        FileEntry file,
        RetentionPolicy policy,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(file);

        return Evaluate([file], policy, now);
    }

    public RetentionEvaluationResult Evaluate(
        IEnumerable<FileEntry> files,
        RetentionPolicy policy,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(files);
        ArgumentNullException.ThrowIfNull(policy);
        Validate(policy);

        var materializedFiles = files.ToArray();
        if (materializedFiles.Any(file => file is null))
        {
            throw new ArgumentException("The file sequence cannot contain null entries.", nameof(files));
        }

        return new RetentionEvaluationResult(
            [.. materializedFiles.Select(file => EvaluateFile(file!, policy, now))]);
    }

    private static FileRetentionEvaluation EvaluateFile(
        FileEntry file,
        RetentionPolicy policy,
        DateTimeOffset now)
    {
        var previousVersions = file.Versions.Where(version => !version.IsCurrent).ToArray();
        var commonHoldReason = GetFileHoldReason(file, previousVersions, policy, now);

        var versionsWithKnownDate = previousVersions
            .Where(version => version.Created.HasValue)
            .OrderByDescending(version => version.Created)
            .ThenByDescending(version => version.VersionId, StringComparer.Ordinal)
            .ToArray();

        DateTimeOffset? keepByCountBoundary = null;
        if (policy.KeepLatestVersions > 0 && versionsWithKnownDate.Length > 0)
        {
            var boundaryIndex = Math.Min(policy.KeepLatestVersions, versionsWithKnownDate.Length) - 1;
            keepByCountBoundary = versionsWithKnownDate[boundaryIndex].Created;
        }

        // Measured back from the file's own last change, not from the clock: a file that
        // stopped changing years ago should keep the versions around that final edit, instead
        // of losing all of them merely because time passed.
        var keepByAgeBoundary = policy.KeepDays > 0 && file.LastModified.HasValue
            ? file.LastModified.Value.AddDays(-policy.KeepDays)
            : (DateTimeOffset?)null;
        var decisions = new List<VersionRetentionDecision>(file.Versions.Count);

        foreach (var version in file.Versions)
        {
            var reasons = new List<RetentionReason>();

            if (version.IsCurrent)
            {
                reasons.Add(RetentionReason.CurrentVersion);
            }
            else if (commonHoldReason.HasValue)
            {
                reasons.Add(commonHoldReason.Value);
            }
            else
            {
                AddVersionKeepReasons(
                    version,
                    policy,
                    keepByCountBoundary,
                    keepByAgeBoundary,
                    reasons);
            }

            var disposition = reasons.Count == 0
                ? RetentionDisposition.DeleteCandidate
                : RetentionDisposition.Keep;

            if (disposition == RetentionDisposition.DeleteCandidate)
            {
                reasons.Add(RetentionReason.EligibleForDeletion);
            }

            decisions.Add(new VersionRetentionDecision(file, version, disposition, reasons));
        }

        return new FileRetentionEvaluation(file, decisions);
    }

    private static RetentionReason? GetFileHoldReason(
        FileEntry file,
        IReadOnlyCollection<VersionEntry> previousVersions,
        RetentionPolicy policy,
        DateTimeOffset now)
    {
        if (file.RetrievalStatus is not (RetrievalStatus.Success or RetrievalStatus.Warning))
        {
            return RetentionReason.FileRetrievalFailed;
        }

        // Both the protection window and the retention window are anchored to the file's last
        // change, so without it neither rule can be evaluated and the file is left alone.
        if ((policy.ProtectRecentlyModifiedFileDays > 0 || policy.KeepDays > 0) &&
            !file.LastModified.HasValue)
        {
            return RetentionReason.FileLastModifiedTimeUnknown;
        }

        // Zero disables the window. Without the guard the comparison still holds for a file
        // whose timestamp is now or later, which would protect it under a disabled rule.
        if (policy.ProtectRecentlyModifiedFileDays > 0 &&
            file.LastModified >= now.AddDays(-policy.ProtectRecentlyModifiedFileDays))
        {
            return RetentionReason.FileRecentlyModified;
        }

        // A small file cannot hold much history however often it was edited, so it is excluded
        // before anything else is weighed. An unknown size cannot clear the bar either.
        if (policy.MinimumCurrentFileBytes > 0)
        {
            if (file.CurrentSize is not { } currentSize)
            {
                return RetentionReason.FileSizeUnknown;
            }

            if (currentSize < policy.MinimumCurrentFileBytes)
            {
                return RetentionReason.FileBelowMinimumSize;
            }
        }

        if (previousVersions.Count < policy.MinimumPreviousVersionCount)
        {
            return RetentionReason.VersionCountBelowMinimum;
        }

        var knownHistoryBytes = previousVersions
            .Where(version => version.Size is >= 0)
            .Aggregate(0L, (total, version) => checked(total + version.Size!.Value));

        if (knownHistoryBytes < policy.MinimumHistoryBytes)
        {
            return RetentionReason.HistoryBelowMinimumSize;
        }

        return null;
    }

    private static void AddVersionKeepReasons(
        VersionEntry version,
        RetentionPolicy policy,
        DateTimeOffset? keepByCountBoundary,
        DateTimeOffset? keepByAgeBoundary,
        ICollection<RetentionReason> reasons)
    {
        if (version.RetrievalStatus is not (RetrievalStatus.Success or RetrievalStatus.Warning))
        {
            reasons.Add(RetentionReason.VersionRetrievalFailed);
        }

        if (!version.Created.HasValue)
        {
            reasons.Add(RetentionReason.VersionCreationTimeUnknown);
        }

        if (!version.Size.HasValue)
        {
            reasons.Add(RetentionReason.VersionSizeUnknown);
        }
        else if (version.Size.Value < 0)
        {
            reasons.Add(RetentionReason.InvalidVersionSize);
        }

        if (version.CanDelete == false)
        {
            reasons.Add(RetentionReason.ApiMarkedNotDeletable);
        }

        if (version.Created.HasValue &&
            keepByCountBoundary.HasValue &&
            version.Created.Value >= keepByCountBoundary.Value)
        {
            reasons.Add(RetentionReason.KeptByLatestCount);
        }

        if (keepByAgeBoundary.HasValue && version.Created >= keepByAgeBoundary.Value)
        {
            reasons.Add(RetentionReason.KeptByAge);
        }

        if (policy.KeepCreatedOnOrAfter.HasValue &&
            version.Created >= policy.KeepCreatedOnOrAfter.Value)
        {
            reasons.Add(RetentionReason.KeptByExplicitDate);
        }
    }

    private static void Validate(RetentionPolicy policy)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(policy.KeepLatestVersions);
        ArgumentOutOfRangeException.ThrowIfNegative(policy.KeepDays);
        ArgumentOutOfRangeException.ThrowIfNegative(policy.ProtectRecentlyModifiedFileDays);
        ArgumentOutOfRangeException.ThrowIfNegative(policy.MinimumHistoryBytes);
        ArgumentOutOfRangeException.ThrowIfNegative(policy.MinimumPreviousVersionCount);
    }
}
