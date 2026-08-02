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
/// The document library's versioning settings. With versioning off, no file in the library can
/// have history, so the whole scan can be skipped for the cost of reading this once.
/// </summary>
internal sealed record SharePointVersioningInfo
{
    public bool EnableVersioning { get; init; } = true;
}

/// <summary>
/// A folder as the service reports it. Only the library root is read now that the scan finds
/// its files locally, and only its path is used, so nothing else is modelled.
/// </summary>
internal sealed record SharePointFolderInfo
{
    public string ServerRelativeUrl { get; init; } = string.Empty;

    public SharePointResourcePath? ServerRelativePath { get; init; }

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
