using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using OneDriveVersionCleaner.Core.Deletion;
using OneDriveVersionCleaner.Core.Models;

namespace OneDriveVersionCleaner.Core.Auditing;

/// <summary>
/// Opens the append-only JSON Lines record of a deletion run (FR-LOG-003).
/// Only identifiers, sizes and API outcomes are written; no token, cookie or credential
/// ever reaches this file (FR-LOG-002).
/// </summary>
public sealed class DeletionAuditLog(string? directory = null)
{
    private readonly string _directory = string.IsNullOrWhiteSpace(directory)
        ? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OneDriveVersionCleaner",
            "Logs")
        : Path.GetFullPath(Environment.ExpandEnvironmentVariables(directory));

    public string GetLogPath(DateTimeOffset timestamp) => Path.Combine(
        _directory,
        $"delete-{timestamp.ToLocalTime():yyyyMMdd}.jsonl");

    /// <summary>
    /// Opens the journal for a run and writes its <c>runStarted</c> line. The caller is expected
    /// to treat a failure here as a reason not to delete anything: a run that cannot be recorded
    /// is a run nobody can audit afterwards.
    /// </summary>
    public async Task<DeletionRunJournal> BeginRunAsync(
        string accountKey,
        RetentionPolicy policy,
        int plannedVersionCount,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(policy);

        var startedAtUtc = DateTimeOffset.UtcNow;
        Directory.CreateDirectory(_directory);
        var journal = new DeletionRunJournal(GetLogPath(startedAtUtc), accountKey, startedAtUtc);
        try
        {
            await journal.WriteStartAsync(policy, plannedVersionCount, cancellationToken)
                .ConfigureAwait(false);
        }
        catch
        {
            await journal.DisposeAsync().ConfigureAwait(false);
            throw;
        }

        return journal;
    }
}

/// <summary>
/// One run's journal. Every attempt, every verification and the final tally are appended and
/// flushed as they happen, so a run that is killed part-way still leaves behind an account of
/// what it had already done.
/// </summary>
/// <remarks>
/// The absence of a <c>runCompleted</c> line is therefore meaningful: it says the run did not
/// reach its own end. Reading a file that ends in the middle is the point of the format.
/// </remarks>
public sealed class DeletionRunJournal : IAsyncDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    /// <summary>One process-wide gate, so two runs can never interleave halves of a line.</summary>
    private static readonly SemaphoreSlim WriteGate = new(1, 1);

    private static readonly UTF8Encoding Utf8 = new(encoderShouldEmitUTF8Identifier: false);

    private readonly string _accountKey;
    private readonly FileStream _stream;
    private bool _completed;

    internal DeletionRunJournal(string path, string accountKey, DateTimeOffset startedAtUtc)
    {
        Path = path;
        _accountKey = accountKey;
        StartedAtUtc = startedAtUtc;
        RunId = Guid.NewGuid().ToString("N");

        // WriteThrough because the interesting failure is the one that takes the process with
        // it: a line still sitting in a cache when that happens is a line that never existed.
        _stream = new FileStream(
            path,
            new FileStreamOptions
            {
                Mode = FileMode.Append,
                Access = FileAccess.Write,
                Share = FileShare.ReadWrite,
                Options = FileOptions.Asynchronous | FileOptions.WriteThrough,
            });
    }

    /// <summary>The file this run is being written to.</summary>
    public string Path { get; }

    /// <summary>
    /// When the journal was opened, which is also when the run started: the first recycle
    /// request cannot go out before this point, because opening the journal is what allows it.
    /// </summary>
    public DateTimeOffset StartedAtUtc { get; }

    /// <summary>Shared by every line of this run, so concurrent runs stay separable.</summary>
    public string RunId { get; }

    internal Task WriteStartAsync(
        RetentionPolicy policy,
        int plannedVersionCount,
        CancellationToken cancellationToken) => AppendAsync(
        "runStarted",
        StartedAtUtc,
        new
        {
            accountKey = _accountKey,
            plannedVersionCount,
            policy = new
            {
                policy.KeepLatestVersions,
                policy.KeepDays,
                policy.ProtectRecentlyModifiedFileDays,
                policy.MinimumHistoryBytes,
                policy.MinimumPreviousVersionCount,
                KeepCreatedOnOrAfter = Invariant(policy.KeepCreatedOnOrAfter),
            },
        },
        cancellationToken);

    /// <summary>Records one version the run asked the service to recycle, and what came back.</summary>
    public Task RecordAttemptAsync(
        VersionDeletionOutcome outcome,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(outcome);

        return AppendAsync(
            "versionRecycleAttempted",
            DateTimeOffset.UtcNow,
            new
            {
                accountKey = _accountKey,
                driveId = outcome.File.DriveId,
                itemId = outcome.File.ItemId,
                cloudPath = outcome.File.CloudPath,
                localPath = outcome.File.LocalPath,
                versionId = outcome.Version.VersionId,
                versionLabel = outcome.Version.Label,
                versionCreatedUtc = Invariant(outcome.Version.Created),
                versionSizeBytes = outcome.Version.Size,
                status = outcome.Result.Status.ToString(),
                succeeded = outcome.IsSuccess,
                error = outcome.Result.Error,
            },
            cancellationToken);
    }

    /// <summary>Records the re-read of one file's versions and the state of its local copy.</summary>
    public Task RecordVerificationAsync(
        FileDeletionVerification verification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(verification);

        return AppendAsync(
            "versionsReverified",
            DateTimeOffset.UtcNow,
            new
            {
                accountKey = _accountKey,
                driveId = verification.File.DriveId,
                itemId = verification.File.ItemId,
                cloudPath = verification.File.CloudPath,
                remainingPreviousVersionCount = verification.RemainingPreviousVersionCount,
                unexpectedlyPresentVersionIds = verification.UnexpectedlyPresentVersionIds,
                localFile = new
                {
                    path = verification.LocalFile.Before.Path,
                    change = verification.LocalFile.Change.ToString(),
                    existedBefore = verification.LocalFile.Before.Exists,
                    existsAfter = verification.LocalFile.After.Exists,
                    lastWriteBeforeUtc = Invariant(verification.LocalFile.Before.LastWriteTimeUtc),
                    lastWriteAfterUtc = Invariant(verification.LocalFile.After.LastWriteTimeUtc),
                    lengthBefore = verification.LocalFile.Before.Length,
                    lengthAfter = verification.LocalFile.After.Length,
                },
                verified = verification.IsVerified,
                error = verification.Error,
            },
            cancellationToken);
    }

    /// <summary>Closes the run with its tally. Without this line, the run did not finish.</summary>
    public async Task CompleteAsync(
        DeletionRunResult result,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(result);

        await AppendAsync(
            "runCompleted",
            result.CompletedAtUtc,
            new
            {
                accountKey = _accountKey,
                result.AttemptedCount,
                result.NotAttemptedCount,
                result.DeletedCount,
                result.AlreadyDeletedCount,
                result.NotDeletableCount,
                result.AccessDeniedCount,
                result.TransientErrorCount,
                result.PermanentErrorCount,
                result.KnownReclaimedBytes,
                result.UnknownReclaimedSizeCount,
                result.WasStopped,
                result.AbortReason,
                durationSeconds = result.Duration.TotalSeconds,
            },
            cancellationToken).ConfigureAwait(false);
        _completed = true;
    }

    /// <summary>Whether this run wrote its closing line.</summary>
    public bool IsComplete => _completed;

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _stream.FlushAsync().ConfigureAwait(false);
        await _stream.DisposeAsync().ConfigureAwait(false);
    }

    private async Task AppendAsync(
        string eventName,
        DateTimeOffset timestamp,
        object details,
        CancellationToken cancellationToken)
    {
        var line = JsonSerializer.Serialize(
            new
            {
                timestampUtc = timestamp.ToUniversalTime(),
                runId = RunId,
                eventName,
                details,
            },
            JsonOptions);
        var bytes = Utf8.GetBytes(line + Environment.NewLine);

        await WriteGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await _stream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
            await _stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            WriteGate.Release();
        }
    }

    private static string? Invariant(DateTimeOffset? value) =>
        value?.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
