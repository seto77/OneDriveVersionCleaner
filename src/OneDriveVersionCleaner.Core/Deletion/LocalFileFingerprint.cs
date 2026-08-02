namespace OneDriveVersionCleaner.Core.Deletion;

/// <summary>
/// The observable state of the synced local copy of a file. Reading it never downloads an
/// online-only file, so it is safe to sample around a delete.
/// </summary>
public sealed record LocalFileFingerprint(
    string Path,
    bool Exists,
    DateTimeOffset? LastWriteTimeUtc,
    long? Length,
    string? Error)
{
    /// <summary>
    /// True when the path could actually be sampled, so a comparison is meaningful.
    /// </summary>
    public bool IsObserved => Exists && Error is null;

    public static LocalFileFingerprint Capture(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return new LocalFileFingerprint(string.Empty, false, null, null, null);
        }

        try
        {
            var info = new FileInfo(path);
            info.Refresh();
            return info.Exists
                ? new LocalFileFingerprint(
                    path,
                    true,
                    new DateTimeOffset(info.LastWriteTimeUtc, TimeSpan.Zero),
                    info.Length,
                    null)
                : new LocalFileFingerprint(path, false, null, null, null);
        }
        catch (Exception exception) when (exception is IOException or
            UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return new LocalFileFingerprint(path, false, null, null, exception.Message);
        }
    }

    /// <summary>
    /// Compares this sample against <paramref name="baseline"/>.
    /// A path with no local copy yields <see cref="LocalFileChange.NotChecked"/> rather than a
    /// false alarm. A path that could not be read yields <see cref="LocalFileChange.CheckFailed"/>
    /// and is never treated as safe: an unreadable sample can hide a file that just disappeared.
    /// </summary>
    public LocalFileChange CompareWith(LocalFileFingerprint baseline)
    {
        ArgumentNullException.ThrowIfNull(baseline);

        if (baseline.Error is not null || Error is not null)
        {
            return LocalFileChange.CheckFailed;
        }

        if (!baseline.Exists)
        {
            return LocalFileChange.NotChecked;
        }

        if (!Exists)
        {
            return LocalFileChange.Disappeared;
        }

        if (LastWriteTimeUtc != baseline.LastWriteTimeUtc)
        {
            return LocalFileChange.TimestampChanged;
        }

        return Length != baseline.Length
            ? LocalFileChange.LengthChanged
            : LocalFileChange.Unchanged;
    }
}

public enum LocalFileChange
{
    /// <summary>There is no local copy to compare, so nothing was verified.</summary>
    NotChecked,

    /// <summary>The local file is still there with the same timestamp and size.</summary>
    Unchanged,

    /// <summary>The local file is gone. Deleting history must never do this.</summary>
    Disappeared,

    TimestampChanged,

    LengthChanged,

    /// <summary>
    /// The local copy could not be read, so the check produced no evidence either way.
    /// Treated as unsafe so a failed sample cannot mask a change.
    /// </summary>
    CheckFailed,
}

/// <summary>
/// The before/after pair captured around one file's deletions.
/// </summary>
public sealed record LocalFileCheck(
    LocalFileFingerprint Before,
    LocalFileFingerprint After)
{
    public LocalFileChange Change => After.CompareWith(Before);

    public bool IsSafe => Change is LocalFileChange.Unchanged or LocalFileChange.NotChecked;

    public static LocalFileCheck NotChecked(string? path)
    {
        var empty = new LocalFileFingerprint(path ?? string.Empty, false, null, null, null);
        return new LocalFileCheck(empty, empty);
    }
}
