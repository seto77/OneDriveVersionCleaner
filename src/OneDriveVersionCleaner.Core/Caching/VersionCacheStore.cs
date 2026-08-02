using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OneDriveVersionCleaner.Core.Caching;

/// <summary>
/// Persists earlier version-query answers, one file per account, so a later run can reuse them
/// instead of asking the service again.
/// </summary>
public sealed class VersionCacheStore
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    private readonly string _directory;

    public VersionCacheStore(string? directory = null)
    {
        _directory = string.IsNullOrWhiteSpace(directory)
            ? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "OneDriveVersionCleaner",
                "Cache")
            : Path.GetFullPath(Environment.ExpandEnvironmentVariables(directory));
    }

    public string GetCachePath(string accountKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountKey);
        var safeKey = string.Concat(accountKey.Select(character =>
            Path.GetInvalidFileNameChars().Contains(character) ? '_' : character));
        return Path.Combine(_directory, $"versions-{safeKey}.json");
    }

    /// <summary>
    /// Loads the cached answers keyed by item id. A missing, unreadable or unrecognised file
    /// yields an empty cache: losing the cache only costs requests, while trusting a file we
    /// cannot parse would be worse.
    /// </summary>
    public async Task<IReadOnlyDictionary<string, CachedFileVersions>> LoadAsync(
        string accountKey,
        CancellationToken cancellationToken = default)
    {
        var path = GetCachePath(accountKey);
        if (!File.Exists(path))
        {
            return new Dictionary<string, CachedFileVersions>(StringComparer.Ordinal);
        }

        try
        {
            var json = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
            var document = JsonSerializer.Deserialize<VersionCacheDocument>(json, JsonOptions);
            if (document is null ||
                !string.Equals(
                    document.SchemaVersion,
                    VersionCacheDocument.CurrentSchemaVersion,
                    StringComparison.Ordinal))
            {
                return new Dictionary<string, CachedFileVersions>(StringComparer.Ordinal);
            }

            var entries = new Dictionary<string, CachedFileVersions>(StringComparer.Ordinal);
            foreach (var file in document.Files.Where(file => !string.IsNullOrEmpty(file.ItemId)))
            {
                entries[file.ItemId] = file;
            }

            return entries;
        }
        catch (Exception exception) when (exception is IOException or
            UnauthorizedAccessException or JsonException)
        {
            return new Dictionary<string, CachedFileVersions>(StringComparer.Ordinal);
        }
    }

    /// <summary>
    /// Writes the cache through a temporary file, so an interrupted save cannot leave a
    /// half-written cache behind.
    /// </summary>
    public async Task SaveAsync(
        string accountKey,
        IEnumerable<CachedFileVersions> files,
        DateTimeOffset savedAtUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(files);

        var path = GetCachePath(accountKey);
        var temporaryPath = path + ".tmp";
        var document = new VersionCacheDocument
        {
            SavedAtUtc = savedAtUtc.ToUniversalTime(),
            Files = files.ToArray(),
        };

        Directory.CreateDirectory(_directory);
        await File.WriteAllTextAsync(
            temporaryPath,
            JsonSerializer.Serialize(document, JsonOptions),
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            cancellationToken).ConfigureAwait(false);
        File.Move(temporaryPath, path, overwrite: true);
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
