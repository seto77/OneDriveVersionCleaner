using System.Globalization;
using System.Text.Json;
using OneDriveVersionCleaner.Core.Caching;
using OneDriveVersionCleaner.Core.Deletion;
using OneDriveVersionCleaner.Core.Models;
using OneDriveVersionCleaner.Infrastructure.OneDriveAccounts;

namespace OneDriveVersionCleaner.App;

internal sealed record ScanProgress(
    int FoldersCompleted,
    int FoldersDiscovered,
    int FilesFound,
    string CurrentFolder);

/// <summary>
/// A scanned file together with the address needed to act on it again.
/// </summary>
/// <param name="QueriedAtUtc">
/// When this file's versions were read from the service. Carried over unchanged for a reused
/// answer, so reusing it does not keep resetting its age and postponing the next refresh.
/// </param>
internal sealed record ScannedFile(
    FileEntry Entry,
    string ServerRelativePath,
    DateTimeOffset QueriedAtUtc);

internal sealed record ScanResult(
    IReadOnlyList<ScannedFile> Files,
    IReadOnlyList<string> FolderErrors,
    bool UsedBulkListing = false,
    int DetailedFileCount = 0,
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
/// Asked before the per-file phase starts, with the number of files that would be read one by
/// one. Returning false ends the scan with what the bulk phase already produced.
/// </summary>
internal delegate bool ConfirmDetailPhase(int fileCount, long thresholdBytes);

/// <summary>
/// Walks the personal document library breadth first. Every folder on a level is read
/// concurrently; the session's request gate bounds how many requests are actually in flight.
/// </summary>
internal sealed class OneDriveScanner(
    SharePointWebSession session,
    OneDriveBusinessAccount account,
    string libraryRoot,
    SharePointVersioningInfo versioning)
{
    /// <summary>
    /// SharePoint keeps list forms in a folder of this name. It holds no user content.
    /// </summary>
    private const string SystemFolderName = "Forms";

    /// <summary>How many per-file reads are started at once during the detail phase.</summary>
    private const int DetailBatchSize = 32;

    /// <summary>
    /// Scans by reading the library's item list once and then asking for version detail only
    /// where the reported history is large enough to matter. Falls back to walking folders
    /// when the item list is unavailable.
    /// </summary>
    public async Task<ScanResult> ScanBulkAsync(
        string rootServerRelativePath,
        long minimumHistoryBytes,
        long minimumCurrentFileBytes,
        VersionCacheOptions cache,
        ConfirmDetailPhase confirmDetailPhase,
        IProgress<ScanProgress>? progress,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootServerRelativePath);
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(confirmDetailPhase);

        var listProgress = new Progress<int>(count =>
            progress?.Report(new ScanProgress(0, 0, count, "ライブラリ項目を取得しています")));
        var items = await session.TryEnumerateLibraryItemsAsync(listProgress, cancellationToken);
        if (items is null)
        {
            return await ScanAsync(
                new SharePointFolderInfo { ServerRelativeUrl = rootServerRelativePath },
                progress,
                cancellationToken);
        }

        var prefix = rootServerRelativePath.TrimEnd('/') + "/";
        var files = items
            .Where(item => item.IsFile &&
                item.FileRef.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        // Only files that can hold history, are large enough to be worth reclaiming, and whose
        // reported history clears the threshold, are worth a request each. An unreported total
        // counts as "unknown", never as "none".
        var inScope = files
            .Where(item => HasHistory(item) &&
                ClearsFileSize(item, minimumCurrentFileBytes) &&
                (item.EstimatedHistoryBytes is not { } bytes || bytes >= minimumHistoryBytes))
            .ToArray();

        // Anything already answered for stays out of the request budget until the file both
        // changes and the answer has aged past the configured interval.
        var reused = new List<ScannedFile>();
        var detailed = new List<SharePointListItemInfo>(inScope.Length);
        foreach (var item in inScope)
        {
            var cached = FindCached(cache, item);
            if (VersionCachePolicy.ShouldRequery(cached, item.Modified, DateTimeOffset.UtcNow, cache.MinimumAge))
            {
                detailed.Add(item);
            }
            else
            {
                reused.Add(new ScannedFile(
                    ToFileEntry(cached!),
                    cached!.ServerRelativePath,
                    cached.QueriedAtUtc));
            }
        }

        if (detailed.Count > 0 && !confirmDetailPhase(detailed.Count, minimumHistoryBytes))
        {
            return new ScanResult(
                reused,
                [],
                UsedBulkListing: true,
                DetailedFileCount: 0,
                ReusedFromCacheCount: reused.Count);
        }

        var scanned = new List<ScannedFile>(detailed.Count + reused.Count);
        scanned.AddRange(reused);
        var errors = new List<string>();
        var completed = 0;
        foreach (var batch in detailed.Chunk(DetailBatchSize))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var probes = await Task.WhenAll(batch.Select(item =>
                ReadFileAsync(item, cancellationToken)));

            foreach (var (probe, error) in probes)
            {
                if (error is not null)
                {
                    errors.Add(error);
                    continue;
                }

                scanned.Add(new ScannedFile(
                    CreateEntry(account, libraryRoot, probe!),
                    probe!.File.Path,
                    DateTimeOffset.UtcNow));
            }

            completed += batch.Length;
            progress?.Report(new ScanProgress(
                completed,
                detailed.Count,
                scanned.Count,
                "バージョン履歴を取得しています"));
        }

        return new ScanResult(
            scanned,
            errors,
            UsedBulkListing: true,
            DetailedFileCount: detailed.Count,
            ReusedFromCacheCount: reused.Count);
    }

    /// <summary>
    /// Finds the earlier answer for a listed item. The cache is keyed by item id, but the bulk
    /// listing does not carry one, so the server-relative path is what ties the two together.
    /// </summary>
    private static CachedFileVersions? FindCached(
        VersionCacheOptions cache,
        SharePointListItemInfo item) => cache.Entries.Values.FirstOrDefault(entry =>
        string.Equals(entry.ServerRelativePath, item.FileRef, StringComparison.OrdinalIgnoreCase));

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
        CurrentSize = file.Entry.CurrentSize,
        Versions = file.Entry.Versions,
    };

    /// <summary>
    /// True when the file is large enough to be worth asking about. The size comes from the
    /// local sync folder first: reading it costs nothing and does not touch the service.
    /// Placeholders of online-only files still report their real length. A size that cannot be
    /// established anywhere keeps the file in scope rather than dropping it silently.
    /// </summary>
    private bool ClearsFileSize(SharePointListItemInfo item, long minimumBytes)
    {
        if (minimumBytes <= 0)
        {
            return true;
        }

        var localPath = ToLocalPath(account.UserFolder, ToCloudRelativePath(libraryRoot, item.FileRef));
        var localSize = LocalFileFingerprint.Capture(localPath);
        if (localSize is { Exists: true, Length: { } length })
        {
            return length >= minimumBytes;
        }

        return item.FileSize is not { } reported || reported >= minimumBytes;
    }

    /// <summary>
    /// True unless the version label rules out any previous version. <c>1.0</c> is the first
    /// version ever written when minor versions are off; with them on, drafts may precede it.
    /// </summary>
    private bool HasHistory(SharePointListItemInfo item) =>
        versioning.EnableVersioning &&
        (versioning.EnableMinorVersions ||
            !string.Equals(item.UIVersionString, "1.0", StringComparison.Ordinal));

    public async Task<ScanResult> ScanAsync(
        SharePointFolderInfo root,
        IProgress<ScanProgress>? progress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(root);

        var files = new List<ScannedFile>();
        var errors = new List<string>();
        var level = new List<SharePointFolderInfo> { root };
        var discovered = 1;
        var completed = 0;

        while (level.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report(new ScanProgress(completed, discovered, files.Count, level[0].Path));

            var contents = await Task.WhenAll(level.Select(folder =>
                ReadFolderAsync(folder, cancellationToken)));

            var next = new List<SharePointFolderInfo>();
            foreach (var (folder, error) in contents)
            {
                completed++;
                if (error is not null)
                {
                    errors.Add(error);
                    continue;
                }

                files.AddRange(folder!.Files.Select(probe => new ScannedFile(
                    CreateEntry(account, libraryRoot, probe),
                    probe.File.Path,
                    DateTimeOffset.UtcNow)));
                next.AddRange(folder.Folders.Where(IsScannable));
            }

            discovered += next.Count;
            progress?.Report(new ScanProgress(completed, discovered, files.Count, string.Empty));
            level = next;
        }

        return new ScanResult(files, errors);
    }

    /// <summary>
    /// One unreadable file must not abort the detail phase, so its failure is collected and
    /// the remaining files continue (NFR-REL-003).
    /// </summary>
    private async Task<(SharePointFileProbe? Probe, string? Error)> ReadFileAsync(
        SharePointListItemInfo item,
        CancellationToken cancellationToken)
    {
        try
        {
            return (await session.GetFileVersionsAsync(item.FileRef, Guid.Empty, cancellationToken), null);
        }
        catch (Exception exception) when (exception is InvalidOperationException or
            InvalidDataException or JsonException or TimeoutException)
        {
            return (null, $"{item.FileRef}: {exception.Message}");
        }
    }

    /// <summary>
    /// One unreadable folder must not abort the whole library (NFR-REL-003), so failures are
    /// collected and the walk continues with the folders that did respond.
    /// </summary>
    private async Task<(SharePointFolderContents? Contents, string? Error)> ReadFolderAsync(
        SharePointFolderInfo folder,
        CancellationToken cancellationToken)
    {
        try
        {
            return (await session.GetFolderContentsAsync(folder, versioning, cancellationToken), null);
        }
        catch (Exception exception) when (exception is InvalidOperationException or
            InvalidDataException or JsonException or TimeoutException)
        {
            return (null, $"{folder.Path}: {exception.Message}");
        }
    }

    /// <summary>
    /// Builds the domain entry for a probed file. The current version is synthesised from
    /// the file itself, because SharePoint's version collection holds only historical ones.
    /// </summary>
    public static FileEntry CreateEntry(
        OneDriveBusinessAccount account,
        string libraryRoot,
        SharePointFileProbe probe)
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
            LocalPath = ToLocalPath(account.UserFolder, cloudPath),
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

    private static bool IsScannable(SharePointFolderInfo folder) =>
        !string.Equals(folder.Name, SystemFolderName, StringComparison.OrdinalIgnoreCase) &&
        !folder.Name.StartsWith('_') &&
        !string.IsNullOrWhiteSpace(folder.Path);

    private static string ToCloudRelativePath(string libraryRoot, string serverRelativePath)
    {
        var root = libraryRoot.TrimEnd('/');
        return serverRelativePath.StartsWith(root + "/", StringComparison.OrdinalIgnoreCase)
            ? serverRelativePath[(root.Length + 1)..]
            : serverRelativePath.TrimStart('/');
    }

    private static string ToLocalPath(string userFolder, string cloudRelativePath)
    {
        var segments = cloudRelativePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.Length == 0
            ? userFolder
            : Path.Combine([userFolder, .. segments]);
    }
}
