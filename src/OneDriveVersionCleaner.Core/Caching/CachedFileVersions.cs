using OneDriveVersionCleaner.Core.Models;

namespace OneDriveVersionCleaner.Core.Caching;

/// <summary>
/// What one earlier query returned for one file, together with when it was asked and what the
/// file looked like at that moment. Keeping the versions means a file that does not need to be
/// asked about again still appears in the results.
/// </summary>
public sealed record CachedFileVersions
{
    public string DriveId { get; init; } = string.Empty;

    public string ItemId { get; init; } = string.Empty;

    public string CloudPath { get; init; } = string.Empty;

    public string LocalPath { get; init; } = string.Empty;

    public string ServerRelativePath { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string WebUrl { get; init; } = string.Empty;

    /// <summary>When the version list was read from the service.</summary>
    public DateTimeOffset QueriedAtUtc { get; init; }

    /// <summary>The file's last modification as it stood when the versions were read.</summary>
    public DateTimeOffset? FileLastModified { get; init; }

    /// <summary>
    /// The synced local copy's write time when the versions were read. The local survey sees
    /// this clock and not the service's, so reuse is decided against this field; null in an
    /// entry written before the scan became local-first, which simply costs one request.
    /// </summary>
    public DateTimeOffset? LocalLastWriteUtc { get; init; }

    public long? CurrentSize { get; init; }

    public IReadOnlyList<VersionEntry> Versions { get; init; } = [];
}

public sealed record VersionCacheDocument
{
    public const string CurrentSchemaVersion = "1.0";

    public string SchemaVersion { get; init; } = CurrentSchemaVersion;

    public DateTimeOffset SavedAtUtc { get; init; }

    public IReadOnlyList<CachedFileVersions> Files { get; init; } = [];
}
