using OneDriveVersionCleaner.Core.Aggregation;
using OneDriveVersionCleaner.Core.Models;
using OneDriveVersionCleaner.Core.Retention;

namespace OneDriveVersionCleaner.Core.Tests;

public sealed class ScanAggregatorTests
{
    private readonly ScanAggregator _sut = new();

    [Fact]
    public void Aggregate_CalculatesFileAndOverallTotalsWithoutDoubleCountingCurrent()
    {
        var current = TestData.Version("current", 0, size: 1_000, isCurrent: true);
        var file = TestData.File(
            current,
            TestData.Version("v1", 1, size: 10),
            TestData.Version("v2", 2, size: 20),
            TestData.Version("v3", 3, size: 30));

        var result = _sut.Aggregate([file], scanDuration: TimeSpan.FromSeconds(2));

        var item = Assert.Single(result.Files);
        Assert.Equal(3, item.PreviousVersionCount);
        Assert.Equal(60, item.KnownPreviousVersionBytes);
        Assert.Equal(60, item.PreviousVersionTotalBytes);
        Assert.Equal(1_060, item.EstimatedAllVersionsTotalBytes);
        Assert.Equal(3, result.Summary.PreviousVersionCount);
        Assert.Equal(60, result.Summary.KnownPreviousVersionBytes);
        Assert.Equal(TimeSpan.FromSeconds(2), result.Summary.ScanDuration);
    }

    [Fact]
    public void Aggregate_RepresentsUnknownHistoricalSizeAsPartialNotZero()
    {
        var file = TestData.File(
            TestData.Version("known", 1, size: 20),
            TestData.Version("unknown", 2, size: null));

        var result = _sut.Aggregate([file]);

        var item = Assert.Single(result.Files);
        Assert.Equal(20, item.KnownPreviousVersionBytes);
        Assert.Equal(1, item.UnknownPreviousVersionSizeCount);
        Assert.Null(item.PreviousVersionTotalBytes);
        Assert.Null(item.EstimatedAllVersionsTotalBytes);
        Assert.False(item.IsHistorySizeComplete);
        Assert.False(result.Summary.IsHistorySizeComplete);
        Assert.Null(file.PreviousVersionTotalSize);
    }

    [Fact]
    public void Aggregate_TreatsNegativeApiSizeAsUnknown()
    {
        var file = TestData.File(TestData.Version("invalid", 1, size: -1));

        var item = Assert.Single(_sut.Aggregate([file]).Files);

        Assert.Equal(0, item.KnownPreviousVersionBytes);
        Assert.Equal(1, item.UnknownPreviousVersionSizeCount);
        Assert.Null(item.PreviousVersionTotalBytes);
    }

    [Fact]
    public void Aggregate_CalculatesCandidateCountAndReclaimableBytes()
    {
        var file = TestData.File(
            TestData.Version("old", 100, size: 300),
            TestData.Version("new", 1, size: 200));
        var retention = new RetentionPolicyEvaluator().Evaluate(
            file,
            TestData.Policy(keepLatest: 1),
            TestData.Now);

        var item = Assert.Single(_sut.Aggregate([file], retention).Files);

        Assert.Equal(1, item.DeleteCandidateCount);
        Assert.Equal(300, item.KnownReclaimableBytes);
        Assert.Equal(0, item.UnknownDeleteCandidateSizeCount);
    }

    [Fact]
    public void Aggregate_FindsOldestAndLatestKnownPastVersionDates()
    {
        var file = TestData.File(
            TestData.Version("old", 100),
            new VersionEntry { VersionId = "unknown", Size = 10 },
            TestData.Version("new", 2),
            TestData.Version("current", 0, isCurrent: true));

        var item = Assert.Single(_sut.Aggregate([file]).Files);

        Assert.Equal(TestData.Now.AddDays(-100), item.OldestPreviousVersionCreated);
        Assert.Equal(TestData.Now.AddDays(-2), item.LatestPreviousVersionCreated);
    }

    [Fact]
    public void Aggregate_CreatesRecursiveFolderRollups()
    {
        var first = TestData.File(TestData.Version("a", 1)) with
        {
            ItemId = "a",
            CloudPath = "A/a.txt",
        };
        var second = TestData.File(TestData.Version("b", 1)) with
        {
            ItemId = "b",
            CloudPath = "A/B/b.txt",
        };
        var third = TestData.File() with
        {
            ItemId = "c",
            CloudPath = "X/c.txt",
        };

        var folders = _sut.Aggregate([first, second, third]).Folders;

        Assert.Equal(3, folders.Single(folder => folder.CloudFolderPath == string.Empty).FileCount);
        Assert.Equal(2, folders.Single(folder => folder.CloudFolderPath == "A").FileCount);
        Assert.Equal(1, folders.Single(folder => folder.CloudFolderPath == "A/B").FileCount);
        Assert.Equal(1, folders.Single(folder => folder.CloudFolderPath == "X").FileCount);
    }

    [Fact]
    public void Aggregate_CountsErrorsWithoutTreatingWarningsAsErrors()
    {
        var errorFile = TestData.File(
            TestData.Version("error", 1) with { RetrievalStatus = RetrievalStatus.Error }) with
        {
            RetrievalStatus = RetrievalStatus.AccessDenied,
        };
        var warningFile = TestData.File() with
        {
            ItemId = "warning",
            RetrievalStatus = RetrievalStatus.Warning,
        };

        var result = _sut.Aggregate([errorFile, warningFile]);

        Assert.Equal(2, result.Summary.ErrorCount);
    }

    [Fact]
    public void Aggregate_EmptyScanReturnsZeros()
    {
        var result = _sut.Aggregate([]);

        Assert.Empty(result.Files);
        Assert.Empty(result.Folders);
        Assert.Equal(0, result.Summary.TargetFileCount);
        Assert.Equal(0, result.Summary.KnownPreviousVersionBytes);
    }

    [Fact]
    public void Aggregate_NegativeDurationIsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _sut.Aggregate([], scanDuration: TimeSpan.FromTicks(-1)));
    }
}
