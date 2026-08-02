using OneDriveVersionCleaner.Core.Caching;

namespace OneDriveVersionCleaner.Core.Tests;

public sealed class VersionCachePolicyTests
{
    private static readonly TimeSpan TenDays = TimeSpan.FromDays(10);

    [Fact]
    public void ShouldRequery_IsTrueWhenTheFileWasNeverQueried()
    {
        Assert.True(VersionCachePolicy.ShouldRequery(null, TestData.Now, TestData.Now, TenDays));
    }

    [Fact]
    public void ShouldRequery_IsFalseWhileTheCachedAnswerIsStillYoung()
    {
        var written = TestData.Now.AddDays(-1);
        var cached = Cached(queriedDaysAgo: 3, written);

        // Changed since the query, but asking again this soon is what got us throttled.
        Assert.False(VersionCachePolicy.ShouldRequery(
            cached,
            written.AddHours(1),
            TestData.Now,
            TenDays));
    }

    [Fact]
    public void ShouldRequery_IsFalseForAnOldAnswerWhenTheLocalCopyNeverMoved()
    {
        var written = TestData.Now.AddDays(-400);
        var cached = Cached(queriedDaysAgo: 300, written);

        // History only grows when the file is edited, so an untouched file needs no request.
        Assert.False(VersionCachePolicy.ShouldRequery(cached, written, TestData.Now, TenDays));
    }

    [Fact]
    public void ShouldRequery_IsTrueOnceTheAnswerIsOldAndTheLocalCopyChanged()
    {
        var cached = Cached(queriedDaysAgo: 11, TestData.Now.AddDays(-20));

        Assert.True(VersionCachePolicy.ShouldRequery(
            cached,
            TestData.Now.AddDays(-1),
            TestData.Now,
            TenDays));
    }

    [Fact]
    public void ShouldRequery_TreatsTheAgeBoundaryAsOldEnough()
    {
        var cached = Cached(queriedDaysAgo: 10, TestData.Now.AddDays(-20));

        Assert.True(VersionCachePolicy.ShouldRequery(
            cached,
            TestData.Now.AddDays(-1),
            TestData.Now,
            TenDays));
    }

    [Fact]
    public void ShouldRequery_IsTrueForAChangedFileWhenNoAgeIsRequired()
    {
        var cached = Cached(queriedDaysAgo: 0, TestData.Now.AddDays(-2));

        Assert.True(VersionCachePolicy.ShouldRequery(
            cached,
            TestData.Now,
            TestData.Now,
            TimeSpan.Zero));
    }

    [Fact]
    public void ShouldRequery_IsTrueForAnEntryThatCarriesNoLocalTimestamp()
    {
        // Written before the scan became local-first. There is nothing to compare against, so
        // the file is read once more and then carries a timestamp like every other entry.
        var cached = new CachedFileVersions
        {
            DriveId = "drive-1",
            ItemId = "item-1",
            QueriedAtUtc = TestData.Now.AddDays(-300),
        };

        Assert.True(VersionCachePolicy.ShouldRequery(
            cached,
            TestData.Now.AddDays(-400),
            TestData.Now,
            TenDays));
    }

    [Fact]
    public void ShouldRequery_RejectsANegativeAge()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => VersionCachePolicy.ShouldRequery(
            null,
            TestData.Now,
            TestData.Now,
            TimeSpan.FromDays(-1)));
    }

    private static CachedFileVersions Cached(
        int queriedDaysAgo,
        DateTimeOffset localLastWrite) => new()
        {
            DriveId = "drive-1",
            ItemId = "item-1",
            QueriedAtUtc = TestData.Now.AddDays(-queriedDaysAgo),
            FileLastModified = localLastWrite,
            LocalLastWriteUtc = localLastWrite,
        };
}
