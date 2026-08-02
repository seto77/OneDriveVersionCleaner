using OneDriveVersionCleaner.Core.Models;

namespace OneDriveVersionCleaner.Core.Tests;

public sealed class ModelContractTests
{
    [Fact]
    public void FileEntry_UnknownHistoricalSizeDoesNotBecomeZeroTotal()
    {
        var file = TestData.File(new VersionEntry
        {
            VersionId = "unknown",
            Created = TestData.Now.AddDays(-1),
            Size = null,
        });

        Assert.Equal(1, file.PreviousVersionCount);
        Assert.Equal(1, file.UnknownPreviousVersionSizeCount);
        Assert.Equal(0, file.KnownPreviousVersionTotalSize);
        Assert.Null(file.PreviousVersionTotalSize);
    }

    [Theory]
    [InlineData(DeleteResultStatus.Deleted, true)]
    [InlineData(DeleteResultStatus.AlreadyDeleted, true)]
    [InlineData(DeleteResultStatus.NotDeletable, false)]
    [InlineData(DeleteResultStatus.AccessDenied, false)]
    [InlineData(DeleteResultStatus.TransientError, false)]
    [InlineData(DeleteResultStatus.PermanentError, false)]
    public void DeleteResult_RepresentsOutcomeWithoutLosingSuccessSemantics(
        DeleteResultStatus status,
        bool expectedSuccess)
    {
        Assert.Equal(expectedSuccess, new DeleteResult { Status = status }.IsSuccess);
    }
}
