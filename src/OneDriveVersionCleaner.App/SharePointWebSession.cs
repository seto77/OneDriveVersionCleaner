using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Text.Json;
using Microsoft.Web.WebView2.Core;
using OneDriveVersionCleaner.Core.Models;
using OneDriveVersionCleaner.Core.Retry;
using static OneDriveVersionCleaner.App.Localization.Localization;

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
    /// What reading one file's history costs in requests: the file itself, and its version
    /// collection. Callers estimate the size of a run with this rather than restating the
    /// multiplier, which would go quietly stale if <see cref="GetFileVersionsAsync"/> changed
    /// what it asks for.
    /// </summary>
    public const int RequestsPerVersionRead = 2;

    /// <summary>
    /// How many times one request waits out a throttle before giving up. Kept small: a scan
    /// issues thousands of requests, so patience per request multiplies into hours.
    /// </summary>
    private const int MaximumThrottleRetries = 3;

    /// <summary>Upper bound on a single wait, including a server-supplied Retry-After.</summary>
    private const int MaximumRetryAfterSeconds = 30;

    /// <summary>
    /// Total time an operation may spend waiting out throttling before the session gives up
    /// entirely. Without this the retries alone keep a throttled tenant grinding for hours
    /// while the UI shows nothing but a wait message.
    /// </summary>
    private static readonly TimeSpan ThrottleBudget = TimeSpan.FromMinutes(2);

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

    /// <summary>
    /// Re-translates the sign-in window. It outlives its own dialog, so a language change while
    /// a session is alive has to reach it here rather than at its next construction.
    /// </summary>
    public void ApplyLocalizedText() => _signInForm.ApplyLocalizedText();

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
        EnsureSuccess(response, Loc(en: "Confirming the SharePoint sign-in", ja: "SharePointへのログイン確認"));
        var result = DeserializeBody<SharePointSiteInfo>(
            response,
            Loc(en: "Reading the SharePoint site info", ja: "SharePointサイト情報"));
        if (string.IsNullOrWhiteSpace(result.Url))
        {
            throw new InvalidDataException(Loc(
                en: "The SharePoint site URL could not be read.",
                ja: "SharePointサイトURLを取得できませんでした。"));
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
            Loc(en: "Reading the OneDrive root", ja: "OneDriveルートの取得"),
            cancellationToken);
        if (string.IsNullOrWhiteSpace(result.Path))
        {
            throw new InvalidDataException(Loc(
                en: "The server-relative OneDrive URL could not be read.",
                ja: "OneDriveのサーバー相対URLを取得できませんでした。"));
        }

        return result;
    }

    /// <summary>
    /// Addresses a file by its unique id when one is known, and by its path otherwise, falling
    /// back to the other form if the first is rejected.
    /// </summary>
    /// <remarks>
    /// The id comes first on purpose. A path names a location, and a location can come to hold a
    /// different file between one request and the next — a rename or a replacement is enough. The
    /// id names the item itself, so nothing that happens to the path can redirect the request to
    /// something else. Callers that have no id (the scan, which is discovering files by path in
    /// the first place) get the path form and no fallback.
    /// <para>
    /// The path form still earns its place as the fallback: a name that cannot be round-tripped
    /// through a URL, or a path that is simply too long, fails before the OData layer sees it.
    /// </para>
    /// </remarks>
    private static async Task<T> ReadByIdOrPathAsync<T>(
        Func<string, Task<T>> read,
        string pathResource,
        string? idResource)
    {
        var preferred = idResource ?? pathResource;
        var alternate = idResource is null ? null : pathResource;
        try
        {
            return await read(preferred);
        }
        catch (Exception exception) when (alternate is not null &&
            exception is InvalidOperationException or InvalidDataException)
        {
            return await read(alternate);
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
                BuildApiUri("web/DefaultDocumentLibrary?$select=EnableVersioning"),
                Loc(en: "Reading the library's versioning settings", ja: "ライブラリのバージョン設定の取得"),
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

        var fileTask = ReadByIdOrPathAsync(
            resource => GetSingleAsync<SharePointFileInfo>(
                BuildApiUri(resource),
                Loc(en: "Reading the file", ja: "対象ファイルの取得"),
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
                Error = Loc(en: "Not usable as a SharePoint version id", ja: "SharePointのバージョンIDとして扱えません") +
                        $": {versionId}",
            };
        }

        var recycle = $"/Versions/RecycleByID(vid={numericVersionId.ToString(CultureInfo.InvariantCulture)})";

        try
        {
            var response = await ReadByIdOrPathAsync(
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

    private Task<IReadOnlyList<SharePointFileVersionInfo>> GetVersionsAsync(
        string serverRelativePath,
        Guid uniqueId,
        CancellationToken cancellationToken) => ReadByIdOrPathAsync(
        resource => ReadVersionsAsync(resource, cancellationToken),
        FileResource(serverRelativePath),
        uniqueId == Guid.Empty ? null : FileResourceById(uniqueId));

    private Task<IReadOnlyList<SharePointFileVersionInfo>> ReadVersionsAsync(
        string fileResource,
        CancellationToken cancellationToken) => GetCollectionAsync<SharePointFileVersionInfo>(
        BuildApiUri($"{fileResource}/Versions?$select={VersionSelect},CreatedBy/Title&$expand=CreatedBy"),
        Loc(en: "Reading the version history", ja: "バージョン履歴の取得"),
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
                Loc(en: "The SharePoint next-page URL could not be resolved",
                    ja: "SharePointの次ページURLを解釈できませんでした") +
                $": {nextLink}");
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

        throw new SharePointThrottledException(Loc(
            en: "Stopped because SharePoint kept throttling. Waiting and retrying did not clear it.",
            ja: "SharePointの要求制限が続いているため中断しました。待機して再試行しましたが解消しませんでした。"));
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
            throw new InvalidOperationException(Loc(
                en: "The WebView2 session is not initialized.",
                ja: "WebView2セッションが初期化されていません。"));
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
            throw new InvalidOperationException(Loc(
                en: "A SharePoint request id could not be created.",
                ja: "SharePoint要求IDを作成できませんでした。"));
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
        EnsureSuccess(response, Loc(
            en: "Reading the SharePoint update token",
            ja: "SharePointの更新トークン取得"));
        var context = DeserializeBody<SharePointContextInfo>(
            response,
            Loc(en: "Reading the SharePoint context info", ja: "SharePointコンテキスト情報")).Resolve();
        if (string.IsNullOrWhiteSpace(context.FormDigestValue))
        {
            throw new InvalidDataException(Loc(
                en: "The SharePoint update token could not be read.",
                ja: "SharePointの更新トークンを取得できませんでした。"));
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

    private static string FileResourceById(Guid uniqueId) =>
        $"web/GetFileById('{uniqueId:D}')";

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
            throw new InvalidOperationException(Loc(
                en: "That SharePoint API URL is not allowed.",
                ja: "許可されていないSharePoint API URLです。"));
        }
    }

    private void ValidateActiveDocument()
    {
        if (!IsAllowedSiteDocument(_signInForm.DocumentSource))
        {
            throw new InvalidOperationException(Loc(
                en: "The OneDrive site is not on screen. Open the sign-in window again.",
                ja: "OneDriveサイトが表示されていません。サインイン画面を開き直してください。"));
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
                   ?? throw new InvalidDataException(Loc(
                       en: $"{operation}: the response was empty.",
                       ja: $"{operation}の応答が空です。"));
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException(
                Loc(en: $"{operation}: the response could not be parsed. " +
                        "Check that you are still signed in to Microsoft 365.",
                    ja: $"{operation}の応答を解析できませんでした。Microsoft 365へのログイン状態を確認してください。"),
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
            throw new SharePointThrottledException(Loc(
                en: $"{operation}: SharePoint throttled the request. Waiting and retrying did not " +
                    "clear it. Leave it a while, then run one folder at a time.",
                ja: $"{operation}が SharePoint の要求制限にかかりました。待機して再試行しましたが解消しませんでした。" +
                    "しばらく時間をおいてから、対象フォルダーを分けて実行してください。"));
        }

        throw new InvalidOperationException(Loc(
            en: $"{operation} failed (HTTP {response.Status}: {BuildFailureDetail(response)}). " +
                "Check that you are signed in with an ordinary work account and that the OneDrive " +
                "page is showing.",
            ja: $"{operation}に失敗しました (HTTP {response.Status}: {BuildFailureDetail(response)})。" +
                "通常の組織アカウントでサインインし、OneDriveページが表示されていることを確認してください。"));
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
            parts.Add(
                Loc(en: "responded-from", ja: "応答先") +
                $"={responseUri.Host}{responseUri.AbsolutePath}");
        }

        return parts.Count == 0
            ? Loc(en: "no response", ja: "応答なし")
            : string.Join(", ", parts);
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
            throw new InvalidOperationException(Loc(
                en: "The SharePoint sign-in has not been confirmed yet.",
                ja: "SharePointへのログイン確認が完了していません。"));
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
