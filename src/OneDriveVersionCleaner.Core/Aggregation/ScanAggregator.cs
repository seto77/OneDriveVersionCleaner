using OneDriveVersionCleaner.Core.Models;
using OneDriveVersionCleaner.Core.Retention;

namespace OneDriveVersionCleaner.Core.Aggregation;

public sealed class ScanAggregator
{
    public ScanAggregate Aggregate(
        IEnumerable<FileEntry> files,
        RetentionEvaluationResult? retentionResult = null,
        TimeSpan? scanDuration = null)
    {
        ArgumentNullException.ThrowIfNull(files);

        if (scanDuration < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(scanDuration));
        }

        var materializedFiles = files.ToArray();
        if (materializedFiles.Any(file => file is null))
        {
            throw new ArgumentException("The file sequence cannot contain null entries.", nameof(files));
        }

        var candidateKeys = retentionResult?.DeleteCandidates
            .Select(decision => VersionKey.From(decision.File, decision.Version))
            .ToHashSet() ?? [];

        var fileAggregates = materializedFiles
            .Select(file => AggregateFile(file!, candidateKeys))
            .ToArray();

        var folders = AggregateFolders(fileAggregates);
        var summary = new ScanSummary(
            TargetFileCount: fileAggregates.Length,
            FilesWithPreviousVersions: fileAggregates.Count(file => file.PreviousVersionCount > 0),
            PreviousVersionCount: CheckedSum(fileAggregates, file => file.PreviousVersionCount),
            KnownPreviousVersionBytes: CheckedSum(fileAggregates, file => file.KnownPreviousVersionBytes),
            UnknownPreviousVersionSizeCount: CheckedSum(fileAggregates, file => file.UnknownPreviousVersionSizeCount),
            DeleteCandidateCount: CheckedSum(fileAggregates, file => file.DeleteCandidateCount),
            KnownReclaimableBytes: CheckedSum(fileAggregates, file => file.KnownReclaimableBytes),
            UnknownDeleteCandidateSizeCount: CheckedSum(fileAggregates, file => file.UnknownDeleteCandidateSizeCount),
            ErrorCount: CheckedSum(fileAggregates, file => file.ErrorCount),
            ScanDuration: scanDuration);

        return new ScanAggregate(fileAggregates, folders, summary);
    }

    private static FileScanAggregate AggregateFile(
        FileEntry file,
        IReadOnlySet<VersionKey> candidateKeys)
    {
        var previousVersions = file.Versions.Where(version => !version.IsCurrent).ToArray();
        var knownPreviousBytes = previousVersions
            .Where(version => version.Size is >= 0)
            .Aggregate(0L, (total, version) => checked(total + version.Size!.Value));
        var unknownPreviousCount = previousVersions.Count(version => version.Size is not >= 0);
        var exactPreviousBytes = unknownPreviousCount == 0 ? knownPreviousBytes : (long?)null;

        var currentSize = file.CurrentSize is >= 0 ? file.CurrentSize : null;
        var estimatedAllVersionsBytes = currentSize.HasValue && exactPreviousBytes.HasValue
            ? checked(currentSize.Value + exactPreviousBytes.Value)
            : (long?)null;

        var candidates = previousVersions
            .Where(version => candidateKeys.Contains(VersionKey.From(file, version)))
            .ToArray();
        var knownReclaimableBytes = candidates
            .Where(version => version.Size is >= 0)
            .Aggregate(0L, (total, version) => checked(total + version.Size!.Value));
        var unknownCandidateCount = candidates.Count(version => version.Size is not >= 0);

        var knownDates = previousVersions
            .Where(version => version.Created.HasValue)
            .Select(version => version.Created!.Value)
            .ToArray();

        var errorCount = (IsError(file.RetrievalStatus) ? 1 : 0) +
            previousVersions.Count(version => IsError(version.RetrievalStatus));

        return new FileScanAggregate(
            File: file,
            CurrentSizeBytes: currentSize,
            PreviousVersionCount: previousVersions.Length,
            KnownPreviousVersionBytes: knownPreviousBytes,
            UnknownPreviousVersionSizeCount: unknownPreviousCount,
            PreviousVersionTotalBytes: exactPreviousBytes,
            EstimatedAllVersionsTotalBytes: estimatedAllVersionsBytes,
            DeleteCandidateCount: candidates.Length,
            KnownReclaimableBytes: knownReclaimableBytes,
            UnknownDeleteCandidateSizeCount: unknownCandidateCount,
            OldestPreviousVersionCreated: knownDates.Length == 0 ? null : knownDates.Min(),
            LatestPreviousVersionCreated: knownDates.Length == 0 ? null : knownDates.Max(),
            ErrorCount: errorCount);
    }

    private static IReadOnlyList<FolderScanAggregate> AggregateFolders(
        IReadOnlyCollection<FileScanAggregate> files)
    {
        var buckets = new Dictionary<string, List<FileScanAggregate>>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in files)
        {
            foreach (var folderPath in GetAncestorFolderPaths(file.File.CloudPath))
            {
                if (!buckets.TryGetValue(folderPath, out var bucket))
                {
                    bucket = [];
                    buckets.Add(folderPath, bucket);
                }

                bucket.Add(file);
            }
        }

        return buckets
            .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .Select(pair => new FolderScanAggregate(
                CloudFolderPath: pair.Key,
                FileCount: pair.Value.Count,
                FilesWithPreviousVersions: pair.Value.Count(file => file.PreviousVersionCount > 0),
                PreviousVersionCount: CheckedSum(pair.Value, file => file.PreviousVersionCount),
                KnownPreviousVersionBytes: CheckedSum(pair.Value, file => file.KnownPreviousVersionBytes),
                UnknownPreviousVersionSizeCount: CheckedSum(pair.Value, file => file.UnknownPreviousVersionSizeCount),
                DeleteCandidateCount: CheckedSum(pair.Value, file => file.DeleteCandidateCount),
                KnownReclaimableBytes: CheckedSum(pair.Value, file => file.KnownReclaimableBytes),
                UnknownDeleteCandidateSizeCount: CheckedSum(pair.Value, file => file.UnknownDeleteCandidateSizeCount),
                ErrorCount: CheckedSum(pair.Value, file => file.ErrorCount)))
            .ToArray();
    }

    private static IEnumerable<string> GetAncestorFolderPaths(string cloudPath)
    {
        yield return string.Empty;

        var segments = cloudPath
            .Replace('\\', '/')
            .Split('/', StringSplitOptions.RemoveEmptyEntries);

        for (var index = 1; index < segments.Length; index++)
        {
            yield return string.Join('/', segments.Take(index));
        }
    }

    private static bool IsError(RetrievalStatus status) =>
        status is RetrievalStatus.AccessDenied or
            RetrievalStatus.NotFound or
            RetrievalStatus.Error;

    private static int CheckedSum<T>(IEnumerable<T> values, Func<T, int> selector) =>
        values.Aggregate(0, (total, value) => checked(total + selector(value)));

    private static long CheckedSum<T>(IEnumerable<T> values, Func<T, long> selector) =>
        values.Aggregate(0L, (total, value) => checked(total + selector(value)));

    private readonly record struct VersionKey(string DriveId, string ItemId, string VersionId)
    {
        public static VersionKey From(FileEntry file, VersionEntry version) =>
            new(file.DriveId, file.ItemId, version.VersionId);
    }
}
