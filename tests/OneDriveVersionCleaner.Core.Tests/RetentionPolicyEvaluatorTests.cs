using OneDriveVersionCleaner.Core.Models;
using OneDriveVersionCleaner.Core.Retention;

namespace OneDriveVersionCleaner.Core.Tests;

public sealed class RetentionPolicyEvaluatorTests
{
    private readonly RetentionPolicyEvaluator _sut = new();

    [Fact]
    public void CurrentVersion_IsNeverADeleteCandidate()
    {
        var current = TestData.Version("current", daysAgo: 500, isCurrent: true);

        var result = _sut.Evaluate(TestData.File(current), TestData.Policy(), TestData.Now);

        var decision = Assert.Single(result.Decisions);
        Assert.Equal(RetentionDisposition.Keep, decision.Disposition);
        Assert.Contains(RetentionReason.CurrentVersion, decision.Reasons);
    }

    [Fact]
    public void KeepLatestVersions_CountsPreviousVersionsNotCurrentVersion()
    {
        var versions = Enumerable.Range(1, 11)
            .Select(index => TestData.Version($"v{index}", 20 + index))
            .Prepend(TestData.Version("current", 0, isCurrent: true))
            .ToArray();

        var result = _sut.Evaluate(
            TestData.File(versions),
            TestData.Policy(keepLatest: 10),
            TestData.Now);

        var candidate = Assert.Single(result.DeleteCandidates);
        Assert.Equal("v11", candidate.Version.VersionId);
    }

    [Fact]
    public void CandidateSelection_DoesNotDependOnInputOrder()
    {
        var versions = new[]
        {
            TestData.Version("old", 100),
            TestData.Version("new", 10),
            TestData.Version("middle", 50),
        };

        var policy = TestData.Policy(keepLatest: 2);
        var forward = _sut.Evaluate(TestData.File(versions), policy, TestData.Now);
        var reverse = _sut.Evaluate(TestData.File(versions.Reverse().ToArray()), policy, TestData.Now);

        Assert.Equal(
            forward.DeleteCandidates.Select(item => item.Version.VersionId),
            reverse.DeleteCandidates.Select(item => item.Version.VersionId));
    }

    [Fact]
    public void RetentionCountAndAge_AreCombinedAsKeepOr()
    {
        var newestButOld = TestData.Version("count", 100);
        var recentButOutsideCount = TestData.Version("age", 5);
        var neither = TestData.Version("delete", 200);
        var file = TestData.File(newestButOld, recentButOutsideCount, neither) with
        {
            LastModified = TestData.Now,
        };
        var policy = TestData.Policy(keepLatest: 1, keepDays: 30);

        var result = _sut.Evaluate(file, policy, TestData.Now);

        // "age" is also newest, so use the reasons to prove both independent rules.
        Assert.Contains(
            result.Decisions.Single(item => item.Version.VersionId == "age").Reasons,
            reason => reason == RetentionReason.KeptByAge);
        Assert.Contains(
            result.Decisions.Single(item => item.Version.VersionId == "age").Reasons,
            reason => reason == RetentionReason.KeptByLatestCount);
        Assert.Equal(
            ["count", "delete"],
            result.DeleteCandidates.Select(item => item.Version.VersionId).Order());
    }

    [Fact]
    public void RetentionAge_IncludesExactCutoff()
    {
        var exact = new VersionEntry
        {
            VersionId = "exact",
            Created = TestData.Now.AddDays(-30),
            Size = 1,
        };
        var older = exact with
        {
            VersionId = "older",
            Created = exact.Created!.Value.AddTicks(-1),
        };

        var file = TestData.File(exact, older) with { LastModified = TestData.Now };

        var result = _sut.Evaluate(file, TestData.Policy(keepDays: 30), TestData.Now);

        Assert.Equal(RetentionDisposition.Keep, Decision(result, "exact").Disposition);
        Assert.Equal(RetentionDisposition.DeleteCandidate, Decision(result, "older").Disposition);
    }

    [Fact]
    public void RetentionAge_IsMeasuredFromTheFilesLastModifiedTimeNotFromNow()
    {
        // The file stopped changing 300 days ago. A version from 320 days ago is still within
        // 30 days of that final edit, so it stays even though it is far older than 30 days.
        var file = TestData.File(
            TestData.Version("insideWindow", 320),
            TestData.Version("outsideWindow", 340)) with
        {
            LastModified = TestData.Now.AddDays(-300),
        };

        var result = _sut.Evaluate(file, TestData.Policy(keepDays: 30), TestData.Now);

        Assert.Contains(RetentionReason.KeptByAge, Decision(result, "insideWindow").Reasons);
        Assert.Equal("outsideWindow", Assert.Single(result.DeleteCandidates).Version.VersionId);
    }

    [Fact]
    public void RetentionAge_ProtectsTheFileWhenItsLastModifiedTimeIsUnknown()
    {
        var file = TestData.File(TestData.Version("old", 500)) with { LastModified = null };

        var result = _sut.Evaluate(file, TestData.Policy(keepDays: 30), TestData.Now);

        Assert.Contains(
            RetentionReason.FileLastModifiedTimeUnknown,
            Assert.Single(result.Decisions).Reasons);
        Assert.Empty(result.DeleteCandidates);
    }

    [Fact]
    public void ExplicitRetentionDate_IncludesExactCutoff()
    {
        var cutoff = TestData.Now.AddDays(-365);
        var exact = new VersionEntry { VersionId = "exact", Created = cutoff, Size = 1 };
        var older = new VersionEntry { VersionId = "older", Created = cutoff.AddTicks(-1), Size = 1 };
        var policy = TestData.Policy() with { KeepCreatedOnOrAfter = cutoff };

        var result = _sut.Evaluate(TestData.File(exact, older), policy, TestData.Now);

        Assert.Equal(RetentionDisposition.Keep, Decision(result, "exact").Disposition);
        Assert.Equal(RetentionDisposition.DeleteCandidate, Decision(result, "older").Disposition);
    }

    [Fact]
    public void UnknownCreationTime_IsKept()
    {
        var version = new VersionEntry { VersionId = "unknown-date", Size = 100 };

        var result = _sut.Evaluate(TestData.File(version), TestData.Policy(), TestData.Now);

        Assert.Contains(RetentionReason.VersionCreationTimeUnknown, Assert.Single(result.Decisions).Reasons);
        Assert.Empty(result.DeleteCandidates);
    }

    [Fact]
    public void UnknownSize_IsKeptAndNotCountedAsZeroSizedCandidate()
    {
        var version = TestData.Version("unknown-size", 100, size: null);

        var result = _sut.Evaluate(TestData.File(version), TestData.Policy(), TestData.Now);

        Assert.Contains(RetentionReason.VersionSizeUnknown, Assert.Single(result.Decisions).Reasons);
        Assert.Empty(result.DeleteCandidates);
    }

    [Fact]
    public void ApiMarkedNotDeletable_IsKept()
    {
        var version = TestData.Version("locked", 100, canDelete: false);

        var result = _sut.Evaluate(TestData.File(version), TestData.Policy(), TestData.Now);

        Assert.Contains(RetentionReason.ApiMarkedNotDeletable, Assert.Single(result.Decisions).Reasons);
        Assert.Empty(result.DeleteCandidates);
    }

    [Fact]
    public void RecentlyModifiedFile_IsProtectedAtExactBoundary()
    {
        var file = TestData.File(TestData.Version("old", 100)) with
        {
            LastModified = TestData.Now.AddDays(-7),
        };

        var result = _sut.Evaluate(
            file,
            TestData.Policy(protectDays: 7),
            TestData.Now);

        Assert.Contains(RetentionReason.FileRecentlyModified, Assert.Single(result.Decisions).Reasons);
        Assert.Empty(result.DeleteCandidates);
    }

    [Fact]
    public void RecentlyModifiedProtection_IsOffAtZeroEvenForAFileJustModified()
    {
        var file = TestData.File(TestData.Version("old", 100)) with
        {
            LastModified = TestData.Now,
        };

        var result = _sut.Evaluate(file, TestData.Policy(protectDays: 0), TestData.Now);

        Assert.DoesNotContain(
            RetentionReason.FileRecentlyModified,
            Assert.Single(result.Decisions).Reasons);
        Assert.Single(result.DeleteCandidates);
    }

    [Fact]
    public void UnknownFileModifiedTime_IsProtectedWhenProtectionWindowIsEnabled()
    {
        var file = TestData.File(TestData.Version("old", 100)) with { LastModified = null };

        var result = _sut.Evaluate(
            file,
            TestData.Policy(protectDays: 7),
            TestData.Now);

        Assert.Contains(
            RetentionReason.FileLastModifiedTimeUnknown,
            Assert.Single(result.Decisions).Reasons);
        Assert.Empty(result.DeleteCandidates);
    }

    [Fact]
    public void MinimumHistoryThreshold_ProcessesEqualButNotLowerValue()
    {
        var lower = TestData.File(TestData.Version("lower", 100, size: 99));
        var equal = TestData.File(TestData.Version("equal", 100, size: 100));
        var policy = TestData.Policy(minimumHistoryBytes: 100);

        var lowerResult = _sut.Evaluate(lower, policy, TestData.Now);
        var equalResult = _sut.Evaluate(equal, policy, TestData.Now);

        Assert.Contains(RetentionReason.HistoryBelowMinimumSize, Assert.Single(lowerResult.Decisions).Reasons);
        Assert.Single(equalResult.DeleteCandidates);
    }

    [Fact]
    public void MinimumFileSizeThreshold_ProcessesEqualButNotSmallerFile()
    {
        var small = TestData.File(TestData.Version("small", 100)) with { CurrentSize = 999 };
        var equal = TestData.File(TestData.Version("equal", 100)) with { CurrentSize = 1_000 };
        var policy = TestData.Policy(minimumFileBytes: 1_000);

        Assert.Contains(
            RetentionReason.FileBelowMinimumSize,
            Assert.Single(_sut.Evaluate(small, policy, TestData.Now).Decisions).Reasons);
        Assert.Single(_sut.Evaluate(equal, policy, TestData.Now).DeleteCandidates);
    }

    [Fact]
    public void MinimumFileSizeThreshold_ProtectsAFileWhoseSizeIsUnknown()
    {
        var file = TestData.File(TestData.Version("old", 100)) with { CurrentSize = null };

        var result = _sut.Evaluate(file, TestData.Policy(minimumFileBytes: 1_000), TestData.Now);

        Assert.Contains(RetentionReason.FileSizeUnknown, Assert.Single(result.Decisions).Reasons);
        Assert.Empty(result.DeleteCandidates);
    }

    [Fact]
    public void MinimumFileSizeThreshold_IsOffAtZero()
    {
        var file = TestData.File(TestData.Version("old", 100)) with { CurrentSize = 1 };

        Assert.Single(_sut.Evaluate(file, TestData.Policy(), TestData.Now).DeleteCandidates);
    }

    [Fact]
    public void MinimumVersionThreshold_ProcessesEqualButNotLowerCount()
    {
        var one = TestData.File(TestData.Version("one", 100));
        var two = TestData.File(TestData.Version("one", 100), TestData.Version("two", 101));
        var policy = TestData.Policy(minimumVersionCount: 2);

        Assert.Empty(_sut.Evaluate(one, policy, TestData.Now).DeleteCandidates);
        Assert.Equal(2, _sut.Evaluate(two, policy, TestData.Now).DeleteCandidates.Count);
    }

    [Fact]
    public void MaximumDeletes_IsGlobalAndSelectsOldestFirst()
    {
        var newerFile = TestData.File(TestData.Version("newer", 50)) with
        {
            ItemId = "item-newer",
            CloudPath = "newer.txt",
        };
        var olderFile = TestData.File(TestData.Version("older", 100)) with
        {
            ItemId = "item-older",
            CloudPath = "older.txt",
        };

        var result = _sut.Evaluate(
            [newerFile, olderFile],
            TestData.Policy(maximumDeletes: 1),
            TestData.Now);

        Assert.Equal("older", Assert.Single(result.DeleteCandidates).Version.VersionId);
        Assert.Equal(1, result.DeferredDeleteCount);
        Assert.Equal(
            RetentionDisposition.DeferredByRunLimit,
            Decision(result, "newer").Disposition);
    }

    [Fact]
    public void ZeroMaximumDeletes_DefersEveryEligibleVersion()
    {
        var result = _sut.Evaluate(
            TestData.File(TestData.Version("old", 100)),
            TestData.Policy(maximumDeletes: 0),
            TestData.Now);

        Assert.Empty(result.DeleteCandidates);
        Assert.Equal(1, result.DeferredDeleteCount);
    }

    [Fact]
    public void SameTimestampAtCountBoundary_IsKeptConservatively()
    {
        var first = TestData.Version("a", 10);
        var second = TestData.Version("b", 10);

        var result = _sut.Evaluate(
            TestData.File(first, second),
            TestData.Policy(keepLatest: 1),
            TestData.Now);

        Assert.Empty(result.DeleteCandidates);
        Assert.All(result.Decisions, decision =>
            Assert.Contains(RetentionReason.KeptByLatestCount, decision.Reasons));
    }

    [Fact]
    public void RetrievalFailure_ProtectsTheAffectedFile()
    {
        var file = TestData.File(TestData.Version("old", 100)) with
        {
            RetrievalStatus = RetrievalStatus.AccessDenied,
        };

        var result = _sut.Evaluate(file, TestData.Policy(), TestData.Now);

        Assert.Contains(RetentionReason.FileRetrievalFailed, Assert.Single(result.Decisions).Reasons);
        Assert.Empty(result.DeleteCandidates);
    }

    [Fact]
    public void NegativePolicyValue_IsRejected()
    {
        var policy = TestData.Policy() with { KeepDays = -1 };

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _sut.Evaluate(TestData.File(), policy, TestData.Now));
    }

    private static VersionRetentionDecision Decision(
        RetentionEvaluationResult result,
        string versionId) => result.Decisions.Single(
        item => item.Version.VersionId == versionId);
}
