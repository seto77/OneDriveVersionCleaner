using OneDriveVersionCleaner.Core.Deletion;
using OneDriveVersionCleaner.Core.Models;

namespace OneDriveVersionCleaner.Core.Tests;

public sealed class VersionDeletionPlannerTests
{
    private readonly VersionDeletionPlanner _sut = new();

    [Fact]
    public void Create_GroupsTheSelectionByFile()
    {
        var file = TestData.File(TestData.Version("a", 400), TestData.Version("b", 500));

        var plan = _sut.Create(Selection(file));

        var planned = Assert.Single(plan.Files);
        Assert.Equal(2, planned.VersionCount);
        Assert.Equal(2, plan.VersionCount);
        Assert.False(plan.IsEmpty);
    }

    [Fact]
    public void Create_OrdersVersionsOldestFirstSoAnInterruptedRunKeepsTheNewestHistory()
    {
        var file = TestData.File(
            TestData.Version("middle", 200),
            TestData.Version("oldest", 900),
            TestData.Version("newest", 100));

        var plan = _sut.Create(Selection(file));

        Assert.Equal(
            ["oldest", "middle", "newest"],
            plan.Files.Single().Versions.Select(version => version.VersionId));
    }

    [Fact]
    public void Create_PutsVersionsWithoutACreationDateLast()
    {
        var file = TestData.File(
            TestData.Version("undated", 500) with { Created = null },
            TestData.Version("older", 900));

        var plan = _sut.Create(Selection(file));

        Assert.Equal(
            ["older", "undated"],
            plan.Files.Single().Versions.Select(version => version.VersionId));
    }

    [Fact]
    public void Create_IgnoresARepeatedSelectionOfTheSameVersion()
    {
        var file = TestData.File(TestData.Version("a", 400));
        var duplicated = Selection(file).Concat(Selection(file));

        Assert.Equal(1, _sut.Create(duplicated).VersionCount);
    }

    [Fact]
    public void Create_PlansEveryVersionThatWasSelected()
    {
        var file = TestData.File(
            TestData.Version("small", 400, size: 10),
            TestData.Version("large", 500, size: 900),
            TestData.Version("unknown", 600, size: null));

        // Nothing is trimmed: the selection is the user's, and the confirmation dialog states
        // its size before anything is touched.
        Assert.Equal(3, _sut.Create(Selection(file)).VersionCount);
    }

    [Fact]
    public void Create_OrdersFilesByCloudPath()
    {
        var second = TestData.File(TestData.Version("b", 400)) with
        {
            ItemId = "item-b",
            CloudPath = "Research/zeta.xlsx",
        };
        var first = TestData.File(TestData.Version("a", 400)) with
        {
            ItemId = "item-a",
            CloudPath = "Research/alpha.xlsx",
        };

        var plan = _sut.Create([.. Selection(second), .. Selection(first)]);

        Assert.Equal(
            ["Research/alpha.xlsx", "Research/zeta.xlsx"],
            plan.Files.Select(file => file.File.CloudPath));
    }

    [Fact]
    public void Create_ReturnsAnEmptyPlanForAnEmptySelection()
    {
        var plan = _sut.Create([]);

        Assert.True(plan.IsEmpty);
        Assert.Empty(plan.Files);
        Assert.Equal(0, plan.KnownReclaimableBytes);
    }

    [Fact]
    public void Create_SumsKnownSizesAndCountsUnknownOnesSeparately()
    {
        var file = TestData.File(
            TestData.Version("a", 400, size: 300),
            TestData.Version("b", 500, size: 700),
            TestData.Version("c", 600, size: null));

        var plan = _sut.Create(Selection(file));

        Assert.Equal(1_000, plan.KnownReclaimableBytes);
        Assert.Equal(1, plan.UnknownSizeCount);
    }

    [Fact]
    public void Create_RefusesASelectionThatContainsTheCurrentVersion()
    {
        var file = TestData.File(TestData.Version("current", 0, isCurrent: true));

        var exception = Assert.Throws<InvalidOperationException>(
            () => _sut.Create(Selection(file)));

        Assert.Contains("current version", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_RefusesASelectionThatContainsAVersionTheApiMarkedUndeletable()
    {
        var file = TestData.File(TestData.Version("locked", 400, canDelete: false));

        Assert.Throws<InvalidOperationException>(() => _sut.Create(Selection(file)));
    }

    private static PlannedVersionDeletion[] Selection(FileEntry file) =>
        file.Versions.Select(version => new PlannedVersionDeletion(file, version)).ToArray();
}
