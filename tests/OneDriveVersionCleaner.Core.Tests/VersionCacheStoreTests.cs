using OneDriveVersionCleaner.Core.Caching;

namespace OneDriveVersionCleaner.Core.Tests;

public sealed class VersionCacheStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        "OneDriveVersionCleanerTests",
        Guid.NewGuid().ToString("N"));

    private readonly VersionCacheStore _sut;

    public VersionCacheStoreTests() => _sut = new VersionCacheStore(_directory);

    [Fact]
    public async Task LoadAsync_ReturnsAnEmptyCacheBeforeAnythingWasSaved()
    {
        Assert.Empty(await _sut.LoadAsync("Business1"));
    }

    [Fact]
    public async Task SaveAsync_ThenLoadAsync_RoundTripsTheVersions()
    {
        var entry = Entry("item-1") with
        {
            Versions = [TestData.Version("512", 30, size: 4096)],
        };

        await _sut.SaveAsync("Business1", [entry], TestData.Now);
        var loaded = await _sut.LoadAsync("Business1");

        var restored = loaded["item-1"];
        Assert.Equal("Research/report.xlsx", restored.CloudPath);
        Assert.Equal(entry.QueriedAtUtc, restored.QueriedAtUtc);
        Assert.Equal(entry.FileLastModified, restored.FileLastModified);
        var version = Assert.Single(restored.Versions);
        Assert.Equal("512", version.VersionId);
        Assert.Equal(4096, version.Size);
    }

    [Fact]
    public async Task SaveAsync_ReplacesTheEarlierContentRatherThanAppending()
    {
        await _sut.SaveAsync("Business1", [Entry("item-1"), Entry("item-2")], TestData.Now);
        await _sut.SaveAsync("Business1", [Entry("item-3")], TestData.Now);

        var loaded = await _sut.LoadAsync("Business1");

        Assert.Equal(["item-3"], loaded.Keys);
    }

    [Fact]
    public async Task LoadAsync_KeepsAccountsApart()
    {
        await _sut.SaveAsync("Business1", [Entry("item-1")], TestData.Now);

        Assert.Empty(await _sut.LoadAsync("Business2"));
        Assert.Single(await _sut.LoadAsync("Business1"));
    }

    [Fact]
    public async Task LoadAsync_IgnoresAnUnreadableCacheInsteadOfFailingTheScan()
    {
        var path = _sut.GetCachePath("Business1");
        Directory.CreateDirectory(_directory);
        await File.WriteAllTextAsync(path, "{ this is not json");

        Assert.Empty(await _sut.LoadAsync("Business1"));
    }

    [Fact]
    public async Task LoadAsync_IgnoresACacheWrittenByADifferentSchema()
    {
        await File.WriteAllTextAsync(
            EnsurePath("Business1"),
            """{"schemaVersion":"0.9","savedAtUtc":"2026-08-01T00:00:00+00:00","files":[]}""");

        Assert.Empty(await _sut.LoadAsync("Business1"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    private string EnsurePath(string accountKey)
    {
        Directory.CreateDirectory(_directory);
        return _sut.GetCachePath(accountKey);
    }

    private static CachedFileVersions Entry(string itemId) => new()
    {
        DriveId = "drive-1",
        ItemId = itemId,
        CloudPath = "Research/report.xlsx",
        ServerRelativePath = "/personal/user/Documents/Research/report.xlsx",
        Name = "report.xlsx",
        QueriedAtUtc = TestData.Now.AddDays(-3),
        FileLastModified = TestData.Now.AddDays(-40),
        CurrentSize = 1_000,
    };
}
