using OneDriveVersionCleaner.Core.Deletion;
using OneDriveVersionCleaner.Core.Models;

namespace OneDriveVersionCleaner.Core.Tests;

public sealed class DeletionRunResultTests
{
    [Fact]
    public void KnownReclaimedBytes_CountsOnlyVersionsRemovedInThisRun()
    {
        var result = Result(
            Outcome("deleted", 300, DeleteResultStatus.Deleted),
            Outcome("alreadyGone", 500, DeleteResultStatus.AlreadyDeleted),
            Outcome("denied", 700, DeleteResultStatus.AccessDenied));

        Assert.Equal(300, result.KnownReclaimedBytes);
    }

    [Fact]
    public void KnownReclaimedBytes_ExcludesUnknownSizesInsteadOfTreatingThemAsZero()
    {
        var result = Result(
            Outcome("known", 400, DeleteResultStatus.Deleted),
            Outcome("unknown", null, DeleteResultStatus.Deleted));

        Assert.Equal(400, result.KnownReclaimedBytes);
        Assert.Equal(1, result.UnknownReclaimedSizeCount);
    }

    [Fact]
    public void Counts_SeparateEveryDeleteStatus()
    {
        var result = Result(
            Outcome("a", 1, DeleteResultStatus.Deleted),
            Outcome("b", 1, DeleteResultStatus.AlreadyDeleted),
            Outcome("c", 1, DeleteResultStatus.NotDeletable),
            Outcome("d", 1, DeleteResultStatus.AccessDenied),
            Outcome("e", 1, DeleteResultStatus.TransientError),
            Outcome("f", 1, DeleteResultStatus.PermanentError));

        Assert.Equal(1, result.DeletedCount);
        Assert.Equal(1, result.AlreadyDeletedCount);
        Assert.Equal(1, result.NotDeletableCount);
        Assert.Equal(1, result.AccessDeniedCount);
        Assert.Equal(1, result.TransientErrorCount);
        Assert.Equal(1, result.PermanentErrorCount);
        Assert.Equal(6, result.AttemptedCount);

        // AlreadyDeleted is idempotent success, so only the last four count as failures.
        Assert.Equal(4, result.FailureCount);
    }

    [Fact]
    public void IsVerified_IsFalseWhenARemovedVersionIsStillPresent()
    {
        var verification = new FileDeletionVerification(
            TestData.File(),
            2,
            ["512"],
            TestData.UnchangedLocalFile(),
            null);

        Assert.False(verification.IsVerified);
    }

    [Fact]
    public void IsVerified_IsFalseWhenTheReReadItselfFailed()
    {
        var verification = new FileDeletionVerification(
            TestData.File(),
            null,
            [],
            TestData.UnchangedLocalFile(),
            "HTTP 503");

        Assert.False(verification.IsVerified);
    }

    [Fact]
    public void IsVerified_IsFalseWhenTheLocalFileChanged()
    {
        var verification = new FileDeletionVerification(
            TestData.File(),
            2,
            [],
            TestData.ChangedLocalFile(),
            null);

        Assert.False(verification.IsVerified);
    }

    [Fact]
    public void LocalFileWarnings_ListOnlyTheFilesWhoseLocalCopyMoved()
    {
        var safe = new FileDeletionVerification(
            TestData.File(),
            0,
            [],
            TestData.UnchangedLocalFile(),
            null);
        var unsafeVerification = new FileDeletionVerification(
            TestData.File() with { Name = "moved.xlsx" },
            0,
            [],
            TestData.ChangedLocalFile(),
            null);

        var result = Result() with { Verifications = [safe, unsafeVerification] };

        Assert.Equal("moved.xlsx", Assert.Single(result.LocalFileWarnings).File.Name);
    }

    private static VersionDeletionOutcome Outcome(string id, long? size, DeleteResultStatus status) =>
        new(
            TestData.File(),
            TestData.Version(id, 400, size),
            new DeleteResult { Status = status });

    private static DeletionRunResult Result(params VersionDeletionOutcome[] outcomes) => new(
        TestData.Now,
        TestData.Now.AddSeconds(5),
        outcomes,
        [],
        NotAttemptedCount: 0,
        WasStopped: false);
}
