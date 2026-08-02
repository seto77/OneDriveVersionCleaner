using System.Text.Json.Serialization;

namespace OneDriveVersionCleaner.App;

/// <summary>
/// SharePoint kept refusing the request even after the retries and waits. Distinguished from
/// other failures so the UI can say "slow down and try a smaller scope" instead of pointing at
/// sign-in, which is not the problem.
/// </summary>
internal sealed class SharePointThrottledException(string message) : Exception(message);

internal sealed record SharePointSiteInfo
{
    public string Title { get; init; } = string.Empty;

    public string Url { get; init; } = string.Empty;
}

/// <summary>
/// SharePoint returns a path both as a string and as a resource path object. The object
/// form is always decoded, so it is preferred whenever it is present.
/// </summary>
internal sealed record SharePointResourcePath
{
    public string DecodedUrl { get; init; } = string.Empty;
}

/// <summary>
/// The document library's versioning settings. They make it provable, rather than merely
/// likely, that a given file has no history at all — which lets the scan skip its request.
/// </summary>
internal sealed record SharePointVersioningInfo
{
    public bool EnableVersioning { get; init; } = true;

    public bool EnableMinorVersions { get; init; } = true;

    /// <summary>
    /// True unless the library's settings rule out any previous version for this file.
    /// With minor versions off, version 1.0 is the first version ever written, so nothing can
    /// precede it. With minor versions on, drafts may precede 1.0, so no shortcut is taken.
    /// </summary>
    public bool MayHaveHistory(SharePointFileInfo file) =>
        EnableVersioning &&
        (EnableMinorVersions || file.MajorVersion > 1 || file.MinorVersion > 0);
}

internal sealed record SharePointFolderInfo
{
    public Guid UniqueId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string ServerRelativeUrl { get; init; } = string.Empty;

    public SharePointResourcePath? ServerRelativePath { get; init; }

    public int ItemCount { get; init; }

    public string Path => string.IsNullOrEmpty(ServerRelativePath?.DecodedUrl)
        ? ServerRelativeUrl
        : ServerRelativePath.DecodedUrl;
}

internal sealed record SharePointFileInfo
{
    public Guid UniqueId { get; init; }

    public string Name { get; init; } = string.Empty;

    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public long Length { get; init; }

    public DateTimeOffset? TimeLastModified { get; init; }

    public string ServerRelativeUrl { get; init; } = string.Empty;

    public SharePointResourcePath? ServerRelativePath { get; init; }

    public string UIVersionLabel { get; init; } = string.Empty;

    public int MajorVersion { get; init; }

    public int MinorVersion { get; init; }

    public string Path => string.IsNullOrEmpty(ServerRelativePath?.DecodedUrl)
        ? ServerRelativeUrl
        : ServerRelativePath.DecodedUrl;
}

/// <summary>
/// One row of the document library's item list. A single paged query over this list covers
/// the whole library, which is what makes a large scan affordable: the per-file version
/// request is then only needed for the few files that actually carry history worth looking at.
/// </summary>
internal sealed record SharePointListItemInfo
{
    /// <summary>0 for a file, 1 for a folder.</summary>
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public int FSObjType { get; init; }

    /// <summary>Server-relative path of the item.</summary>
    public string FileRef { get; init; } = string.Empty;

    public string FileLeafRef { get; init; } = string.Empty;

    /// <summary>Current file size. Absent on some deployments.</summary>
    [JsonPropertyName("File_x0020_Size")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public long? FileSize { get; init; }

    /// <summary>
    /// Total bytes of every version of the file. The whole point of this query: it exposes
    /// history size without asking for the version list.
    /// </summary>
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public long? SMTotalFileStreamSize { get; init; }

    public DateTimeOffset? Modified { get; init; }

    /// <summary>Current version label, for example <c>3.0</c>.</summary>
    public string UIVersionString { get; init; } = string.Empty;

    public bool IsFile => FSObjType == 0;

    /// <summary>
    /// Bytes held by previous versions, or null when the service did not report the total.
    /// Never negative: a total that is smaller than the current size means the figures are not
    /// comparable, and guessing would be worse than admitting the value is unknown.
    /// </summary>
    public long? EstimatedHistoryBytes => SMTotalFileStreamSize is not { } total
        ? null
        : FileSize is not { } current
            ? total
            : total > current ? total - current : 0;
}

internal sealed record SharePointFileVersionInfo
{
    public int ID { get; init; }

    public string VersionLabel { get; init; } = string.Empty;

    public DateTimeOffset? Created { get; init; }

    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public long? Length { get; init; }

    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public long Size { get; init; }

    public bool IsCurrentVersion { get; init; }

    public SharePointUserInfo? CreatedBy { get; init; }
}

internal sealed record SharePointUserInfo
{
    public string Title { get; init; } = string.Empty;
}

/// <summary>
/// A SharePoint collection response. The service has used several shapes over time, so all
/// of them are accepted and normalised here.
/// </summary>
internal sealed record SharePointCollection<T>
{
    public IReadOnlyList<T>? Value { get; init; }

    public IReadOnlyList<T>? Results { get; init; }

    [JsonPropertyName("odata.nextLink")]
    public string? ODataNextLink { get; init; }

    [JsonPropertyName("@odata.nextLink")]
    public string? AnnotatedNextLink { get; init; }

    [JsonPropertyName("__next")]
    public string? LegacyNextLink { get; init; }

    public IReadOnlyList<T> Items => Value ?? Results ?? [];

    public string? NextLink => ODataNextLink ?? AnnotatedNextLink ?? LegacyNextLink;
}

/// <summary>
/// The form digest required by every SharePoint write. Modern SharePoint returns the fields
/// at the root; older responses nest them under <c>GetContextWebInformation</c>.
/// </summary>
internal sealed record SharePointContextInfo
{
    public string FormDigestValue { get; init; } = string.Empty;

    public int FormDigestTimeoutSeconds { get; init; }

    public SharePointContextInfo? GetContextWebInformation { get; init; }

    public SharePointContextInfo Resolve() =>
        string.IsNullOrWhiteSpace(FormDigestValue) && GetContextWebInformation is not null
            ? GetContextWebInformation
            : this;
}

/// <summary>
/// A file and its history. <paramref name="Error"/> is set when the history could not be
/// read, so an empty list is never mistaken for "no previous versions".
/// </summary>
internal sealed record SharePointFileProbe(
    SharePointFileInfo File,
    IReadOnlyList<SharePointFileVersionInfo> Versions,
    string? Error = null);

internal sealed record SharePointFolderContents(
    string Path,
    IReadOnlyList<SharePointFileProbe> Files,
    IReadOnlyList<SharePointFolderInfo> Folders);
