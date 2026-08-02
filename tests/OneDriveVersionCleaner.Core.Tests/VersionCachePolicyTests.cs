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
        var modified = TestData.Now.AddDays(-1);
        var cached = Cached(queriedDaysAgo: 3, modified);

        // Changed since the query, but asking again this soon is what got us throttled.
        Assert.False(VersionCachePolicy.ShouldRequery(
            cached,
            modified.AddHours(1),
            TestData.Now,
            TenDays));
    }

    [Fact]
    public void ShouldRequery_IsFalseForAnOldAnswerWhenTheFileNeverChanged()
    {
        var modified = TestData.Now.AddDays(-400);
        var cached = Cached(queriedDaysAgo: 300, modified);

        // History only grows when the file is edited, so an untouched file needs no request.
        Assert.False(VersionCachePolicy.ShouldRequery(cached, modified, TestData.Now, TenDays));
    }

    [Fact]
    public void ShouldRequery_IsTrueOnceTheAnswerIsOldAndTheFileHasChanged()
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

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void HasChangedSince_TreatsAMissingTimestampAsAChange(
        bool cachedMissing,
        bool currentMissing)
    {
        var cached = Cached(
            queriedDaysAgo: 30,
            cachedMissing ? null : TestData.Now.AddDays(-20));

        Assert.True(VersionCachePolicy.HasChangedSince(
            cached,
            currentMissing ? null : TestData.Now.AddDays(-20)));
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

    private static CachedFileVersions Cached(int queriedDaysAgo, DateTimeOffset? modified) => new()
    {
        DriveId = "drive-1",
        ItemId = "item-1",
        QueriedAtUtc = TestData.Now.AddDays(-queriedDaysAgo),
        FileLastModified = modified,
    };
}
