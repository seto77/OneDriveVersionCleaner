using System.Text.Json;
using OneDriveVersionCleaner.Core.Auditing;
using OneDriveVersionCleaner.Core.Deletion;
using OneDriveVersionCleaner.Core.Models;

namespace OneDriveVersionCleaner.Core.Tests;

public sealed class DeletionAuditLogTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        "OneDriveVersionCleanerTests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task WriteRunAsync_RecordsEveryAttemptWithItsIdentifiersAndOutcome()
    {
        var path = await new DeletionAuditLog(_directory).WriteRunAsync(
            "Business1",
            TestData.Policy(keepLatest: 10, keepDays: 30),
            Result(Outcome("512", 300, DeleteResultStatus.Deleted)));

        var events = ReadEvents(path);
        var attempt = Assert.Single(events, e => Name(e) == "versionRecycleAttempted");
        var details = attempt.GetProperty("details");

        Assert.Equal("Business1", details.GetProperty("accountKey").GetString());
        Assert.Equal("drive-1", details.GetProperty("driveId").GetString());
        Assert.Equal("item-1", details.GetProperty("itemId").GetString());
        Assert.Equal("Research/report.xlsx", details.GetProperty("cloudPath").GetString());
        Assert.Equal("512", details.GetProperty("versionId").GetString());
        Assert.Equal(300, details.GetProperty("versionSizeBytes").GetInt64());
        Assert.Equal("Deleted", details.GetProperty("status").GetString());
        Assert.True(details.GetProperty("succeeded").GetBoolean());
        Assert.NotEqual(default, attempt.GetProperty("timestampUtc").GetDateTimeOffset());
    }

    [Fact]
    public async Task WriteRunAsync_BracketsTheRunWithStartAndCompletionEvents()
    {
        var path = await new DeletionAuditLog(_directory).WriteRunAsync(
            "Business1",
            TestData.Policy(),
            Result(
                Outcome("512", 300, DeleteResultStatus.Deleted),
                Outcome("1024", 400, DeleteResultStatus.AccessDenied)));

        var events = ReadEvents(path);

        Assert.Equal("runStarted", Name(events[0]));
        Assert.Equal("runCompleted", Name(events[^1]));

        var completion = events[^1].GetProperty("details");
        Assert.Equal(1, completion.GetProperty("deletedCount").GetInt32());
        Assert.Equal(1, completion.GetProperty("accessDeniedCount").GetInt32());
        Assert.Equal(300, completion.GetProperty("knownReclaimedBytes").GetInt64());

        // Every line of a run shares one identifier, so concurrent runs stay separable.
        var runIds = events.Select(e => e.GetProperty("runId").GetString()).Distinct();
        Assert.Single(runIds);
    }

    [Fact]
    public async Task WriteRunAsync_RecordsTheCloudReReadAndTheLocalFileCheck()
    {
        var result = Result(Outcome("512", 300, DeleteResultStatus.Deleted)) with
        {
            Verifications =
            [
                new FileDeletionVerification(
                    TestData.File(),
                    2,
                    ["512"],
                    TestData.ChangedLocalFile(),
                    null),
            ],
        };

        var path = await new DeletionAuditLog(_directory).WriteRunAsync(
            "Business1",
            TestData.Policy(),
            result);

        var verification = Assert.Single(ReadEvents(path), e => Name(e) == "versionsReverified");
        var details = verification.GetProperty("details");

        Assert.Equal(2, details.GetProperty("remainingPreviousVersionCount").GetInt32());
        Assert.False(details.GetProperty("verified").GetBoolean());
        Assert.Equal("512", details.GetProperty("unexpectedlyPresentVersionIds")[0].GetString());

        var localFile = details.GetProperty("localFile");
        Assert.Equal("TimestampChanged", localFile.GetProperty("change").GetString());
        Assert.True(localFile.GetProperty("existedBefore").GetBoolean());
        Assert.True(localFile.GetProperty("existsAfter").GetBoolean());
        Assert.NotEqual(
            localFile.GetProperty("lastWriteBeforeUtc").GetString(),
            localFile.GetProperty("lastWriteAfterUtc").GetString());
    }

    [Fact]
    public async Task WriteRunAsync_RecordsWhyARunWasAborted()
    {
        var result = Result(Outcome("512", 300, DeleteResultStatus.Deleted)) with
        {
            AbortReason = "ローカルファイルの状態が変化しました",
        };

        var path = await new DeletionAuditLog(_directory).WriteRunAsync(
            "Business1",
            TestData.Policy(),
            result);

        var completion = ReadEvents(path)[^1].GetProperty("details");
        Assert.Equal(
            "ローカルファイルの状態が変化しました",
            completion.GetProperty("abortReason").GetString());
    }

    [Fact]
    public async Task WriteRunAsync_AppendsWithoutLosingEarlierRuns()
    {
        var log = new DeletionAuditLog(_directory);
        var result = Result(Outcome("512", 300, DeleteResultStatus.Deleted));

        var first = await log.WriteRunAsync("Business1", TestData.Policy(), result);
        var second = await log.WriteRunAsync("Business1", TestData.Policy(), result);

        Assert.Equal(first, second);
        Assert.Equal(6, ReadEvents(second).Count);
    }

    [Fact]
    public void GetLogPath_UsesOneFilePerLocalDay()
    {
        var log = new DeletionAuditLog(_directory);
        var timestamp = new DateTimeOffset(2026, 8, 2, 3, 4, 5, TimeSpan.Zero);

        Assert.Equal(
            Path.Combine(_directory, $"delete-{timestamp.ToLocalTime():yyyyMMdd}.jsonl"),
            log.GetLogPath(timestamp));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    private static string? Name(JsonElement element) => element.GetProperty("eventName").GetString();

    private static IReadOnlyList<JsonElement> ReadEvents(string path) => File
        .ReadAllLines(path)
        .Where(line => !string.IsNullOrWhiteSpace(line))
        .Select(line => JsonDocument.Parse(line).RootElement.Clone())
        .ToArray();

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
