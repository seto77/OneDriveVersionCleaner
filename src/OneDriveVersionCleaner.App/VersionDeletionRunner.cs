using System.Globalization;
using OneDriveVersionCleaner.Core.Deletion;
using OneDriveVersionCleaner.Core.Models;

namespace OneDriveVersionCleaner.App;

internal sealed record VersionDeletionProgress(
    int CompletedCount,
    int TotalCount,
    string CloudPath,
    string VersionLabel);

/// <summary>
/// The state of one file as this run last observed it: what the service reported, and the
/// local sample taken at that same moment. They travel together so a caller never has to go
/// back to disk for a timestamp this run already read, nor pair a probe with a sample taken
/// at some other time.
/// </summary>
internal sealed record RefreshedFile(
    SharePointFileProbe Probe,
    LocalFileFingerprint LocalFile);

internal sealed record VersionDeletionRunOutput(
    DeletionRunResult Result,
    IReadOnlyDictionary<string, RefreshedFile> RefreshedFiles);

/// <summary>
/// Executes a <see cref="VersionDeletionPlan"/> against the signed-in SharePoint session.
/// Before touching a file it re-reads that file to confirm it is still the same item and that
/// the targets are still historical versions; afterwards it re-reads the version list
/// (FR-DELETE-010) and re-samples the synced local copy. Deleting history must never touch the
/// current file, so if the local copy disappears or its timestamp moves, the run stops.
/// </summary>
internal sealed class VersionDeletionRunner(
    SharePointWebSession session,
    Func<FileEntry, string?> serverRelativePathResolver)
{
    public async Task<VersionDeletionRunOutput> RunAsync(
        VersionDeletionPlan plan,
        IProgress<VersionDeletionProgress>? progress,
        CancellationToken stopToken)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var startedAtUtc = DateTimeOffset.UtcNow;
        var outcomes = new List<VersionDeletionOutcome>(plan.VersionCount);
        var verifications = new List<FileDeletionVerification>(plan.FileCount);
        var refreshed = new Dictionary<string, RefreshedFile>(StringComparer.Ordinal);
        var totalCount = plan.VersionCount;
        var stopped = false;
        string? abortReason = null;

        foreach (var file in plan.Files)
        {
            var serverRelativePath = serverRelativePathResolver(file.File);
            if (string.IsNullOrWhiteSpace(serverRelativePath))
            {
                RecordAll(outcomes, file, DeleteResultStatus.PermanentError,
                    "OneDrive上の位置を特定できませんでした。再度スキャンしてください。");
                continue;
            }

            var localBefore = LocalFileFingerprint.Capture(file.File.LocalPath);

            SharePointFileProbe current;
            try
            {
                current = await session.GetFileVersionsAsync(
                    serverRelativePath,
                    UniqueIdOf(file.File),
                    CancellationToken.None);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                RecordAll(outcomes, file, DeleteResultStatus.TransientError,
                    $"削除前の確認に失敗しました（{exception.GetType().Name}: {exception.Message}）");
                continue;
            }

            // The path was captured during the scan. If the item behind it is no longer the
            // file that was planned, a rename or replacement happened in between and every
            // guard so far validated the wrong item.
            if (!IsSameFile(file.File, current.File))
            {
                RecordAll(outcomes, file, DeleteResultStatus.PermanentError,
                    "走査時とは別のファイルがこの位置にあります。再度スキャンしてください。");
                continue;
            }

            // This read already is the file's authoritative state, so it is published before
            // anything is recycled. Without it, a selection whose versions all turned out to be
            // gone already would refresh nothing — the post-delete verification only runs when
            // something was actually attempted — and those rows would stay on screen for ever,
            // reporting "already in the recycle bin" every time they were selected again.
            refreshed[file.File.ItemId] = new RefreshedFile(current, localBefore);

            // Sets, not a dictionary: a version id repeated across a page boundary must not
            // throw in the middle of a run that has already recycled something.
            var present = current.Versions
                .Select(version => version.ID.ToString(CultureInfo.InvariantCulture))
                .ToHashSet(StringComparer.Ordinal);
            var historical = current.Versions
                .Where(version => !version.IsCurrentVersion)
                .Select(version => version.ID.ToString(CultureInfo.InvariantCulture))
                .ToHashSet(StringComparer.Ordinal);

            var attemptedAnything = false;
            foreach (var version in file.Versions)
            {
                if (stopToken.IsCancellationRequested)
                {
                    stopped = true;
                    break;
                }

                progress?.Report(new VersionDeletionProgress(
                    outcomes.Count,
                    totalCount,
                    file.File.CloudPath,
                    version.Label ?? version.VersionId));

                if (!historical.Contains(version.VersionId))
                {
                    // Still there but no longer historical means it became the current version;
                    // that is a refusal, not a completed deletion, so the two are kept apart.
                    outcomes.Add(new VersionDeletionOutcome(
                        file.File,
                        version,
                        present.Contains(version.VersionId)
                            ? new DeleteResult
                            {
                                Status = DeleteResultStatus.NotDeletable,
                                Error = "削除直前の確認で、現行バージョンになっていました。",
                            }
                            : new DeleteResult
                            {
                                Status = DeleteResultStatus.AlreadyDeleted,
                                Error = "削除直前の確認で、既に存在しませんでした。",
                            }));
                    continue;
                }

                DeleteResult result;
                try
                {
                    // FR-DELETE-009: stopping must not abandon a request that is already in
                    // flight, so the stop token gates the loop instead of the request itself.
                    // SharePointWebSession bounds every request with its own timeout.
                    result = await session.RecycleFileVersionAsync(
                        serverRelativePath,
                        UniqueIdOf(file.File),
                        version.VersionId,
                        CancellationToken.None);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    // Deliberately broad: once anything has been recycled, an escaping
                    // exception would take the audit log and the post-checks with it. Every
                    // failure therefore ends the run through the normal path, carrying its
                    // type so an unexpected one is still diagnosable.
                    outcomes.Add(new VersionDeletionOutcome(file.File, version, new DeleteResult
                    {
                        Status = DeleteResultStatus.PermanentError,
                        Error = $"{exception.GetType().Name}: {exception.Message}",
                    }));
                    attemptedAnything = true;
                    abortReason =
                        $"処理を継続できませんでした（{exception.GetType().Name}: {exception.Message}）。";
                    break;
                }

                attemptedAnything = true;
                outcomes.Add(new VersionDeletionOutcome(file.File, version, result));
            }

            if (attemptedAnything)
            {
                var verification = await VerifyAsync(file, serverRelativePath, localBefore, outcomes, refreshed);
                verifications.Add(verification);

                if (!verification.LocalFile.IsSafe)
                {
                    abortReason =
                        $"ローカルファイルの状態が変化しました（{verification.File.LocalPath}: " +
                        $"{DescribeLocalChange(verification.LocalFile.Change)}）。安全のため中止しました。";
                }
            }

            if (stopped || abortReason is not null)
            {
                break;
            }
        }

        progress?.Report(new VersionDeletionProgress(outcomes.Count, totalCount, string.Empty, string.Empty));

        var runResult = new DeletionRunResult(
            startedAtUtc,
            DateTimeOffset.UtcNow,
            outcomes,
            verifications,
            NotAttemptedCount: totalCount - outcomes.Count,
            WasStopped: stopped,
            AbortReason: abortReason);

        return new VersionDeletionRunOutput(runResult, refreshed);
    }

    internal static string DescribeLocalChange(LocalFileChange change) => change switch
    {
        LocalFileChange.Disappeared => "ローカルから消えています",
        LocalFileChange.TimestampChanged => "更新日時が変化しました",
        LocalFileChange.LengthChanged => "サイズが変化しました",
        LocalFileChange.CheckFailed => "ローカルを読み取れず確認できませんでした",
        LocalFileChange.NotChecked => "ローカルに実体がなく確認対象外です",
        _ => "変化なし",
    };

    /// <summary>
    /// Confirms the item behind the scanned path is still the planned file. Without a
    /// UniqueId on both sides, identity would degrade to comparing the path with itself, so
    /// the check fails closed instead.
    /// </summary>
    /// <summary>
    /// The scanner stores the SharePoint UniqueId as the item id, falling back to the path
    /// when the service did not supply one. Only the id form is usable for addressing.
    /// </summary>
    private static Guid UniqueIdOf(FileEntry file) =>
        Guid.TryParse(file.ItemId, out var uniqueId) ? uniqueId : Guid.Empty;

    private static bool IsSameFile(FileEntry expected, SharePointFileInfo actual) =>
        actual.UniqueId != Guid.Empty &&
        string.Equals(expected.ItemId, actual.UniqueId.ToString("D"), StringComparison.OrdinalIgnoreCase);

    private static void RecordAll(
        List<VersionDeletionOutcome> outcomes,
        PlannedFileDeletion file,
        DeleteResultStatus status,
        string error) => outcomes.AddRange(file.Versions.Select(version =>
        new VersionDeletionOutcome(file.File, version, new DeleteResult
        {
            Status = status,
            Error = error,
        })));

    private async Task<FileDeletionVerification> VerifyAsync(
        PlannedFileDeletion file,
        string serverRelativePath,
        LocalFileFingerprint localBefore,
        IReadOnlyList<VersionDeletionOutcome> outcomes,
        Dictionary<string, RefreshedFile> refreshed)
    {
        var localCheck = new LocalFileCheck(
            localBefore,
            LocalFileFingerprint.Capture(file.File.LocalPath));

        try
        {
            var probe = await session.GetFileVersionsAsync(
                serverRelativePath,
                UniqueIdOf(file.File),
                CancellationToken.None);

            // Paired with the sample taken moments ago for this same check, so whoever records
            // this file next does not have to read the timestamp off disk a third time.
            refreshed[file.File.ItemId] = new RefreshedFile(probe, localCheck.After);

            var remainingIds = probe.Versions
                .Where(version => !version.IsCurrentVersion)
                .Select(version => version.ID.ToString(CultureInfo.InvariantCulture))
                .ToHashSet(StringComparer.Ordinal);

            var stillPresent = outcomes
                .Where(outcome =>
                    ReferenceEquals(outcome.File, file.File) &&
                    outcome.Result.Status == DeleteResultStatus.Deleted &&
                    remainingIds.Contains(outcome.Version.VersionId))
                .Select(outcome => outcome.Version.VersionId)
                .ToArray();

            return new FileDeletionVerification(
                file.File,
                remainingIds.Count,
                stillPresent,
                localCheck,
                null);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // The re-read is the last chance to notice something wrong, so its own failure is
            // reported as an unverified file rather than replacing the run's results.
            return new FileDeletionVerification(
                file.File,
                null,
                [],
                localCheck,
                $"{exception.GetType().Name}: {exception.Message}");
        }
    }
}
