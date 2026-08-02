using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Text.Json;
using Microsoft.Web.WebView2.Core;
using OneDriveVersionCleaner.Core.Models;
using OneDriveVersionCleaner.Core.Retry;

namespace OneDriveVersionCleaner.App;

/// <summary>
/// Issues same-origin SharePoint REST requests from the page the user signed in to.
/// Authentication cookies never leave WebView2 and are never copied into application storage.
/// The session owns the sign-in window and keeps it alive: the requests below run inside that
/// page, so disposing it when the dialog closes would throw the signed-in session away.
/// </summary>
internal sealed class SharePointWebSession : IDisposable
{
    /// <summary>
    /// NFR-PERF-002: SharePoint Online throttles a single user aggressively, and a full
    /// library scan is tens of thousands of requests, so this stays deliberately small.
    /// </summary>
    private const int MaximumConcurrentRequests = 2;

    /// <summary>
    /// How many times one request waits out a throttle before giving up. Kept small: a scan
    /// issues thousands of requests, so patience per request multiplies into hours.
    /// </summary>
    private const int MaximumThrottleRetries = 3;

    /// <summary>Upper bound on a single wait, including a server-supplied Retry-After.</summary>
    private const int MaximumRetryAfterSeconds = 30;

    /// <summary>SharePoint's maximum rows per list query.</summary>
    private const int ListItemPageSize = 5000;

    /// <summary>
    /// Total time an operation may spend waiting out throttling before the session gives up
    /// entirely. Without this the retries alone keep a throttled tenant grinding for hours
    /// while the UI shows nothing but a wait message.
    /// </summary>
    private static readonly TimeSpan ThrottleBudget = TimeSpan.FromMinutes(2);

    /// <summary>
    /// How many per-file version requests are started at once. A small multiple of the
    /// concurrency limit keeps the gate saturated without materialising a task per file.
    /// </summary>
    private const int ProbeBatchSize = MaximumConcurrentRequests * 8;

    private const string VersionSelect =
        "ID,VersionLabel,Created,Length,Size,IsCurrentVersion";

    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(30);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly Uri _siteUri;
    private readonly Uri _serviceEndpointUri;
    private readonly SharePointSignInForm _signInForm;
    private readonly SemaphoreSlim _requestGate = new(MaximumConcurrentRequests, MaximumConcurrentRequests);
    private readonly ConcurrentDictionary<string, TaskCompletionSource<BrowserResponse>> _pending = new();
    private readonly RetryPolicyEvaluator _retryPolicy = new();
    private readonly Lock _cooldownLock = new();
    private DateTimeOffset _throttledUntilUtc;
    private TimeSpan _throttleWaited;
    private bool _throttleBudgetExhausted;
    private int _throttleEvents;
    private bool _verified;
    private string? _formDigest;
    private DateTimeOffset _formDigestExpiresAtUtc;

    public SharePointWebSession(Uri siteUri, Uri serviceEndpointUri)
    {
        ArgumentNullException.ThrowIfNull(siteUri);
        ArgumentNullException.ThrowIfNull(serviceEndpointUri);
        if (!SharePointOrigin.IsHttps(siteUri) ||
            !SharePointOrigin.IsHttps(serviceEndpointUri) ||
            !string.Equals(siteUri.Host, serviceEndpointUri.Host, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("SharePoint site and API endpoint must use HTTPS on the same host.");
        }

        _siteUri = siteUri;
        _serviceEndpointUri = serviceEndpointUri;
        _signInForm = new SharePointSignInForm(siteUri, () => !_pending.IsEmpty, VerifySiteTitleAsync);
        _signInForm.WebMessageReceived += WebMessageReceived;
        _signInForm.BrowserFailed += SignInFormOnBrowserFailed;
        _signInForm.SignedOut += SignInFormOnSignedOut;
    }

    public bool IsVerified => _verified;

    /// <summary>
    /// Shows the sign-in window. The window is kept afterwards: every request below runs inside
    /// the page it leaves behind, so it is only torn down when the session is.
    /// </summary>
    public DialogResult ShowDialog(IWin32Window owner) => _signInForm.ShowDialog(owner);

    /// <summary>Raised when a request has to wait out SharePoint throttling.</summary>
    public event EventHandler<TimeSpan>? ThrottleWaitStarted;

    /// <summary>How many times this session has been throttled since it was created.</summary>
    public int ThrottleEventCount
    {
        get
        {
            lock (_cooldownLock)
            {
                return _throttleEvents;
            }
        }
    }

    /// <summary>Confirms the sign-in on the window's behalf; it cannot make requests itself.</summary>
    private async Task<string> VerifySiteTitleAsync(CancellationToken cancellationToken) =>
        (await VerifyAsync(cancellationToken)).Title;

    private void SignInFormOnBrowserFailed(object? sender, string reason) =>
        FailPendingRequests(new InvalidOperationException(reason));

    /// <summary>
    /// The page is being replaced, so the confirmed sign-in no longer describes what the
    /// browser is showing.
    /// </summary>
    private void SignInFormOnSignedOut(object? sender, EventArgs eventArgs) => _verified = false;

    public async Task<SharePointSiteInfo> VerifyAsync(CancellationToken cancellationToken)
    {
        var endpoint = BuildApiUri("web?$select=Title,Url");
        var response = await SendGetAsync(endpoint, cancellationToken);
        EnsureSuccess(response, "SharePointへのログイン確認");
        var result = DeserializeBody<SharePointSiteInfo>(response, "SharePointサイト情報");
        if (string.IsNullOrWhiteSpace(result.Url))
        {
            throw new InvalidDataException("SharePointサイトURLを取得できませんでした。");
        }

        _verified = true;
        return result;
    }

    public async Task<SharePointFolderInfo> GetDefaultDocumentLibraryRootAsync(
        CancellationToken cancellationToken)
    {
        EnsureVerified();
        var result = await GetSingleAsync<SharePointFolderInfo>(
            BuildApiUri("web/DefaultDocumentLibrary/RootFolder"),
            "OneDriveルートの取得",
            cancellationToken);
        if (string.IsNullOrWhiteSpace(result.Path))
        {
            throw new InvalidDataException("OneDriveのサーバー相対URLを取得できませんでした。");
        }

        return result;
    }

    /// <summary>
    /// Reads the whole document library as list items, in pages of up to 5,000.
    /// This is what makes a large library affordable: one request covers thousands of files
    /// and already reports how much each file's versions occupy.
    /// Returns null when the query is not usable here, so the caller can walk folders instead.
    /// </summary>
    public async Task<IReadOnlyList<SharePointListItemInfo>?> TryEnumerateLibraryItemsAsync(
        IProgress<int>? progress,
        CancellationToken cancellationToken)
    {
        EnsureVerified();

        // Most specific projection first. Dropping the current size only costs precision: the
        // total then includes the live file, which over-estimates history and therefore asks
        // for a few more detail reads than strictly needed - never fewer.
        foreach (var select in (string[])
                 [
                     "FSObjType,FileRef,FileLeafRef,File_x0020_Size,SMTotalFileStreamSize,Modified,UIVersionString",
                     "FSObjType,FileRef,FileLeafRef,SMTotalFileStreamSize,Modified,UIVersionString",
                 ])
        {
            var items = await TryReadLibraryItemsAsync(select, progress, cancellationToken);
            if (items is not null)
            {
                return items;
            }
        }

        return null;
    }

    private async Task<IReadOnlyList<SharePointListItemInfo>?> TryReadLibraryItemsAsync(
        string select,
        IProgress<int>? progress,
        CancellationToken cancellationToken)
    {
        var items = new List<SharePointListItemInfo>();
        var next = BuildApiUri($"web/DefaultDocumentLibrary/items?$select={select}&$top={ListItemPageSize}");

        try
        {
            while (next is not null)
            {
                var page = next;
                var response = await SendGetAsync(page, cancellationToken);
                EnsureSuccess(response, "ライブラリ項目の取得");
                var collection = DeserializeBody<SharePointCollection<SharePointListItemInfo>>(
                    response,
                    "ライブラリ項目の取得");
                items.AddRange(collection.Items);
                progress?.Report(items.Count);
                next = ResolveNextLink(page, collection.NextLink);
            }

            return items;
        }
        catch (Exception exception) when (exception is InvalidOperationException or InvalidDataException)
        {
            return null;
        }
    }

    /// <summary>
    /// Runs a read against the path form of a resource and, if that is rejected, against its
    /// unique id. A name that cannot be round-tripped through a URL, or a path that is simply
    /// too long, fails before the OData layer ever sees it; the id form is short and carries
    /// no characters of its own.
    /// </summary>
    private static async Task<T> ReadWithIdFallbackAsync<T>(
        Func<string, Task<T>> read,
        string pathResource,
        string? idResource)
    {
        try
        {
            return await read(pathResource);
        }
        catch (Exception exception) when (idResource is not null &&
            exception is InvalidOperationException or InvalidDataException)
        {
            return await read(idResource);
        }
    }

    /// <summary>
    /// Reads the library's versioning settings. Falls back to "assume everything is versioned"
    /// when they cannot be read, so an unreadable setting never causes history to be skipped.
    /// </summary>
    public async Task<SharePointVersioningInfo> GetVersioningInfoAsync(CancellationToken cancellationToken)
    {
        EnsureVerified();
        try
        {
            return await GetSingleAsync<SharePointVersioningInfo>(
                BuildApiUri("web/DefaultDocumentLibrary?$select=EnableVersioning,EnableMinorVersions"),
                "ライブラリのバージョン設定の取得",
                cancellationToken);
        }
        catch (Exception exception) when (exception is InvalidOperationException or
            InvalidDataException or JsonException or TimeoutException or SharePointThrottledException)
        {
            // Assume everything is versioned. Being wrong here only costs requests; the
            // opposite mistake would silently skip real history. This call is an optimisation,
            // so it must not spend the budget the scan itself needs.
            ResetThrottleState();
            return new SharePointVersioningInfo();
        }
    }

    /// <summary>
    /// Reads one folder: its files with their version history, and its subfolders.
    /// The two collections are fetched concurrently; the shared request gate keeps the
    /// total number of in-flight requests bounded.
    /// </summary>
    public async Task<SharePointFolderContents> GetFolderContentsAsync(
        SharePointFolderInfo folder,
        SharePointVersioningInfo versioning,
        CancellationToken cancellationToken)
    {
        EnsureVerified();
        ArgumentNullException.ThrowIfNull(folder);
        ArgumentNullException.ThrowIfNull(versioning);

        var byPath = FolderResource(folder.Path);
        var byId = folder.UniqueId == Guid.Empty ? null : FolderResourceById(folder.UniqueId);

        var filesTask = ReadWithIdFallbackAsync(
            resource => GetFilesWithVersionsAsync(resource, versioning, cancellationToken),
            byPath,
            byId);
        var foldersTask = ReadWithIdFallbackAsync(
            resource => GetCollectionAsync<SharePointFolderInfo>(
                BuildApiUri($"{resource}/Folders"),
                "サブフォルダーの取得",
                cancellationToken),
            byPath,
            byId);

        await Task.WhenAll(filesTask, foldersTask);
        return new SharePointFolderContents(
            folder.Path,
            await filesTask,
            await foldersTask);
    }

    /// <summary>
    /// Reads a file and its history. <paramref name="uniqueId"/> addresses the file when its
    /// path cannot be expressed in a URL; pass <see cref="Guid.Empty"/> when it is not known.
    /// </summary>
    public async Task<SharePointFileProbe> GetFileVersionsAsync(
        string serverRelativePath,
        Guid uniqueId,
        CancellationToken cancellationToken)
    {
        EnsureVerified();
        ArgumentException.ThrowIfNullOrWhiteSpace(serverRelativePath);

        var fileTask = ReadWithIdFallbackAsync(
            resource => GetSingleAsync<SharePointFileInfo>(
                BuildApiUri(resource),
                "対象ファイルの取得",
                cancellationToken),
            FileResource(serverRelativePath),
            uniqueId == Guid.Empty ? null : FileResourceById(uniqueId));
        var versionsTask = GetVersionsAsync(serverRelativePath, uniqueId, cancellationToken);

        await Task.WhenAll(fileTask, versionsTask);
        return new SharePointFileProbe(await fileTask, await versionsTask);
    }

    /// <summary>
    /// Moves one historical version to the recycle bin. The caller is responsible for having
    /// excluded the current version; this method additionally refuses anything whose
    /// identifier is not a SharePoint version id, so the synthesised current-version entry
    /// can never be sent. There is no permanent-delete path.
    /// </summary>
    public async Task<DeleteResult> RecycleFileVersionAsync(
        string serverRelativePath,
        Guid uniqueId,
        string versionId,
        CancellationToken cancellationToken)
    {
        EnsureVerified();
        ArgumentException.ThrowIfNullOrWhiteSpace(serverRelativePath);

        if (!int.TryParse(versionId, NumberStyles.None, CultureInfo.InvariantCulture, out var numericVersionId) ||
            numericVersionId <= 0)
        {
            return new DeleteResult
            {
                Status = DeleteResultStatus.NotDeletable,
                Error = $"SharePointのバージョンIDとして扱えません: {versionId}",
            };
        }

        var recycle = $"/Versions/RecycleByID(vid={numericVersionId.ToString(CultureInfo.InvariantCulture)})";

        try
        {
            var response = await ReadWithIdFallbackAsync(
                resource => PostWithDigestAsync(BuildApiUri(resource + recycle), cancellationToken),
                FileResource(serverRelativePath),
                uniqueId == Guid.Empty ? null : FileResourceById(uniqueId));
            return response.Ok
                ? new DeleteResult { Status = DeleteResultStatus.Deleted }
                : new DeleteResult
                {
                    Status = MapDeleteFailure(response.Status),
                    Error = BuildMutationError(response),
                };
        }
        catch (TimeoutException exception)
        {
            return new DeleteResult
            {
                Status = DeleteResultStatus.TransientError,
                Error = exception.Message,
            };
        }
    }

    /// <summary>
    /// Ends the session and, with it, the signed-in page. Nothing else may dispose the sign-in
    /// window: every request runs inside that page.
    /// </summary>
    public void Dispose()
    {
        _signInForm.WebMessageReceived -= WebMessageReceived;
        _signInForm.BrowserFailed -= SignInFormOnBrowserFailed;
        _signInForm.SignedOut -= SignInFormOnSignedOut;
        FailPendingRequests(new ObjectDisposedException(nameof(SharePointWebSession)));

        // _requestGate is deliberately not disposed: its AvailableWaitHandle is never used,
        // so disposal buys nothing and would make the Release in an in-flight SendAsync throw.
        _signInForm.Dispose();
    }

    /// <summary>
    /// Lists a folder's files together with their versions.
    /// The history is always read from each file's own paged Versions collection. An inlined
    /// <c>$expand=Versions</c> would be one round trip fewer, but a server-truncated inline
    /// collection is indistinguishable from a complete one, and under-reporting history is
    /// exactly the failure this tool must not have.
    /// </summary>
    private async Task<IReadOnlyList<SharePointFileProbe>> GetFilesWithVersionsAsync(
        string folder,
        SharePointVersioningInfo versioning,
        CancellationToken cancellationToken)
    {
        // No $select: the default projection already carries every field the scan needs, and
        // it cannot fail over a property name the tenant's SharePoint build does not expose.
        var files = await GetCollectionAsync<SharePointFileInfo>(
            BuildApiUri($"{folder}/Files"),
            "ファイル一覧の取得",
            cancellationToken);

        // Started in batches rather than all at once: only a few requests can be in flight
        // anyway, and a folder with tens of thousands of files would otherwise allocate one
        // pending task per file up front.
        var probes = new List<SharePointFileProbe>(files.Count);
        foreach (var batch in files.Chunk(ProbeBatchSize))
        {
            probes.AddRange(await Task.WhenAll(
                batch.Select(file => ProbeAsync(file, versioning, cancellationToken))));
        }

        return probes;
    }

    /// <summary>
    /// Attaches the history to a listed file. A failure is reported on the probe instead of
    /// failing the folder, so one unreadable file cannot hide the rest of the library
    /// (NFR-REL-003), and it is never mistaken for a file without history.
    /// </summary>
    private async Task<SharePointFileProbe> ProbeAsync(
        SharePointFileInfo file,
        SharePointVersioningInfo versioning,
        CancellationToken cancellationToken)
    {
        // Skipping a request the library's own settings prove would return nothing is what
        // keeps a 40,000 file scan from being 40,000 requests.
        if (!versioning.MayHaveHistory(file))
        {
            return new SharePointFileProbe(file, []);
        }

        try
        {
            return new SharePointFileProbe(
                file,
                await ReadWithIdFallbackAsync(
                    resource => ReadVersionsAsync(resource, cancellationToken),
                    FileResource(file.Path),
                    file.UniqueId == Guid.Empty ? null : FileResourceById(file.UniqueId)));
        }
        catch (Exception exception) when (exception is InvalidOperationException or
            InvalidDataException or JsonException or TimeoutException)
        {
            return new SharePointFileProbe(file, [], exception.Message);
        }
    }

    private Task<IReadOnlyList<SharePointFileVersionInfo>> GetVersionsAsync(
        string serverRelativePath,
        Guid uniqueId,
        CancellationToken cancellationToken) => ReadWithIdFallbackAsync(
        resource => ReadVersionsAsync(resource, cancellationToken),
        FileResource(serverRelativePath),
        uniqueId == Guid.Empty ? null : FileResourceById(uniqueId));

    private Task<IReadOnlyList<SharePointFileVersionInfo>> ReadVersionsAsync(
        string fileResource,
        CancellationToken cancellationToken) => GetCollectionAsync<SharePointFileVersionInfo>(
        BuildApiUri($"{fileResource}/Versions?$select={VersionSelect},CreatedBy/Title&$expand=CreatedBy"),
        "バージョン履歴の取得",
        cancellationToken);

    private async Task<T> GetSingleAsync<T>(Uri endpoint, string operation, CancellationToken cancellationToken)
    {
        var response = await SendGetAsync(endpoint, cancellationToken);
        EnsureSuccess(response, operation);
        return DeserializeBody<T>(response, operation);
    }

    private async Task<IReadOnlyList<T>> GetCollectionAsync<T>(
        Uri endpoint,
        string operation,
        CancellationToken cancellationToken)
    {
        var items = new List<T>();
        var next = (Uri?)endpoint;
        while (next is not null)
        {
            var page = next;
            var response = await SendGetAsync(page, cancellationToken);
            EnsureSuccess(response, operation);
            var collection = DeserializeBody<SharePointCollection<T>>(response, operation);
            items.AddRange(collection.Items);
            next = ResolveNextLink(page, collection.NextLink);
        }

        return items;
    }

    /// <summary>
    /// Resolves the link to the next page against the page it came from. SharePoint returns
    /// the continuation as an absolute URL, a relative reference, or a bare
    /// <c>?$skiptoken=...</c>; resolving against <paramref name="page"/> is the only base that
    /// is correct for all three. A link that cannot be resolved or validated raises rather
    /// than ending the loop, because an incomplete listing must never look like a complete one.
    /// </summary>
    private Uri? ResolveNextLink(Uri page, string? nextLink)
    {
        if (string.IsNullOrWhiteSpace(nextLink))
        {
            return null;
        }

        if (!Uri.TryCreate(page, nextLink, out var uri))
        {
            throw new InvalidDataException(
                $"SharePointの次ページURLを解釈できませんでした: {nextLink}");
        }

        // The service owns this URL, but it still has to stay inside the personal site.
        ValidateApiEndpoint(uri);
        return uri;
    }

    private Task<BrowserResponse> SendGetAsync(Uri endpoint, CancellationToken cancellationToken) =>
        SendAsync("GET", endpoint, formDigest: null, cancellationToken);

    /// <summary>
    /// Sends one request, retrying while SharePoint reports throttling. All callers share one
    /// cooldown, so a throttled tenant is not hammered by the other in-flight requests.
    /// </summary>
    private async Task<BrowserResponse> SendAsync(
        string method,
        Uri endpoint,
        string? formDigest,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            ThrowIfThrottleBudgetExhausted();
            await WaitForCooldownAsync(cancellationToken);
            var response = await SendOnceAsync(method, endpoint, formDigest, cancellationToken);
            if (!IsThrottled(response))
            {
                ResetThrottleBudget();
                return response;
            }

            var decision = _retryPolicy.EvaluateHttpFailure(
                HttpStatusCode.TooManyRequests,
                attempt,
                MaximumThrottleRetries,
                retryAfter: response.RetryAfterSeconds > 0
                    ? TimeSpan.FromSeconds(Math.Min(response.RetryAfterSeconds, MaximumRetryAfterSeconds))
                    : null,
                baseDelay: TimeSpan.FromSeconds(2),
                maximumBackoff: TimeSpan.FromSeconds(MaximumRetryAfterSeconds));

            if (!decision.ShouldRetry)
            {
                ExhaustThrottleBudget();
                ThrowIfThrottleBudgetExhausted();
            }

            EnterCooldown(decision.Delay);
        }
    }

    /// <summary>
    /// Once the session has spent its whole waiting budget, every further request fails
    /// immediately. Otherwise each of the thousands of queued requests would serve out its own
    /// retries, and an operation that cannot succeed would appear to hang instead of ending.
    /// </summary>
    private void ThrowIfThrottleBudgetExhausted()
    {
        lock (_cooldownLock)
        {
            if (!_throttleBudgetExhausted)
            {
                return;
            }
        }

        throw new SharePointThrottledException(
            "SharePointの要求制限が続いているため中断しました。待機して再試行しましたが解消しませんでした。");
    }

    private void ExhaustThrottleBudget()
    {
        lock (_cooldownLock)
        {
            _throttleBudgetExhausted = true;
        }
    }

    private void ResetThrottleBudget()
    {
        lock (_cooldownLock)
        {
            _throttleWaited = TimeSpan.Zero;
        }
    }

    /// <summary>
    /// Clears the throttle state so a new operation starts with a full budget.
    /// </summary>
    public void ResetThrottleState()
    {
        lock (_cooldownLock)
        {
            _throttleWaited = TimeSpan.Zero;
            _throttleBudgetExhausted = false;
            _throttledUntilUtc = default;
            _throttleEvents = 0;
        }
    }

    /// <summary>
    /// SharePoint reports throttling either with a status code, or by redirecting the request
    /// to a throttle page, which arrives here as a non-JSON response from Throttle.htm.
    /// </summary>
    private static bool IsThrottled(BrowserResponse response) =>
        response.Status is 429 or 503 ||
        response.ResponseUrl.Contains("/_layouts/15/Throttle.htm", StringComparison.OrdinalIgnoreCase);

    private void EnterCooldown(TimeSpan delay)
    {
        var until = DateTimeOffset.UtcNow + delay;
        lock (_cooldownLock)
        {
            if (until > _throttledUntilUtc)
            {
                // Only the extension counts towards the budget: concurrent requests share one
                // cooldown, so charging each of them for the same wall clock would trip early.
                _throttleWaited += until - _throttledUntilUtc;
                _throttledUntilUtc = until;
            }

            _throttleEvents++;
            if (_throttleWaited >= ThrottleBudget)
            {
                _throttleBudgetExhausted = true;
            }
        }
    }

    /// <summary>
    /// Waits out the shared cooldown, reporting the remaining time about once a second so the
    /// caller can show a countdown rather than a message that looks frozen.
    /// </summary>
    private async Task WaitForCooldownAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            TimeSpan remaining;
            lock (_cooldownLock)
            {
                remaining = _throttledUntilUtc - DateTimeOffset.UtcNow;
            }

            if (remaining <= TimeSpan.Zero)
            {
                return;
            }

            ThrottleWaitStarted?.Invoke(this, remaining);
            var slice = remaining < TimeSpan.FromSeconds(1) ? remaining : TimeSpan.FromSeconds(1);
            await Task.Delay(slice, cancellationToken);
        }
    }

    private async Task<BrowserResponse> SendOnceAsync(
        string method,
        Uri endpoint,
        string? formDigest,
        CancellationToken cancellationToken)
    {
        if (!_signInForm.IsBrowserReady)
        {
            throw new InvalidOperationException("WebView2セッションが初期化されていません。");
        }

        ValidateApiEndpoint(endpoint);
        ValidateActiveDocument();
        cancellationToken.ThrowIfCancellationRequested();

        // Acquire before starting the clock, so a queued request is not counted as timed out.
        await _requestGate.WaitAsync(cancellationToken);
        var requestId = Guid.NewGuid().ToString("N");
        var completion = new TaskCompletionSource<BrowserResponse>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        if (!_pending.TryAdd(requestId, completion))
        {
            _requestGate.Release();
            throw new InvalidOperationException("SharePoint要求IDを作成できませんでした。");
        }

        var request = JsonSerializer.Serialize(new
        {
            id = requestId,
            url = endpoint.AbsoluteUri,
            method,
            digest = formDigest,
        });
        var script = $$"""
            (() => {
              const request = {{request}};
              const headers = {
                'Accept': 'application/json;odata.metadata=none',
                'OData-Version': '4.0'
              };
              if (request.digest) {
                headers['X-RequestDigest'] = request.digest;
              }
              fetch(request.url, {
                method: request.method,
                credentials: 'include',
                headers: headers
              }).then(async response => {
                const body = await response.text();
                chrome.webview.postMessage({
                  source: 'OneDriveVersionCleaner',
                  id: request.id,
                  ok: response.ok,
                  status: response.status,
                  statusText: response.statusText,
                  contentType: response.headers.get('content-type') || '',
                  retryAfterSeconds: Number(response.headers.get('retry-after')) || 0,
                  responseUrl: response.url,
                  body: body
                });
              }).catch(error => {
                chrome.webview.postMessage({
                  source: 'OneDriveVersionCleaner',
                  id: request.id,
                  ok: false,
                  status: 0,
                  statusText: String(error),
                  contentType: '',
                  retryAfterSeconds: 0,
                  responseUrl: request.url,
                  body: ''
                });
              });
            })();
            """;

        try
        {
            // Both the injection and the reply are bounded: an unresponsive renderer must not
            // hold a gate permit forever and stall every other request behind it.
            await _signInForm.ExecuteScriptAsync(script)
                .WaitAsync(RequestTimeout, cancellationToken);
            return await completion.Task.WaitAsync(RequestTimeout, cancellationToken);
        }
        finally
        {
            _pending.TryRemove(requestId, out _);
            _requestGate.Release();
        }
    }

    private async Task<BrowserResponse> PostWithDigestAsync(Uri endpoint, CancellationToken cancellationToken)
    {
        var digest = await EnsureFormDigestAsync(forceRefresh: false, cancellationToken);
        var response = await SendAsync("POST", endpoint, digest, cancellationToken);
        if (response.Status != 403)
        {
            return response;
        }

        // SharePoint reports an expired form digest as 403. Refresh once before reporting it
        // as an access failure.
        digest = await EnsureFormDigestAsync(forceRefresh: true, cancellationToken);
        return await SendAsync("POST", endpoint, digest, cancellationToken);
    }

    private async Task<string> EnsureFormDigestAsync(bool forceRefresh, CancellationToken cancellationToken)
    {
        if (!forceRefresh &&
            _formDigest is not null &&
            DateTimeOffset.UtcNow < _formDigestExpiresAtUtc)
        {
            return _formDigest;
        }

        var response = await SendAsync("POST", BuildApiUri("contextinfo"), formDigest: null, cancellationToken);
        EnsureSuccess(response, "SharePointの更新トークン取得");
        var context = DeserializeBody<SharePointContextInfo>(response, "SharePointコンテキスト情報").Resolve();
        if (string.IsNullOrWhiteSpace(context.FormDigestValue))
        {
            throw new InvalidDataException("SharePointの更新トークンを取得できませんでした。");
        }

        // Renew a minute early so a long run never posts with a digest that expires in flight.
        var lifetimeSeconds = Math.Max(120, context.FormDigestTimeoutSeconds);
        _formDigest = context.FormDigestValue;
        _formDigestExpiresAtUtc = DateTimeOffset.UtcNow.AddSeconds(lifetimeSeconds - 60);
        return _formDigest;
    }

    private void WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs eventArgs)
    {
        if (!IsAllowedSiteDocument(eventArgs.Source))
        {
            return;
        }

        BrowserResponse? response;
        try
        {
            response = JsonSerializer.Deserialize<BrowserResponse>(eventArgs.WebMessageAsJson, JsonOptions);
        }
        catch (JsonException)
        {
            return;
        }

        if (response is null ||
            !string.Equals(response.Source, "OneDriveVersionCleaner", StringComparison.Ordinal) ||
            string.IsNullOrWhiteSpace(response.Id))
        {
            return;
        }

        if (_pending.TryGetValue(response.Id, out var completion))
        {
            completion.TrySetResult(response);
        }
    }

    private void FailPendingRequests(Exception reason)
    {
        foreach (var requestId in _pending.Keys)
        {
            if (_pending.TryRemove(requestId, out var completion))
            {
                completion.TrySetException(reason);
            }
        }
    }

    private static string FileResource(string serverRelativePath) =>
        $"web/GetFileByServerRelativePath(decodedUrl='{EncodeODataString(serverRelativePath)}')";

    private static string FolderResource(string serverRelativePath) =>
        $"web/GetFolderByServerRelativePath(decodedUrl='{EncodeODataString(serverRelativePath)}')";

    private static string FileResourceById(Guid uniqueId) =>
        $"web/GetFileById('{uniqueId:D}')";

    private static string FolderResourceById(Guid uniqueId) =>
        $"web/GetFolderById('{uniqueId:D}')";

    private Uri BuildApiUri(string relative)
    {
        var baseUri = _serviceEndpointUri.AbsoluteUri.EndsWith('/')
            ? _serviceEndpointUri
            : new Uri(_serviceEndpointUri.AbsoluteUri + '/', UriKind.Absolute);
        return new Uri(baseUri, relative);
    }

    private void ValidateApiEndpoint(Uri endpoint)
    {
        if (!SharePointOrigin.IsHttps(endpoint) ||
            !string.Equals(endpoint.Host, _serviceEndpointUri.Host, StringComparison.OrdinalIgnoreCase) ||
            endpoint.Port != _serviceEndpointUri.Port ||
            !endpoint.AbsolutePath.StartsWith(
                _serviceEndpointUri.AbsolutePath.TrimEnd('/') + '/',
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("許可されていないSharePoint API URLです。");
        }
    }

    private void ValidateActiveDocument()
    {
        if (!IsAllowedSiteDocument(_signInForm.DocumentSource))
        {
            throw new InvalidOperationException(
                "OneDriveサイトが表示されていません。サインイン画面を開き直してください。");
        }
    }

    /// <summary>
    /// Whether a document may issue this session's requests. Which API paths it may reach stays
    /// restricted separately, by <see cref="ValidateApiEndpoint"/>.
    /// </summary>
    private bool IsAllowedSiteDocument(string? source) =>
        SharePointOrigin.IsSameOrigin(_siteUri, source);

    private static T DeserializeBody<T>(BrowserResponse response, string operation)
    {
        try
        {
            using var document = JsonDocument.Parse(response.Body);
            var payload = document.RootElement;
            if (payload.ValueKind == JsonValueKind.Object &&
                payload.TryGetProperty("d", out var legacyPayload))
            {
                payload = legacyPayload;
            }

            return payload.Deserialize<T>(JsonOptions)
                   ?? throw new InvalidDataException($"{operation}の応答が空です。");
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException(
                $"{operation}の応答を解析できませんでした。Microsoft 365へのログイン状態を確認してください。",
                exception);
        }
    }

    private static void EnsureSuccess(BrowserResponse response, string operation)
    {
        if (response.Ok && response.ContentType.Contains("json", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (IsThrottled(response))
        {
            throw new SharePointThrottledException(
                $"{operation}が SharePoint の要求制限にかかりました。待機して再試行しましたが解消しませんでした。" +
                "しばらく時間をおいてから、対象フォルダーを分けて実行してください。");
        }

        throw new InvalidOperationException(
            $"{operation}に失敗しました (HTTP {response.Status}: {BuildFailureDetail(response)})。" +
            "通常の組織アカウントでサインインし、OneDriveページが表示されていることを確認してください。");
    }

    private static string BuildFailureDetail(BrowserResponse response)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(response.StatusText))
        {
            parts.Add(response.StatusText);
        }

        var contentType = response.ContentType.Split(';', 2)[0].Trim();
        if (!string.IsNullOrWhiteSpace(contentType))
        {
            parts.Add($"Content-Type={contentType}");
        }

        if (Uri.TryCreate(response.ResponseUrl, UriKind.Absolute, out var responseUri))
        {
            parts.Add($"応答先={responseUri.Host}{responseUri.AbsolutePath}");
        }

        return parts.Count == 0 ? "応答なし" : string.Join(", ", parts);
    }

    private static DeleteResultStatus MapDeleteFailure(int status) => status switch
    {
        404 => DeleteResultStatus.AlreadyDeleted,
        401 or 403 => DeleteResultStatus.AccessDenied,
        409 or 423 => DeleteResultStatus.NotDeletable,
        0 or 408 or 429 or >= 500 and <= 599 => DeleteResultStatus.TransientError,
        _ => DeleteResultStatus.PermanentError,
    };

    private static string BuildMutationError(BrowserResponse response)
    {
        var message = TryReadSharePointErrorMessage(response.Body);
        return string.IsNullOrWhiteSpace(message)
            ? $"HTTP {response.Status}: {BuildFailureDetail(response)}"
            : $"HTTP {response.Status}: {message}";
    }

    private static string? TryReadSharePointErrorMessage(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.ValueKind != JsonValueKind.Object ||
                !document.RootElement.TryGetProperty("error", out var error))
            {
                return null;
            }

            if (error.ValueKind == JsonValueKind.String)
            {
                return error.GetString();
            }

            if (error.ValueKind != JsonValueKind.Object ||
                !error.TryGetProperty("message", out var message))
            {
                return null;
            }

            return message.ValueKind switch
            {
                JsonValueKind.String => message.GetString(),
                JsonValueKind.Object when message.TryGetProperty("value", out var value) => value.GetString(),
                _ => null,
            };
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private void EnsureVerified()
    {
        if (!_verified)
        {
            throw new InvalidOperationException("SharePointへのログイン確認が完了していません。");
        }
    }

    private static string EncodeODataString(string value) =>
        Uri.EscapeDataString(value.Replace("'", "''", StringComparison.Ordinal));

    private sealed record BrowserResponse
    {
        public string Source { get; init; } = string.Empty;

        public string Id { get; init; } = string.Empty;

        public bool Ok { get; init; }

        public int Status { get; init; }

        public string StatusText { get; init; } = string.Empty;

        public string ContentType { get; init; } = string.Empty;

        /// <summary>Seconds the service asked us to wait, or 0 when it did not say.</summary>
        public int RetryAfterSeconds { get; init; }

        public string ResponseUrl { get; init; } = string.Empty;

        public string Body { get; init; } = string.Empty;
    }
}
