using System.Globalization;
using System.Text.Json;
using OneDriveVersionCleaner.Core.Caching;
using OneDriveVersionCleaner.Core.Models;
using OneDriveVersionCleaner.Core.Scanning;
using OneDriveVersionCleaner.Infrastructure.OneDriveAccounts;

namespace OneDriveVersionCleaner.App;

internal sealed record ScanProgress(int Completed, int Total);

/// <summary>
/// A scanned file together with the address needed to act on it again.
/// </summary>
/// <param name="QueriedAtUtc">
/// When this file's versions were read from the service. Carried over unchanged for a reused
/// answer, so reusing it does not keep resetting its age and postponing the next refresh.
/// </param>
/// <param name="LocalLastWriteUtc">
/// The synced copy's write time at that moment. This is what a later run compares against to
/// decide whether the file has to be asked about again.
/// </param>
internal sealed record ScannedFile(
    FileEntry Entry,
    string ServerRelativePath,
    DateTimeOffset QueriedAtUtc,
    DateTimeOffset? LocalLastWriteUtc);

/// <summary>
/// What the scan did. The survey's own figures are not repeated here: the caller still holds
/// the <see cref="LocalSurveyResult"/> it passed in, and one source for a number is enough.
/// </summary>
internal sealed record ScanResult(
    IReadOnlyList<ScannedFile> Files,
    IReadOnlyList<string> Errors,
    int QueriedFileCount = 0,
    int ReusedFromCacheCount = 0);

/// <summary>
/// How much of the previous answer a scan may reuse.
/// </summary>
internal sealed record VersionCacheOptions(
    IReadOnlyDictionary<string, CachedFileVersions> Entries,
    TimeSpan MinimumAge)
{
    public static VersionCacheOptions Disabled { get; } =
        new(new Dictionary<string, CachedFileVersions>(StringComparer.Ordinal), TimeSpan.Zero);
}

/// <summary>
/// Asked before any version request goes out, with the number of files that would be read one
/// by one. Returning false ends the scan with what the cache already provided.
/// </summary>
internal delegate bool ConfirmVersionQuery(int fileCount);

/// <summary>
/// Reads version history for files the local survey already selected.
/// The service is never asked to enumerate anything: the set of files is decided locally, so
/// the request count is known before the first request and is bounded by the size threshold
/// rather than by the size of the library.
/// </summary>
internal sealed class OneDriveScanner(
    SharePointWebSession session,
    OneDriveBusinessAccount account,
    string libraryRoot)
{
    /// <summary>How many per-file reads are started at once.</summary>
    private const int QueryBatchSize = 32;

    /// <summary>
    /// Asks the service for the history of every surveyed file that the cache cannot answer for.
    /// </summary>
    public async Task<ScanResult> ScanAsync(
        LocalSurveyResult survey,
        VersionCacheOptions cache,
        ConfirmVersionQuery confirmQuery,
        IProgress<ScanProgress>? progress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(survey);
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(confirmQuery);

        // Anything already answered for stays out of the request budget until the local copy
        // both changes and the answer has aged past the configured interval.
        var cachedByLocalPath = BuildCacheIndex(cache);
        var now = DateTimeOffset.UtcNow;
        var reused = new List<ScannedFile>();
        var toQuery = new List<LocalCandidateFile>(survey.Candidates.Count);
        foreach (var candidate in survey.Candidates)
        {
            var cached = cachedByLocalPath.GetValueOrDefault(candidate.LocalPath);
            if (VersionCachePolicy.ShouldRequery(
                    cached,
                    candidate.LastWriteTimeUtc,
                    now,
                    cache.MinimumAge))
            {
                toQuery.Add(candidate);
            }
            else
            {
                reused.Add(new ScannedFile(
                    ToFileEntry(cached!),
                    cached!.ServerRelativePath,
                    cached.QueriedAtUtc,
                    cached.LocalLastWriteUtc));
            }
        }

        if (toQuery.Count > 0 && !confirmQuery(toQuery.Count))
        {
            return new ScanResult(
                reused,
                [],
                QueriedFileCount: 0,
                ReusedFromCacheCount: reused.Count);
        }

        var scanned = new List<ScannedFile>(survey.Candidates.Count);
        scanned.AddRange(reused);
        var errors = new List<string>();
        var completed = 0;
        foreach (var batch in toQuery.Chunk(QueryBatchSize))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var probes = await Task.WhenAll(batch.Select(candidate =>
                ReadFileAsync(candidate, cancellationToken)));

            foreach (var (candidate, probe, error) in probes)
            {
                if (error is not null)
                {
                    errors.Add(error);
                    continue;
                }

                scanned.Add(new ScannedFile(
                    CreateEntry(account, libraryRoot, probe!, candidate.LocalPath),
                    probe!.File.Path,
                    DateTimeOffset.UtcNow,
                    candidate.LastWriteTimeUtc));
            }

            completed += batch.Length;
            progress?.Report(new ScanProgress(completed, toQuery.Count));
        }

        return new ScanResult(
            scanned,
            errors,
            QueriedFileCount: toQuery.Count,
            ReusedFromCacheCount: reused.Count);
    }

    /// <summary>
    /// Indexes the cache by local path, which is what the survey produces. The cache is keyed
    /// by item id, and two entries can name the same local path once a file has been replaced,
    /// so the most recently queried one wins.
    /// </summary>
    private static Dictionary<string, CachedFileVersions> BuildCacheIndex(VersionCacheOptions cache) =>
        cache.Entries.Values
            .Where(entry => !string.IsNullOrEmpty(entry.LocalPath))
            .GroupBy(entry => entry.LocalPath, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.MaxBy(entry => entry.QueriedAtUtc)!,
                StringComparer.OrdinalIgnoreCase);

    private static FileEntry ToFileEntry(CachedFileVersions cached) => new()
    {
        DriveId = cached.DriveId,
        ItemId = cached.ItemId,
        Name = cached.Name,
        CloudPath = cached.CloudPath,
        LocalPath = cached.LocalPath,
        WebUrl = cached.WebUrl,
        CurrentSize = cached.CurrentSize,
        LastModified = cached.FileLastModified,
        FileType = Path.GetExtension(cached.Name).TrimStart('.'),
        Versions = cached.Versions,
    };

    /// <summary>Captures a scanned file so a later run can reuse it.</summary>
    public static CachedFileVersions ToCacheEntry(ScannedFile file) => new()
    {
        DriveId = file.Entry.DriveId,
        ItemId = file.Entry.ItemId,
        CloudPath = file.Entry.CloudPath,
        LocalPath = file.Entry.LocalPath,
        ServerRelativePath = file.ServerRelativePath,
        Name = file.Entry.Name,
        WebUrl = file.Entry.WebUrl,
        QueriedAtUtc = file.QueriedAtUtc.ToUniversalTime(),
        FileLastModified = file.Entry.LastModified,
        LocalLastWriteUtc = file.LocalLastWriteUtc,
        CurrentSize = file.Entry.CurrentSize,
        Versions = file.Entry.Versions,
    };

    /// <summary>
    /// One unreadable file must not abort the scan, so its failure is collected and the
    /// remaining files continue (NFR-REL-003). A file that exists locally but not on the
    /// service arrives here as a failed read, which is exactly what it is.
    /// </summary>
    private async Task<(LocalCandidateFile Candidate, SharePointFileProbe? Probe, string? Error)> ReadFileAsync(
        LocalCandidateFile candidate,
        CancellationToken cancellationToken)
    {
        try
        {
            var probe = await session.GetFileVersionsAsync(
                ToServerRelativePath(candidate.CloudRelativePath),
                Guid.Empty,
                cancellationToken);
            return (candidate, probe, null);
        }
        catch (Exception exception) when (exception is InvalidOperationException or
            InvalidDataException or JsonException or TimeoutException)
        {
            return (candidate, null, $"{candidate.CloudRelativePath}: {exception.Message}");
        }
    }

    private string ToServerRelativePath(string cloudRelativePath)
    {
        var root = libraryRoot.TrimEnd('/');
        return cloudRelativePath.Length == 0 ? root : $"{root}/{cloudRelativePath}";
    }

    /// <summary>
    /// Builds the domain entry for a probed file. The current version is synthesised from
    /// the file itself, because SharePoint's version collection holds only historical ones.
    /// </summary>
    /// <remarks>
    /// <paramref name="localPath"/> is the path the local walk actually observed. It is passed
    /// in rather than rebuilt from the server path because the version cache is keyed by it: a
    /// key written by one path builder and looked up by another only has to normalise
    /// differently once for every lookup to miss, which reads as a cold cache rather than as
    /// the fault it is.
    /// </remarks>
    public static FileEntry CreateEntry(
        OneDriveBusinessAccount account,
        string libraryRoot,
        SharePointFileProbe probe,
        string localPath)
    {
        ArgumentNullException.ThrowIfNull(account);
        ArgumentNullException.ThrowIfNull(probe);

        var cloudPath = ToCloudRelativePath(libraryRoot, probe.File.Path);
        var currentLabel = string.IsNullOrWhiteSpace(probe.File.UIVersionLabel)
            ? $"{probe.File.MajorVersion}.{probe.File.MinorVersion}"
            : probe.File.UIVersionLabel;

        var versions = probe.Versions
            .Select(version => new VersionEntry
            {
                VersionId = version.ID.ToString(CultureInfo.InvariantCulture),
                Label = version.VersionLabel,
                Created = version.Created,
                ModifiedBy = version.CreatedBy?.Title,
                Size = version.Length ?? version.Size,
                IsCurrent = version.IsCurrentVersion || string.Equals(
                    version.VersionLabel,
                    currentLabel,
                    StringComparison.OrdinalIgnoreCase),
                CanDelete = null,
            })
            .ToList();

        if (versions.All(version => !version.IsCurrent))
        {
            versions.Add(new VersionEntry
            {
                VersionId = "current",
                Label = currentLabel,
                Created = probe.File.TimeLastModified,
                Size = probe.File.Length,
                IsCurrent = true,
                CanDelete = false,
            });
        }
        else
        {
            for (var index = 0; index < versions.Count; index++)
            {
                if (versions[index].IsCurrent)
                {
                    versions[index] = versions[index] with { CanDelete = false };
                }
            }
        }

        var authority = account.SiteUri.GetLeftPart(UriPartial.Authority);
        return new FileEntry
        {
            DriveId = account.AccountKey,
            ItemId = probe.File.UniqueId == Guid.Empty
                ? probe.File.Path
                : probe.File.UniqueId.ToString("D"),
            Name = probe.File.Name,
            CloudPath = cloudPath,
            LocalPath = localPath,
            WebUrl = Uri.TryCreate(authority + probe.File.Path, UriKind.Absolute, out var webUrl)
                ? webUrl.AbsoluteUri
                : account.SiteUri.AbsoluteUri,
            CurrentSize = probe.File.Length,
            LastModified = probe.File.TimeLastModified,
            FileType = Path.GetExtension(probe.File.Name).TrimStart('.'),

            // A file whose history could not be read must never look like a file without
            // history: the retention evaluator keeps everything for a failed retrieval.
            RetrievalStatus = probe.Error is null ? RetrievalStatus.Success : RetrievalStatus.Error,
            Error = probe.Error,
            Versions = versions,
        };
    }

    private static string ToCloudRelativePath(string libraryRoot, string serverRelativePath)
    {
        var root = libraryRoot.TrimEnd('/');
        return serverRelativePath.StartsWith(root + "/", StringComparison.OrdinalIgnoreCase)
            ? serverRelativePath[(root.Length + 1)..]
            : serverRelativePath.TrimStart('/');
    }
}
