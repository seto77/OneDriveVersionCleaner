using OneDriveVersionCleaner.Core.Deletion;
using OneDriveVersionCleaner.Core.Paths;

namespace OneDriveVersionCleaner.Core.Scanning;

/// <summary>
/// One local file large enough to be worth a request. The cloud-relative path is derived from
/// the sync root, so the caller can address the same file on the service without a lookup.
/// </summary>
public sealed record LocalCandidateFile(
    string LocalPath,
    string CloudRelativePath,
    long Length,
    DateTimeOffset LastWriteTimeUtc);

public sealed record LocalSurveyProgress(int FilesExamined, int CandidatesFound);

/// <summary>
/// What the walk found. <paramref name="FilesExamined"/> is the whole population the threshold
/// was applied to, so the caller can show how much the filter actually removed.
/// </summary>
public sealed record LocalSurveyResult(
    IReadOnlyList<LocalCandidateFile> Candidates,
    int FilesExamined,
    IReadOnlyList<string> Errors);

/// <summary>
/// Walks the synced folder and keeps only the files that clear the size threshold.
/// This is what bounds the scan: the walk costs nothing and touches no service, so the number
/// of requests that follow is decided before a single one goes out. Reading a file's length
/// never downloads an online-only file — the size comes from the directory entry.
/// </summary>
public static class LocalFileSurvey
{
    /// <summary>How many files are examined between progress reports.</summary>
    private const int ProgressInterval = 2_000;

    /// <summary>
    /// Surveys <paramref name="scanRootPath"/> for files of at least
    /// <paramref name="minimumBytes"/>.
    /// </summary>
    /// <param name="syncRootPath">
    /// The OneDrive sync root. Cloud-relative paths are measured from here, not from the
    /// scanned folder, so scanning a subfolder still yields paths the service understands.
    /// </param>
    /// <param name="scanRootPath">The folder to walk. Must be at or below the sync root.</param>
    /// <param name="minimumBytes">Files below this are dropped. Zero keeps everything.</param>
    public static LocalSurveyResult Run(
        string syncRootPath,
        string scanRootPath,
        long minimumBytes,
        IProgress<LocalSurveyProgress>? progress,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(syncRootPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(scanRootPath);
        ArgumentOutOfRangeException.ThrowIfNegative(minimumBytes);

        // Throws when the scanned folder is not below the sync root, which would otherwise
        // produce cloud paths that address someone else's library or nothing at all.
        var scanRoot = LocalPathMapper.Map(syncRootPath, scanRootPath).LocalPath;
        var syncRoot = LocalPathMapper.Map(syncRootPath, syncRootPath).LocalPath;

        var candidates = new List<LocalCandidateFile>();
        var errors = new List<string>();
        var examined = 0;
        var nextReport = ProgressInterval;

        var pending = new Stack<string>();
        pending.Push(scanRoot);

        while (pending.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var folder = pending.Pop();

            IReadOnlyList<FileSystemInfo> entries;
            try
            {
                // One sweep for files and folders together: asking for each separately opens a
                // second handle and re-reads every entry of the directory, which doubles the
                // cost of the walk for nothing. The listing is materialised inside the guard
                // because the enumerator is what throws, well after the call that produced it,
                // and a failure belongs to the folder rather than to either half. Lengths and
                // timestamps come from the directory entries the enumerator already carries, so
                // nothing is opened and no online-only file is recalled.
                entries = [.. new DirectoryInfo(folder).EnumerateFileSystemInfos().Where(IsScannable)];
            }
            catch (Exception exception) when (exception is IOException or
                UnauthorizedAccessException or ArgumentException or NotSupportedException)
            {
                errors.Add($"{folder}: {exception.Message}");
                continue;
            }

            foreach (var entry in entries)
            {
                if (entry is DirectoryInfo subfolder)
                {
                    pending.Push(subfolder.FullName);
                    continue;
                }

                if (entry is not FileInfo file)
                {
                    continue;
                }

                examined++;
                if (minimumBytes > 0 && file.Length < minimumBytes)
                {
                    continue;
                }

                candidates.Add(new LocalCandidateFile(
                    file.FullName,
                    LocalPathMapper.ToCloudRelativePath(syncRoot, file.FullName),
                    file.Length,
                    LocalFileFingerprint.ToUtcTimestamp(file.LastWriteTimeUtc)));
            }

            if (examined >= nextReport)
            {
                nextReport = examined + ProgressInterval;
                progress?.Report(new LocalSurveyProgress(examined, candidates.Count));
            }
        }

        progress?.Report(new LocalSurveyProgress(examined, candidates.Count));
        return new LocalSurveyResult(candidates, examined, errors);
    }

    /// <summary>
    /// Excludes what OneDrive keeps for itself — its bookkeeping files and folders are marked
    /// system — and refuses to follow a junction or symlink, which would either leave the sync
    /// root or walk in a circle.
    /// </summary>
    /// <remarks>
    /// A link is identified by <see cref="FileSystemInfo.LinkTarget"/>, never by the
    /// reparse-point attribute. Every folder OneDrive syncs carries that attribute, so
    /// excluding on it skips the entire library while looking like an empty sync folder.
    /// <c>LinkTarget</c> is null for a cloud placeholder and set only for an actual link.
    /// </remarks>
    private static bool IsScannable(FileSystemInfo entry)
    {
        if (entry.Attributes.HasFlag(FileAttributes.System))
        {
            return false;
        }

        return entry is not DirectoryInfo || entry.LinkTarget is null;
    }
}
