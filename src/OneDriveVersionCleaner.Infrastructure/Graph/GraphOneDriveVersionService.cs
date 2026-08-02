using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text.Json;
using OneDriveVersionCleaner.Core.Models;
using OneDriveVersionCleaner.Core.Paths;
using OneDriveVersionCleaner.Core.Services;
using OneDriveVersionCleaner.Infrastructure.Authentication;

namespace OneDriveVersionCleaner.Infrastructure.Graph;

/// <summary>
/// Microsoft Graph adapter for the signed-in user's own OneDrive.
/// It deliberately exposes no operation that can delete a drive item itself.
/// </summary>
public sealed class GraphOneDriveVersionService : IOneDriveVersionService, IDisposable
{
    private const string ItemSelect =
        "id,name,webUrl,size,lastModifiedDateTime,lastModifiedBy,folder,file,parentReference,remoteItem";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly GraphOneDriveOptions _options;
    private readonly MsalTokenProvider _tokenProvider;
    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;
    private readonly string _localOneDriveRoot;
    private readonly SemaphoreSlim _requestGate = new(1, 1);
    private readonly ConcurrentDictionary<string, string> _knownCloudPaths = new(StringComparer.Ordinal);

    public GraphOneDriveVersionService(GraphOneDriveOptions options, HttpClient? httpClient = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(options.Authentication);
        ArgumentOutOfRangeException.ThrowIfNegative(options.RetryCount);

        if (!options.GraphBaseUri.IsAbsoluteUri)
        {
            throw new ArgumentException("GraphBaseUri must be absolute.", nameof(options));
        }

        _options = options;
        _localOneDriveRoot = NormalizeLocalRoot(options.LocalOneDriveRoot);
        _tokenProvider = new MsalTokenProvider(options.Authentication);
        _httpClient = httpClient ?? new HttpClient();
        _ownsHttpClient = httpClient is null;
    }

    public async Task<AccountInfo> SignInAsync(CancellationToken cancellationToken = default)
    {
        var authentication = await _tokenProvider
            .AcquireTokenAsync(forceRefresh: false, cancellationToken)
            .ConfigureAwait(false);
        var user = await GetAsync<GraphUser>(
                "me?$select=id,displayName,userPrincipalName",
                cancellationToken)
            .ConfigureAwait(false);

        return new AccountInfo
        {
            TenantId = authentication.TenantId ?? _options.Authentication.TenantId,
            UserId = user.Id,
            DisplayName = user.DisplayName,
            UserPrincipalName = user.UserPrincipalName,
        };
    }

    public async Task<OneDriveVersionCleaner.Core.Models.DriveInfo> GetDriveAsync(
        CancellationToken cancellationToken = default)
    {
        var drive = await GetAsync<GraphDrive>(
                "me/drive?$select=id,driveType,webUrl",
                cancellationToken)
            .ConfigureAwait(false);

        return new OneDriveVersionCleaner.Core.Models.DriveInfo
        {
            DriveId = drive.Id,
            DriveType = drive.DriveType,
            WebUrl = drive.WebUrl,
            LocalRootPath = _localOneDriveRoot,
        };
    }

    public async Task<DriveItemReference?> ResolveItemAsync(
        string driveId,
        string cloudRelativePath,
        CancellationToken cancellationToken = default)
    {
        ValidateIdentifier(driveId, nameof(driveId));
        var normalizedPath = NormalizeCloudPath(cloudRelativePath);
        var encodedDriveId = Uri.EscapeDataString(driveId);
        var relativeUri = normalizedPath.Length == 0
            ? $"drives/{encodedDriveId}/root?$select={ItemSelect}"
            : $"drives/{encodedDriveId}/root:/{EncodeCloudPath(normalizedPath)}?$select={ItemSelect}";

        GraphDriveItem item;
        try
        {
            item = await GetAsync<GraphDriveItem>(relativeUri, cancellationToken).ConfigureAwait(false);
        }
        catch (GraphRequestException exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        _knownCloudPaths[GetPathCacheKey(driveId, item.Id)] = normalizedPath;

        return new DriveItemReference
        {
            DriveId = driveId,
            ItemId = item.Id,
            Name = item.Name,
            CloudPath = normalizedPath,
            WebUrl = item.WebUrl,
            IsFolder = item.Folder is not null,
        };
    }

    public async IAsyncEnumerable<FileEntry> EnumerateFilesAsync(
        string driveId,
        string rootItemId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ValidateIdentifier(driveId, nameof(driveId));
        ValidateIdentifier(rootItemId, nameof(rootItemId));

        var root = await GetAsync<GraphDriveItem>(
                $"drives/{Uri.EscapeDataString(driveId)}/items/{Uri.EscapeDataString(rootItemId)}?$select={ItemSelect}",
                cancellationToken)
            .ConfigureAwait(false);
        var rootPath = _knownCloudPaths.GetOrAdd(
            GetPathCacheKey(driveId, rootItemId),
            _ => GetCloudPath(root));

        // A remoteItem is a shortcut/reference into another drive. The MVP is intentionally
        // limited to the signed-in user's own OneDrive and must not cross that boundary.
        if (root.RemoteItem is not null)
        {
            yield break;
        }

        if (root.Folder is null)
        {
            if (root.File is not null)
            {
                yield return ToFileEntry(driveId, root, rootPath);
            }

            yield break;
        }

        var pendingFolders = new Queue<(string ItemId, string CloudPath)>();
        pendingFolders.Enqueue((rootItemId, rootPath));

        while (pendingFolders.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var folder = pendingFolders.Dequeue();
            var firstPage =
                $"drives/{Uri.EscapeDataString(driveId)}/items/{Uri.EscapeDataString(folder.ItemId)}/children" +
                $"?$select={ItemSelect}&$top=200";

            await foreach (var item in GetPagesAsync<GraphDriveItem>(firstPage, cancellationToken))
            {
                var cloudPath = CombineCloudPath(folder.CloudPath, item.Name);

                // Shared-folder shortcuts can describe a folder inside their remote facet. Do not
                // enqueue them and do not expose remote files as files owned by this drive.
                if (item.RemoteItem is not null)
                {
                    continue;
                }

                _knownCloudPaths[GetPathCacheKey(driveId, item.Id)] = cloudPath;

                if (item.Folder is not null)
                {
                    pendingFolders.Enqueue((item.Id, cloudPath));
                    continue;
                }

                // remoteItem shortcuts and non-file drive item types are intentionally skipped.
                if (item.File is not null)
                {
                    yield return ToFileEntry(driveId, item, cloudPath);
                }
            }
        }
    }

    public async Task<IReadOnlyList<VersionEntry>> GetVersionsAsync(
        string driveId,
        string itemId,
        CancellationToken cancellationToken = default)
    {
        ValidateIdentifier(driveId, nameof(driveId));
        ValidateIdentifier(itemId, nameof(itemId));

        var versions = new List<GraphDriveItemVersion>();
        var firstPage =
            $"drives/{Uri.EscapeDataString(driveId)}/items/{Uri.EscapeDataString(itemId)}/versions" +
            "?$select=id,lastModifiedDateTime,lastModifiedBy,size";

        await foreach (var version in GetPagesAsync<GraphDriveItemVersion>(firstPage, cancellationToken))
        {
            EnsureVersionHasId(version, "version list");
            versions.Add(version);
        }

        // The documented `current` alias is the authoritative current-version identity. Never
        // infer this from list ordering or timestamps when deletion safety depends on the result.
        var current = await GetCurrentVersionAsync(driveId, itemId, cancellationToken).ConfigureAwait(false);
        var versionsById = new Dictionary<string, GraphDriveItemVersion>(StringComparer.Ordinal);
        foreach (var version in versions)
        {
            versionsById[version.Id] = version;
        }

        // Some workloads omit the current entry from the collection. Always include the separately
        // retrieved value, preferring its metadata when the collection also contains the same ID.
        versionsById[current.Id] = current;

        var ordered = versionsById.Values
            .OrderByDescending(version => string.Equals(version.Id, current.Id, StringComparison.Ordinal))
            .ThenByDescending(version => version.LastModifiedDateTime)
            .ThenByDescending(version => version.Id, StringComparer.Ordinal)
            .ToArray();

        return ordered.Select(version =>
        {
            var isCurrent = string.Equals(version.Id, current.Id, StringComparison.Ordinal);
            return new VersionEntry
            {
                VersionId = version.Id,
                Label = version.Id,
                Created = version.LastModifiedDateTime,
                ModifiedBy = GetIdentityName(version.LastModifiedBy),
                Size = version.Size,
                IsCurrent = isCurrent,
                CanDelete = isCurrent ? false : null,
                RetrievalStatus = RetrievalStatus.Success,
            };
        }).ToArray();
    }

    public async Task<DeleteResult> DeleteVersionAsync(
        string driveId,
        string itemId,
        string versionId,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Authentication.AllowVersionDelete)
        {
            return new DeleteResult
            {
                Status = DeleteResultStatus.NotDeletable,
                Error = "Version deletion is disabled. Re-authentication with the explicit delete option is required.",
            };
        }

        ValidateIdentifier(driveId, nameof(driveId));
        ValidateIdentifier(itemId, nameof(itemId));
        ValidateIdentifier(versionId, nameof(versionId));

        IReadOnlyList<VersionEntry> versions;
        try
        {
            versions = await GetVersionsAsync(driveId, itemId, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (exception is GraphRequestException or
            HttpRequestException or IOException or InvalidDataException or JsonException or
            Microsoft.Identity.Client.MsalException or OperationCanceledException)
        {
            // The version DELETE endpoint is experimental. Refuse to call it unless Graph has
            // positively identified the current version in this same operation.
            return new DeleteResult
            {
                Status = DeleteResultStatus.NotDeletable,
                Error = $"Version deletion was refused because the current version could not be verified: {exception.Message}",
            };
        }

        var target = versions.FirstOrDefault(version =>
            string.Equals(version.VersionId, versionId, StringComparison.Ordinal));

        if (target is null)
        {
            return new DeleteResult { Status = DeleteResultStatus.AlreadyDeleted };
        }

        if (target.IsCurrent || target.CanDelete == false)
        {
            return new DeleteResult
            {
                Status = DeleteResultStatus.NotDeletable,
                Error = "The current version is always protected.",
            };
        }

        try
        {
            var relativeUri =
                $"drives/{Uri.EscapeDataString(driveId)}/items/{Uri.EscapeDataString(itemId)}/versions/" +
                Uri.EscapeDataString(versionId);
            using var response = await SendAsync(HttpMethod.Delete, relativeUri, cancellationToken)
                .ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                return new DeleteResult { Status = DeleteResultStatus.Deleted };
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return new DeleteResult { Status = DeleteResultStatus.AlreadyDeleted };
            }

            var error = await ReadErrorAsync(response, cancellationToken).ConfigureAwait(false);
            return new DeleteResult
            {
                Status = MapDeleteFailure(response.StatusCode),
                Error = error.Message,
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (exception is HttpRequestException or IOException or
            Microsoft.Identity.Client.MsalException or OperationCanceledException)
        {
            return new DeleteResult
            {
                Status = exception is Microsoft.Identity.Client.MsalException
                    ? DeleteResultStatus.PermanentError
                    : DeleteResultStatus.TransientError,
                Error = exception.Message,
            };
        }
    }

    public Task SignOutAsync(CancellationToken cancellationToken = default) =>
        _tokenProvider.SignOutAsync(cancellationToken);

    public void Dispose()
    {
        _requestGate.Dispose();
        if (_ownsHttpClient)
        {
            _httpClient.Dispose();
        }
    }

    private async IAsyncEnumerable<T> GetPagesAsync<T>(
        string firstPage,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        string? nextPage = firstPage;
        while (!string.IsNullOrWhiteSpace(nextPage))
        {
            var page = await GetAsync<GraphCollection<T>>(nextPage, cancellationToken).ConfigureAwait(false);
            foreach (var value in page.Value)
            {
                yield return value;
            }

            // The service owns this URL. It must be followed verbatim without rebuilding query options.
            nextPage = page.NextLink;
        }
    }

    private async Task<T> GetAsync<T>(string uri, CancellationToken cancellationToken)
    {
        using var response = await SendAsync(HttpMethod.Get, uri, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw await CreateExceptionAsync(response, cancellationToken).ConfigureAwait(false);
        }

        await using var content = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        var value = await JsonSerializer.DeserializeAsync<T>(content, JsonOptions, cancellationToken)
            .ConfigureAwait(false);
        return value ?? throw new InvalidDataException("Microsoft Graph returned an empty JSON response.");
    }

    private async Task<GraphDriveItemVersion> GetCurrentVersionAsync(
        string driveId,
        string itemId,
        CancellationToken cancellationToken)
    {
        var current = await GetAsync<GraphDriveItemVersion>(
                $"drives/{Uri.EscapeDataString(driveId)}/items/{Uri.EscapeDataString(itemId)}/versions/current" +
                "?$select=id,lastModifiedDateTime,lastModifiedBy,size",
                cancellationToken)
            .ConfigureAwait(false);
        EnsureVersionHasId(current, "current version response");
        return current;
    }

    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        string uri,
        CancellationToken cancellationToken)
    {
        await _requestGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var forcedRefresh = false;
            for (var attempt = 0; ; attempt++)
            {
                var authentication = await _tokenProvider
                    .AcquireTokenAsync(forcedRefresh, cancellationToken)
                    .ConfigureAwait(false);
                using var request = new HttpRequestMessage(method, ResolveUri(uri));
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authentication.AccessToken);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                HttpResponseMessage response;
                try
                {
                    response = await _httpClient
                        .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (Exception exception) when (IsRetryableTransportFailure(exception, cancellationToken))
                {
                    if (attempt >= _options.RetryCount)
                    {
                        throw;
                    }

                    await Task.Delay(GetBackoffDelay(attempt), cancellationToken).ConfigureAwait(false);
                    continue;
                }

                if (response.StatusCode == HttpStatusCode.Unauthorized && !forcedRefresh)
                {
                    response.Dispose();
                    forcedRefresh = true;
                    continue;
                }

                if (!IsRetryable(response.StatusCode) || attempt >= _options.RetryCount)
                {
                    return response;
                }

                var delay = GetRetryDelay(response, attempt);
                response.Dispose();
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            _requestGate.Release();
        }
    }

    private Uri ResolveUri(string uri)
    {
        if (Uri.TryCreate(uri, UriKind.Absolute, out var absolute))
        {
            if (!string.Equals(absolute.Scheme, _options.GraphBaseUri.Scheme, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(absolute.Host, _options.GraphBaseUri.Host, StringComparison.OrdinalIgnoreCase) ||
                absolute.Port != _options.GraphBaseUri.Port)
            {
                throw new InvalidOperationException("Refusing an unexpected pagination host.");
            }

            return absolute;
        }

        return new Uri(_options.GraphBaseUri, uri);
    }

    private static bool IsRetryable(HttpStatusCode statusCode) =>
        statusCode is HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests ||
        (int)statusCode is >= 500 and <= 599;

    private static TimeSpan GetRetryDelay(HttpResponseMessage response, int attempt)
    {
        if (response.Headers.RetryAfter?.Delta is { } delta && delta >= TimeSpan.Zero)
        {
            return delta;
        }

        if (response.Headers.RetryAfter?.Date is { } date)
        {
            var serverDelay = date - DateTimeOffset.UtcNow;
            if (serverDelay > TimeSpan.Zero)
            {
                return serverDelay;
            }
        }

        return GetBackoffDelay(attempt);
    }

    private static bool IsRetryableTransportFailure(
        Exception exception,
        CancellationToken cancellationToken) =>
        !cancellationToken.IsCancellationRequested &&
        exception is HttpRequestException or IOException or OperationCanceledException;

    private static TimeSpan GetBackoffDelay(int attempt)
    {
        var seconds = Math.Min(60, Math.Pow(2, attempt + 1)) + Random.Shared.NextDouble();
        return TimeSpan.FromSeconds(seconds);
    }

    private FileEntry ToFileEntry(string driveId, GraphDriveItem item, string cloudPath) => new()
    {
        DriveId = driveId,
        ItemId = item.Id,
        Name = item.Name,
        CloudPath = cloudPath,
        LocalPath = GetLocalPath(cloudPath),
        WebUrl = item.WebUrl,
        CurrentSize = item.Size,
        LastModified = item.LastModifiedDateTime,
        ModifiedBy = GetIdentityName(item.LastModifiedBy),
        FileType = item.File?.MimeType,
        RetrievalStatus = RetrievalStatus.Success,
    };

    private string GetLocalPath(string cloudPath)
    {
        if (_localOneDriveRoot.Length == 0)
        {
            return string.Empty;
        }

        try
        {
            var segments = LocalPathMapper.SplitCloudRelativePath(cloudPath);
            if (segments.Any(segment => segment.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0))
            {
                return string.Empty;
            }

            var candidate = segments.Aggregate(_localOneDriveRoot, Path.Combine);
            var fullPath = Path.GetFullPath(candidate);
            var relativePath = Path.GetRelativePath(_localOneDriveRoot, fullPath);
            if (Path.IsPathRooted(relativePath) ||
                relativePath == ".." ||
                relativePath.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal) ||
                relativePath.StartsWith($"..{Path.AltDirectorySeparatorChar}", StringComparison.Ordinal))
            {
                return string.Empty;
            }

            return fullPath;
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or NotSupportedException)
        {
            // A cloud name may not have a one-to-one representation below the configured sync root.
            return string.Empty;
        }
    }

    private static string NormalizeLocalRoot(string configuredRoot)
    {
        if (string.IsNullOrWhiteSpace(configuredRoot))
        {
            return string.Empty;
        }

        var expanded = Environment.ExpandEnvironmentVariables(configuredRoot.Trim());
        if (!Path.IsPathFullyQualified(expanded))
        {
            throw new ArgumentException("LocalOneDriveRoot must be an absolute path.", nameof(configuredRoot));
        }

        return Path.TrimEndingDirectorySeparator(Path.GetFullPath(expanded));
    }

    private static string? GetIdentityName(GraphIdentitySet? identitySet) =>
        identitySet?.User?.DisplayName ??
        identitySet?.User?.Email ??
        identitySet?.Application?.DisplayName;

    private static string GetCloudPath(GraphDriveItem item)
    {
        var parentPath = item.ParentReference?.Path;
        if (string.IsNullOrEmpty(parentPath))
        {
            return item.Name;
        }

        var rootMarker = parentPath.IndexOf("root:", StringComparison.OrdinalIgnoreCase);
        var relativeParent = rootMarker >= 0
            ? parentPath[(rootMarker + "root:".Length)..].Trim('/')
            : string.Empty;

        return CombineCloudPath(relativeParent, item.Name);
    }

    private static string CombineCloudPath(string parent, string child) =>
        string.IsNullOrEmpty(parent) ? child : $"{parent.TrimEnd('/')}/{child}";

    private static string NormalizeCloudPath(string path) => string.Join(
        '/',
        LocalPathMapper.SplitCloudRelativePath((path ?? string.Empty).Replace('\\', '/').Trim('/')));

    private static string EncodeCloudPath(string path) => LocalPathMapper.EncodeCloudRelativePath(path);

    private static string GetPathCacheKey(string driveId, string itemId) => $"{driveId}\n{itemId}";

    private static void ValidateIdentifier(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("The identifier must not be empty.", parameterName);
        }
    }

    private static void EnsureVersionHasId(GraphDriveItemVersion version, string responseDescription)
    {
        if (string.IsNullOrWhiteSpace(version.Id))
        {
            throw new InvalidDataException($"Microsoft Graph returned a {responseDescription} without an ID.");
        }
    }

    private static DeleteResultStatus MapDeleteFailure(HttpStatusCode statusCode) => statusCode switch
    {
        HttpStatusCode.Forbidden => DeleteResultStatus.AccessDenied,
        HttpStatusCode.BadRequest or HttpStatusCode.MethodNotAllowed or HttpStatusCode.NotImplemented =>
            DeleteResultStatus.NotDeletable,
        _ when IsRetryable(statusCode) => DeleteResultStatus.TransientError,
        _ => DeleteResultStatus.PermanentError,
    };

    private static async Task<GraphRequestException> CreateExceptionAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var error = await ReadErrorAsync(response, cancellationToken).ConfigureAwait(false);
        return new GraphRequestException(response.StatusCode, error.Code, error.Message);
    }

    private static async Task<(string? Code, string Message)> ReadErrorAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            var envelope = await JsonSerializer.DeserializeAsync<GraphErrorEnvelope>(
                    stream,
                    JsonOptions,
                    cancellationToken)
                .ConfigureAwait(false);
            var code = envelope?.Error?.Code;
            var message = envelope?.Error?.Message;
            if (!string.IsNullOrWhiteSpace(message))
            {
                return (code, $"Microsoft Graph returned {(int)response.StatusCode} ({code ?? "unknown"}): {message}");
            }
        }
        catch (JsonException)
        {
            // Fall back to a status-only message; never echo arbitrary HTML or token-bearing content.
        }

        return (null, $"Microsoft Graph returned {(int)response.StatusCode} ({response.ReasonPhrase}).");
    }
}
