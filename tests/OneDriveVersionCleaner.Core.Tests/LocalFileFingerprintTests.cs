using OneDriveVersionCleaner.Core.Deletion;

namespace OneDriveVersionCleaner.Core.Tests;

public sealed class LocalFileFingerprintTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        "OneDriveVersionCleanerTests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public void Capture_ReadsSizeAndTimestampOfAnExistingFile()
    {
        var path = WriteFile("report.xlsx", "content");

        var fingerprint = LocalFileFingerprint.Capture(path);

        Assert.True(fingerprint.Exists);
        Assert.True(fingerprint.IsObserved);
        Assert.Equal(7, fingerprint.Length);
        Assert.NotNull(fingerprint.LastWriteTimeUtc);
        Assert.Null(fingerprint.Error);
    }

    [Fact]
    public void Capture_ReportsAMissingFileWithoutThrowing()
    {
        var fingerprint = LocalFileFingerprint.Capture(Path.Combine(_directory, "missing.xlsx"));

        Assert.False(fingerprint.Exists);
        Assert.False(fingerprint.IsObserved);
        Assert.Null(fingerprint.Error);
    }

    [Fact]
    public void Capture_TreatsAnEmptyPathAsNothingToObserve()
    {
        Assert.False(LocalFileFingerprint.Capture(null).IsObserved);
        Assert.False(LocalFileFingerprint.Capture("  ").IsObserved);
    }

    [Fact]
    public void CompareWith_ReportsUnchangedWhenNothingMoved()
    {
        var path = WriteFile("report.xlsx", "content");
        var before = LocalFileFingerprint.Capture(path);

        Assert.Equal(LocalFileChange.Unchanged, LocalFileFingerprint.Capture(path).CompareWith(before));
    }

    [Fact]
    public void CompareWith_DetectsAFileThatDisappeared()
    {
        var path = WriteFile("report.xlsx", "content");
        var before = LocalFileFingerprint.Capture(path);
        File.Delete(path);

        Assert.Equal(LocalFileChange.Disappeared, LocalFileFingerprint.Capture(path).CompareWith(before));
    }

    [Fact]
    public void CompareWith_DetectsAChangedTimestamp()
    {
        var path = WriteFile("report.xlsx", "content");
        var before = LocalFileFingerprint.Capture(path);
        File.SetLastWriteTimeUtc(path, File.GetLastWriteTimeUtc(path).AddMinutes(5));

        Assert.Equal(
            LocalFileChange.TimestampChanged,
            LocalFileFingerprint.Capture(path).CompareWith(before));
    }

    [Fact]
    public void CompareWith_DetectsAChangedLengthEvenWhenTheTimestampIsPreserved()
    {
        var path = WriteFile("report.xlsx", "content");
        var before = LocalFileFingerprint.Capture(path);
        var timestamp = File.GetLastWriteTimeUtc(path);
        File.WriteAllText(path, "much longer content");
        File.SetLastWriteTimeUtc(path, timestamp);

        Assert.Equal(
            LocalFileChange.LengthChanged,
            LocalFileFingerprint.Capture(path).CompareWith(before));
    }

    [Fact]
    public void CompareWith_ReportsNotCheckedWhenThereWasNoBaseline()
    {
        var path = WriteFile("report.xlsx", "content");
        var missingBaseline = LocalFileFingerprint.Capture(Path.Combine(_directory, "missing.xlsx"));

        Assert.Equal(
            LocalFileChange.NotChecked,
            LocalFileFingerprint.Capture(path).CompareWith(missingBaseline));
    }

    [Fact]
    public void CompareWith_ReportsCheckFailedWhenTheAfterSampleCouldNotBeRead()
    {
        var path = WriteFile("report.xlsx", "content");
        var before = LocalFileFingerprint.Capture(path);
        var unreadableAfter = new LocalFileFingerprint(path, false, null, null, "アクセスが拒否されました");

        // An unreadable sample proves nothing, and must not be reported as "still there".
        Assert.Equal(LocalFileChange.CheckFailed, unreadableAfter.CompareWith(before));
    }

    [Fact]
    public void CompareWith_ReportsCheckFailedWhenTheBaselineItselfCouldNotBeRead()
    {
        var path = WriteFile("report.xlsx", "content");
        var unreadableBefore = new LocalFileFingerprint(path, false, null, null, "アクセスが拒否されました");

        Assert.Equal(
            LocalFileChange.CheckFailed,
            LocalFileFingerprint.Capture(path).CompareWith(unreadableBefore));
    }

    [Fact]
    public void IsSafe_AllowsUnchangedAndUncheckedButNothingElse()
    {
        var path = WriteFile("report.xlsx", "content");
        var before = LocalFileFingerprint.Capture(path);

        Assert.True(new LocalFileCheck(before, before).IsSafe);
        Assert.True(LocalFileCheck.NotChecked(path).IsSafe);
        Assert.False(new LocalFileCheck(before, before with { Exists = false }).IsSafe);
    }

    [Fact]
    public void IsSafe_RejectsAFailedCheckSoAnUnreadableSampleCannotMaskAChange()
    {
        var path = WriteFile("report.xlsx", "content");
        var before = LocalFileFingerprint.Capture(path);
        var unreadableAfter = new LocalFileFingerprint(path, false, null, null, "アクセスが拒否されました");

        Assert.False(new LocalFileCheck(before, unreadableAfter).IsSafe);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    private string WriteFile(string name, string content)
    {
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, name);
        File.WriteAllText(path, content);
        return path;
    }
}
