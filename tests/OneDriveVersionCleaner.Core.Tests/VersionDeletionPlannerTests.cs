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

        var plan = _sut.Create(Selection(file), 100);

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

        var plan = _sut.Create(Selection(file), 100);

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

        var plan = _sut.Create(Selection(file), 100);

        Assert.Equal(
            ["older", "undated"],
            plan.Files.Single().Versions.Select(version => version.VersionId));
    }

    [Fact]
    public void Create_KeepsTheLargestVersionsWhenTheRunLimitIsReached()
    {
        var file = TestData.File(
            TestData.Version("small", 400, size: 10),
            TestData.Version("large", 500, size: 900),
            TestData.Version("medium", 600, size: 100));

        var plan = _sut.Create(Selection(file), 2);

        Assert.Equal(2, plan.VersionCount);
        Assert.Equal(1, plan.DeferredVersionCount);
        Assert.Equal(
            ["medium", "large"],
            plan.Files.Single().Versions.Select(version => version.VersionId));
    }

    [Fact]
    public void Create_TreatsUnknownSizesAsTheLowestPriorityForTheRunLimit()
    {
        var file = TestData.File(
            TestData.Version("unknown", 400, size: null),
            TestData.Version("known", 500, size: 1));

        var plan = _sut.Create(Selection(file), 1);

        Assert.Equal("known", plan.Files.Single().Versions.Single().VersionId);
    }

    [Fact]
    public void Create_IgnoresARepeatedSelectionOfTheSameVersion()
    {
        var file = TestData.File(TestData.Version("a", 400));
        var duplicated = Selection(file).Concat(Selection(file));

        var plan = _sut.Create(duplicated, 100);

        Assert.Equal(1, plan.VersionCount);
        Assert.Equal(0, plan.DeferredVersionCount);
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

        var plan = _sut.Create([.. Selection(second), .. Selection(first)], 100);

        Assert.Equal(
            ["Research/alpha.xlsx", "Research/zeta.xlsx"],
            plan.Files.Select(file => file.File.CloudPath));
    }

    [Fact]
    public void Create_ReturnsAnEmptyPlanForAnEmptySelection()
    {
        var plan = _sut.Create([], 100);

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

        var plan = _sut.Create(Selection(file), 100);

        Assert.Equal(1_000, plan.KnownReclaimableBytes);
        Assert.Equal(1, plan.UnknownSizeCount);
    }

    [Fact]
    public void Create_RefusesASelectionThatContainsTheCurrentVersion()
    {
        var file = TestData.File(TestData.Version("current", 0, isCurrent: true));

        var exception = Assert.Throws<InvalidOperationException>(
            () => _sut.Create(Selection(file), 100));

        Assert.Contains("current version", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_RefusesASelectionThatContainsAVersionTheApiMarkedUndeletable()
    {
        var file = TestData.File(TestData.Version("locked", 400, canDelete: false));

        Assert.Throws<InvalidOperationException>(() => _sut.Create(Selection(file), 100));
    }

    [Fact]
    public void Create_RejectsANegativeRunLimit()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _sut.Create([], -1));
    }

    private static PlannedVersionDeletion[] Selection(FileEntry file) =>
        file.Versions.Select(version => new PlannedVersionDeletion(file, version)).ToArray();
}
