using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using OneDriveVersionCleaner.Core.Deletion;
using OneDriveVersionCleaner.Core.Models;

namespace OneDriveVersionCleaner.Core.Auditing;

/// <summary>
/// Append-only JSON Lines record of every deletion attempt (FR-LOG-003).
/// Only identifiers, sizes and API outcomes are written; no token, cookie or credential
/// ever reaches this file (FR-LOG-002).
/// </summary>
public sealed class DeletionAuditLog(string? directory = null)
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    /// <summary>One process-wide gate, so two runs can never interleave their lines.</summary>
    private static readonly SemaphoreSlim WriteGate = new(1, 1);

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
    /// Writes one <c>runStarted</c> line, one line per attempted version, one line per
    /// verification, and one <c>runCompleted</c> line. Returns the file that was written.
    /// </summary>
    public async Task<string> WriteRunAsync(
        string accountKey,
        RetentionPolicy policy,
        DeletionRunResult result,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(result);

        var runId = Guid.NewGuid().ToString("N");
        var lines = new List<string>(result.Outcomes.Count + result.Verifications.Count + 2)
        {
            Serialize("runStarted", runId, result.StartedAtUtc, new
            {
                accountKey,
                plannedVersionCount = result.AttemptedCount + result.NotAttemptedCount,
                policy = new
                {
                    policy.KeepLatestVersions,
                    policy.KeepDays,
                    policy.ProtectRecentlyModifiedFileDays,
                    policy.MinimumHistoryBytes,
                    policy.MinimumPreviousVersionCount,
                    KeepCreatedOnOrAfter = Invariant(policy.KeepCreatedOnOrAfter),
                },
            }),
        };

        lines.AddRange(result.Outcomes.Select(outcome =>
            Serialize("versionRecycleAttempted", runId, result.StartedAtUtc, new
            {
                accountKey,
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
            })));

        lines.AddRange(result.Verifications.Select(verification =>
            Serialize("versionsReverified", runId, result.CompletedAtUtc, new
            {
                accountKey,
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
            })));

        lines.Add(Serialize("runCompleted", runId, result.CompletedAtUtc, new
        {
            accountKey,
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
        }));

        var path = GetLogPath(result.StartedAtUtc);
        var payload = string.Concat(lines.Select(line => line + Environment.NewLine));

        await WriteGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            Directory.CreateDirectory(_directory);
            await File.AppendAllTextAsync(
                path,
                payload,
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            WriteGate.Release();
        }

        return path;
    }

    private static string Serialize(
        string eventName,
        string runId,
        DateTimeOffset timestamp,
        object details) => JsonSerializer.Serialize(
        new
        {
            timestampUtc = timestamp.ToUniversalTime(),
            runId,
            eventName,
            details,
        },
        JsonOptions);

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
