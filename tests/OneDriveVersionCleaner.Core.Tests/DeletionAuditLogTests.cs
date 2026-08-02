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
    public async Task RecordAttemptAsync_RecordsEveryAttemptWithItsIdentifiersAndOutcome()
    {
        await using var journal = await BeginAsync();
        await journal.RecordAttemptAsync(Outcome("512", 300, DeleteResultStatus.Deleted));

        var attempt = Assert.Single(ReadEvents(journal.Path), e => Name(e) == "versionRecycleAttempted");
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
    public async Task Journal_BracketsTheRunWithStartAndCompletionEvents()
    {
        var outcomes = new[]
        {
            Outcome("512", 300, DeleteResultStatus.Deleted),
            Outcome("1024", 400, DeleteResultStatus.AccessDenied),
        };

        await using var journal = await BeginAsync(plannedVersionCount: 2);
        foreach (var outcome in outcomes)
        {
            await journal.RecordAttemptAsync(outcome);
        }

        await journal.CompleteAsync(Result(outcomes));

        var events = ReadEvents(journal.Path);
        Assert.Equal("runStarted", Name(events[0]));
        Assert.Equal("runCompleted", Name(events[^1]));
        Assert.Equal(2, events[0].GetProperty("details").GetProperty("plannedVersionCount").GetInt32());

        var completion = events[^1].GetProperty("details");
        Assert.Equal(1, completion.GetProperty("deletedCount").GetInt32());
        Assert.Equal(1, completion.GetProperty("accessDeniedCount").GetInt32());
        Assert.Equal(300, completion.GetProperty("knownReclaimedBytes").GetInt64());

        // Every line of a run shares one identifier, so concurrent runs stay separable.
        Assert.Single(events.Select(e => e.GetProperty("runId").GetString()).Distinct());
    }

    /// <summary>
    /// The reason the journal exists. A run that is killed part-way still has to leave behind
    /// what it had already done, and the missing closing line is what says it was killed.
    /// </summary>
    [Fact]
    public async Task Journal_IsReadableBeforeTheRunEnds()
    {
        await using var journal = await BeginAsync();
        await journal.RecordAttemptAsync(Outcome("512", 300, DeleteResultStatus.Deleted));

        // Read it while the journal is still open, as a crash-time reader would.
        var events = ReadEvents(journal.Path);

        Assert.Equal(
            new[] { "runStarted", "versionRecycleAttempted" },
            events.Select(Name).ToArray());
        Assert.DoesNotContain(events, e => Name(e) == "runCompleted");
        Assert.False(journal.IsComplete);
    }

    [Fact]
    public async Task RecordVerificationAsync_RecordsTheCloudReReadAndTheLocalFileCheck()
    {
        await using var journal = await BeginAsync();
        await journal.RecordVerificationAsync(new FileDeletionVerification(
            TestData.File(),
            2,
            ["512"],
            TestData.ChangedLocalFile(),
            null));

        var verification = Assert.Single(ReadEvents(journal.Path), e => Name(e) == "versionsReverified");
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
    public async Task CompleteAsync_RecordsWhyARunWasAborted()
    {
        var result = Result([Outcome("512", 300, DeleteResultStatus.Deleted)]) with
        {
            AbortReason = "ローカルファイルの状態が変化しました",
        };

        await using var journal = await BeginAsync();
        await journal.CompleteAsync(result);

        var completion = ReadEvents(journal.Path)[^1].GetProperty("details");
        Assert.Equal(
            "ローカルファイルの状態が変化しました",
            completion.GetProperty("abortReason").GetString());
    }

    [Fact]
    public async Task BeginRunAsync_AppendsWithoutLosingEarlierRuns()
    {
        var log = new DeletionAuditLog(_directory);
        var outcome = Outcome("512", 300, DeleteResultStatus.Deleted);

        string first;
        await using (var journal = await log.BeginRunAsync("Business1", TestData.Policy(), 1))
        {
            first = journal.Path;
            await journal.RecordAttemptAsync(outcome);
            await journal.CompleteAsync(Result([outcome]));
        }

        await using var second = await log.BeginRunAsync("Business1", TestData.Policy(), 1);
        await second.RecordAttemptAsync(outcome);
        await second.CompleteAsync(Result([outcome]));

        Assert.Equal(first, second.Path);
        Assert.Equal(6, ReadEvents(second.Path).Count);
        Assert.Equal(2, ReadEvents(second.Path).Select(e => e.GetProperty("runId").GetString()).Distinct().Count());
    }

    [Fact]
    public async Task BeginRunAsync_TakesItsStartTimeFromWhenTheJournalOpened()
    {
        var before = DateTimeOffset.UtcNow;
        await using var journal = await BeginAsync();

        Assert.InRange(journal.StartedAtUtc, before, DateTimeOffset.UtcNow);
        Assert.Equal(
            journal.StartedAtUtc.ToUniversalTime(),
            ReadEvents(journal.Path)[0].GetProperty("timestampUtc").GetDateTimeOffset());
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

    private Task<DeletionRunJournal> BeginAsync(int plannedVersionCount = 1) =>
        new DeletionAuditLog(_directory).BeginRunAsync(
            "Business1",
            TestData.Policy(keepLatest: 10, keepDays: 30),
            plannedVersionCount);

    private static string? Name(JsonElement element) => element.GetProperty("eventName").GetString();

    private static IReadOnlyList<JsonElement> ReadEvents(string path)
    {
        // Opened for shared reading: the journal still has the file open for append.
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd()
            .Split('\n')
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => JsonDocument.Parse(line).RootElement.Clone())
            .ToArray();
    }

    private static VersionDeletionOutcome Outcome(string id, long? size, DeleteResultStatus status) =>
        new(
            TestData.File(),
            TestData.Version(id, 400, size),
            new DeleteResult { Status = status });

    private static DeletionRunResult Result(IReadOnlyList<VersionDeletionOutcome> outcomes) => new(
        TestData.Now,
        TestData.Now.AddSeconds(5),
        outcomes,
        [],
        NotAttemptedCount: 0,
        WasStopped: false);
}
