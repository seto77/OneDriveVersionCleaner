using OneDriveVersionCleaner.Core.Scanning;

namespace OneDriveVersionCleaner.Core.Tests;

public sealed class LocalFileSurveyTests : IDisposable
{
    private const long Megabyte = 1024 * 1024;

    private readonly string _root = Path.Combine(
        Path.GetTempPath(),
        "OneDriveVersionCleanerTests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public void Run_KeepsOnlyFilesLargeEnoughToBeWorthARequest()
    {
        WriteFile(2 * Megabyte, "big.7z");
        WriteFile(512 * 1024, "small.txt");

        var survey = Run(minimumBytes: Megabyte);

        Assert.Equal("big.7z", Assert.Single(survey.Candidates).CloudRelativePath);

        // The whole population is still reported, so the caller can say what the filter removed.
        Assert.Equal(2, survey.FilesExamined);
    }

    [Fact]
    public void Run_DescendsIntoSubfolders()
    {
        WriteFile(2 * Megabyte, "資料", "講義", "録画.mp4");

        var survey = Run(minimumBytes: Megabyte);

        Assert.Equal("資料/講義/録画.mp4", Assert.Single(survey.Candidates).CloudRelativePath);
    }

    [Fact]
    public void Run_MeasuresCloudPathsFromTheSyncRootEvenWhenScanningASubfolder()
    {
        WriteFile(2 * Megabyte, "docs", "inner", "kept.bin");
        WriteFile(2 * Megabyte, "elsewhere", "ignored.bin");

        // Scanning a subfolder must still address files the way the service does, which is
        // from the library root, not from the folder the user happened to pick.
        var survey = LocalFileSurvey.Run(
            _root,
            Path.Combine(_root, "docs"),
            Megabyte,
            null,
            CancellationToken.None);

        Assert.Equal("docs/inner/kept.bin", Assert.Single(survey.Candidates).CloudRelativePath);
        Assert.Equal(1, survey.FilesExamined);
    }

    [Fact]
    public void Run_KeepsEverythingWhenTheThresholdIsDisabled()
    {
        WriteFile(10, "tiny.txt");

        Assert.Single(Run(minimumBytes: 0).Candidates);
    }

    [Fact]
    public void Run_ReportsTheLengthAndWriteTimeTheCacheComparesAgainst()
    {
        var path = WriteFile(2 * Megabyte, "big.7z");

        var candidate = Assert.Single(Run(minimumBytes: Megabyte).Candidates);

        Assert.Equal(path, candidate.LocalPath);
        Assert.Equal(2 * Megabyte, candidate.Length);
        Assert.Equal(
            new DateTimeOffset(File.GetLastWriteTimeUtc(path), TimeSpan.Zero),
            candidate.LastWriteTimeUtc);
    }

    [Fact]
    public void Run_SkipsWhatOneDriveKeepsForItself()
    {
        var path = WriteFile(2 * Megabyte, "bookkeeping.dat");
        File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.System);

        var survey = Run(minimumBytes: Megabyte);

        Assert.Empty(survey.Candidates);
        Assert.Empty(survey.Errors);
    }

    [Fact]
    public void Run_DoesNotFollowADirectoryLink()
    {
        WriteFile(2 * Megabyte, "real", "big.7z");

        // Creating a link needs a privilege this machine may not grant, and xUnit 2 cannot
        // skip at run time, so an unavailable link leaves the assertion unexercised rather
        // than failing for a reason that has nothing to do with the survey.
        try
        {
            Directory.CreateSymbolicLink(
                Path.Combine(_root, "link"),
                Path.Combine(_root, "real"));
        }
        catch (Exception exception) when (exception is UnauthorizedAccessException or IOException)
        {
            return;
        }

        // Found once through the real folder, and not a second time through the link.
        Assert.Single(Run(minimumBytes: Megabyte).Candidates);
    }

    [Fact]
    public void Run_RejectsAScanRootOutsideTheSyncRoot()
    {
        Directory.CreateDirectory(_root);

        Assert.Throws<ArgumentException>(() => LocalFileSurvey.Run(
            Path.Combine(_root, "sync"),
            Path.Combine(_root, "somewhere-else"),
            Megabyte,
            null,
            CancellationToken.None));
    }

    [Fact]
    public void Run_RecordsAnUnreadableFolderInsteadOfEndingTheWalk()
    {
        // The walk must survive a folder it cannot read; reporting nothing at all would look
        // exactly like a sync folder that holds nothing worth cleaning.
        var survey = Run(minimumBytes: Megabyte);

        Assert.Empty(survey.Candidates);
        Assert.Single(survey.Errors);
        Assert.Contains(_root, survey.Errors[0], StringComparison.Ordinal);
    }

    [Fact]
    public void Run_StopsWhenCancelled()
    {
        WriteFile(2 * Megabyte, "big.7z");
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() => LocalFileSurvey.Run(
            _root,
            _root,
            Megabyte,
            null,
            cancellation.Token));
    }

    [Fact]
    public void Run_RejectsANegativeThreshold()
    {
        Directory.CreateDirectory(_root);

        Assert.Throws<ArgumentOutOfRangeException>(() => LocalFileSurvey.Run(
            _root,
            _root,
            -1,
            null,
            CancellationToken.None));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private LocalSurveyResult Run(long minimumBytes) =>
        LocalFileSurvey.Run(_root, _root, minimumBytes, null, CancellationToken.None);

    /// <summary>
    /// Creates a file of exactly <paramref name="length"/> bytes without writing them, so a
    /// multi-megabyte case costs nothing.
    /// </summary>
    private string WriteFile(long length, params string[] segments)
    {
        var path = Path.Combine([_root, .. segments]);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var stream = new FileStream(path, FileMode.Create, FileAccess.Write);
        stream.SetLength(length);
        return path;
    }
}
