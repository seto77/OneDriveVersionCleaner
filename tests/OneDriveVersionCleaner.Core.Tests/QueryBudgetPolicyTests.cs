using OneDriveVersionCleaner.Core.Scanning;

namespace OneDriveVersionCleaner.Core.Tests;

public sealed class QueryBudgetPolicyTests
{
    [Fact]
    public void Apply_KeepsEverythingWhenTheLimitIsZero()
    {
        var candidates = Candidates(("a.psd", 300), ("b.psd", 200), ("c.psd", 100));

        var budget = QueryBudgetPolicy.Apply(candidates, 0);

        Assert.Equal(3, budget.Selected.Count);
        Assert.Equal(0, budget.DeferredCount);
    }

    [Fact]
    public void Apply_KeepsEverythingWhenThereAreFewerCandidatesThanTheLimit()
    {
        var candidates = Candidates(("a.psd", 300), ("b.psd", 200));

        var budget = QueryBudgetPolicy.Apply(candidates, 1_000);

        Assert.Equal(2, budget.Selected.Count);
        Assert.Equal(0, budget.DeferredCount);
    }

    [Fact]
    public void Apply_QueriesTheLargestFilesAndDefersTheRest()
    {
        var candidates = Candidates(("small.psd", 100), ("big.psd", 900), ("medium.psd", 500));

        var budget = QueryBudgetPolicy.Apply(candidates, 2);

        // The limit exists to survive the request budget, so it has to be spent where the history
        // worth reclaiming is.
        Assert.Equal(["big.psd", "medium.psd"], budget.Selected.Select(file => file.LocalPath));
        Assert.Equal(1, budget.DeferredCount);
    }

    [Fact]
    public void Apply_OrdersBySizeEvenWhenNothingIsDeferred()
    {
        var candidates = Candidates(("small.psd", 100), ("big.psd", 900));

        // A run cut short by throttling has then still read the files that mattered most.
        var budget = QueryBudgetPolicy.Apply(candidates, 0);

        Assert.Equal(["big.psd", "small.psd"], budget.Selected.Select(file => file.LocalPath));
    }

    [Fact]
    public void Apply_BreaksSizeTiesByPathSoTwoRunsChooseTheSameFiles()
    {
        var candidates = Candidates(("c.psd", 100), ("a.psd", 100), ("b.psd", 100));

        var first = QueryBudgetPolicy.Apply(candidates, 2);
        var second = QueryBudgetPolicy.Apply([.. candidates.Reverse()], 2);

        Assert.Equal(["a.psd", "b.psd"], first.Selected.Select(file => file.LocalPath));
        Assert.Equal(
            first.Selected.Select(file => file.LocalPath),
            second.Selected.Select(file => file.LocalPath));
    }

    [Fact]
    public void Apply_AcceptsAnEmptyCandidateList()
    {
        var budget = QueryBudgetPolicy.Apply([], 1_000);

        Assert.Empty(budget.Selected);
        Assert.Equal(0, budget.DeferredCount);
    }

    [Fact]
    public void Apply_RejectsANegativeLimit()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => QueryBudgetPolicy.Apply(Candidates(("a.psd", 100)), -1));
    }

    private static IReadOnlyList<LocalCandidateFile> Candidates(
        params (string Name, long Length)[] files) =>
        [.. files.Select(file => new LocalCandidateFile(
            file.Name,
            file.Name,
            file.Length,
            TestData.Now))];
}
