using System.Text.Json;
using Microsoft.Web.WebView2.Core;
using static OneDriveVersionCleaner.App.Localization.Localization;

namespace OneDriveVersionCleaner.App;

/// <summary>
/// Hosts the user's normal Microsoft 365 sign-in.
/// The window outlives its own dialog: <see cref="SharePointWebSession"/> keeps issuing
/// same-origin requests from the page it left behind, so whoever owns the session decides when
/// this form is disposed. Authentication cookies never leave WebView2.
/// </summary>
internal sealed partial class SharePointSignInForm : Form
{
    private readonly Uri _siteUri;
    private readonly Func<bool> _hasPendingRequests;
    private readonly Func<CancellationToken, Task<string>> _verifyAsync;
    private bool _initialized;

    /// <summary>
    /// Designer only. The arguments are inert stand-ins: the design surface never navigates,
    /// never verifies, and never creates a CoreWebView2.
    /// </summary>
    private SharePointSignInForm()
        : this(
            new Uri("https://designer.invalid/"),
            static () => false,
            static _ => Task.FromResult(string.Empty))
    {
    }

    /// <param name="siteUri">The personal OneDrive site the window signs in to.</param>
    /// <param name="hasPendingRequests">
    /// Whether the session is waiting for replies from the current page. Navigating away would
    /// strand them, so navigation and the verify button are held back while it is true.
    /// </param>
    /// <param name="verifyAsync">
    /// Confirms the sign-in and returns the site title. Owned by the session, because the
    /// confirmation is a REST call.
    /// </param>
    public SharePointSignInForm(
        Uri siteUri,
        Func<bool> hasPendingRequests,
        Func<CancellationToken, Task<string>> verifyAsync)
    {
        _siteUri = siteUri;
        _hasPendingRequests = hasPendingRequests;
        _verifyAsync = verifyAsync;

        InitializeComponent();
        ApplyLocalizedText();
    }

    /// <summary>
    /// Puts the current interface language over the English the design surface carries. Called
    /// again when the language changes, which is why the status line is re-derived rather than
    /// left showing a sentence in the language it was written in.
    /// </summary>
    public void ApplyLocalizedText()
    {
        Text = Loc(
            en: "Microsoft 365 sign-in (your own account)",
            ja: "Microsoft 365 サインイン（本人権限）");
        _verifyButton.Text = Loc(en: "Confirm sign-in", ja: "ログイン完了を確認");
        _reloadButton.Text = Loc(en: "Reload page", ja: "ページを再読み込み");
        _cancelButton.Text = Loc(en: "Cancel", ja: "キャンセル");

        if (_initialized)
        {
            RefreshVerifyState();
        }
        else
        {
            _statusLabel.Text = SignInPrompt();
        }
    }

    private static string SignInPrompt() => Loc(
        en: "Sign in with your work or school Microsoft 365 account.",
        ja: "職場または学校のMicrosoft 365アカウントでサインインしてください。");

    /// <summary>Raised for every message the page posts back.</summary>
    public event EventHandler<CoreWebView2WebMessageReceivedEventArgs>? WebMessageReceived;

    /// <summary>Raised once the browser process is gone and no reply can arrive any more.</summary>
    public event EventHandler<string>? BrowserFailed;

    /// <summary>
    /// Raised when the page is about to be replaced, so the session stops treating the sign-in
    /// as confirmed until it has been verified against the new page.
    /// </summary>
    public event EventHandler? SignedOut;

    public bool IsBrowserReady => _initialized && _webView.CoreWebView2 is not null;

    /// <summary>The document the browser currently shows, or null before it has one.</summary>
    public string? DocumentSource => _webView.CoreWebView2?.Source;

    public Task<string> ExecuteScriptAsync(string script) => _webView.CoreWebView2 is { } browser
        ? browser.ExecuteScriptAsync(script)
        : throw new InvalidOperationException(Loc(
            en: "The WebView2 session is not initialized.",
            ja: "WebView2セッションが初期化されていません。"));

    /// <summary>
    /// Creating the browser is runtime work, so it starts here rather than anywhere the
    /// designer could reach: not from the constructor, and not from InitializeComponent.
    /// </summary>
    protected override async void OnShown(EventArgs eventArgs)
    {
        base.OnShown(eventArgs);
        try
        {
            _statusLabel.Text = Loc(
                en: "Starting the embedded browser...",
                ja: "Web表示を初期化しています...");
            await InitializeBrowserAsync(CancellationToken.None);
            if (!IsDisposed)
            {
                NavigateToSite();
            }
        }
        catch (Exception exception)
        {
            if (IsDisposed)
            {
                return;
            }

            _verifyButton.Enabled = false;
            _reloadButton.Enabled = false;
            _statusLabel.Text = Loc(
                en: "The embedded browser could not be started.",
                ja: "Web表示を初期化できませんでした。");
            MessageBox.Show(
                this,
                exception.Message,
                Loc(en: "Embedded browser", ja: "Web表示の初期化"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            DialogResult = DialogResult.Cancel;
        }
    }

    private async Task InitializeBrowserAsync(CancellationToken cancellationToken)
    {
        if (_initialized)
        {
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();
        var userDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OneDriveVersionCleaner",
            "WebView2");
        Directory.CreateDirectory(userDataFolder);

        var environment = await CoreWebView2Environment.CreateAsync(
            browserExecutableFolder: null,
            userDataFolder: userDataFolder);
        cancellationToken.ThrowIfCancellationRequested();
        await _webView.EnsureCoreWebView2Async(environment);

        var browser = _webView.CoreWebView2;
        browser.WebMessageReceived += BrowserOnWebMessageReceived;
        browser.NavigationStarting += BrowserOnNavigationStarting;
        browser.NavigationCompleted += BrowserOnNavigationCompleted;

        // OneDrive Web is a single-page app: after sign-in it routes between /my, / and
        // /shared without a document navigation, so NavigationCompleted alone goes stale.
        browser.SourceChanged += BrowserOnSourceChanged;
        browser.HistoryChanged += BrowserOnHistoryChanged;
        browser.ProcessFailed += BrowserOnProcessFailed;
        browser.Settings.AreDevToolsEnabled = false;
        browser.Settings.IsWebMessageEnabled = true;
        browser.Settings.IsStatusBarEnabled = true;
        _initialized = true;
    }

    private void BrowserOnWebMessageReceived(
        object? sender,
        CoreWebView2WebMessageReceivedEventArgs eventArgs) =>
        WebMessageReceived?.Invoke(this, eventArgs);

    private void BrowserOnNavigationStarting(
        object? sender,
        CoreWebView2NavigationStartingEventArgs eventArgs)
    {
        if (_hasPendingRequests())
        {
            eventArgs.Cancel = true;
            return;
        }

        _navigationTimer.Stop();
        _navigationTimer.Start();
        _verifyButton.Enabled = false;
        _reloadButton.Enabled = false;
        _statusLabel.Text = Uri.TryCreate(eventArgs.Uri, UriKind.Absolute, out var destination) &&
            string.Equals(destination.Host, _siteUri.Host, StringComparison.OrdinalIgnoreCase)
            ? Loc(en: "Loading your OneDrive page...", ja: "本人のOneDriveページを読み込んでいます...")
            : Loc(en: "Waiting for the Microsoft 365 sign-in...", ja: "Microsoft 365のサインインを待っています...");
    }

    private void BrowserOnNavigationCompleted(
        object? sender,
        CoreWebView2NavigationCompletedEventArgs eventArgs)
    {
        _navigationTimer.Stop();
        _reloadButton.Enabled = true;
        if (!eventArgs.IsSuccess)
        {
            _verifyButton.Enabled = false;
            _statusLabel.Text =
                Loc(en: "The page could not be opened", ja: "ページを開けませんでした") +
                $": {eventArgs.WebErrorStatus}";
            return;
        }

        RefreshVerifyState();
    }

    private void BrowserOnSourceChanged(
        object? sender,
        CoreWebView2SourceChangedEventArgs eventArgs) => RefreshVerifyState();

    private void BrowserOnHistoryChanged(object? sender, object eventArgs) => RefreshVerifyState();

    private void BrowserOnProcessFailed(
        object? sender,
        CoreWebView2ProcessFailedEventArgs eventArgs)
    {
        _navigationTimer.Stop();
        _verifyButton.Enabled = false;
        _reloadButton.Enabled = true;
        var message =
            Loc(en: "The embedded browser failed", ja: "Web表示で問題が発生しました") +
            $": {eventArgs.ProcessFailedKind}";
        _statusLabel.Text = message;

        // No reply can arrive once the renderer is gone. The session fails the outstanding
        // requests now instead of leaving every caller to wait out its own timeout.
        BrowserFailed?.Invoke(this, message);
    }

    private void NavigationTimerOnTick(object? sender, EventArgs eventArgs)
    {
        _navigationTimer.Stop();
        _reloadButton.Enabled = true;
        _statusLabel.Text = Loc(
            en: "The page is taking a long time to load. Try reloading it.",
            ja: "ページの読み込みに時間がかかっています。再読み込みを試してください。");
    }

    private void ReloadButtonOnClick(object? sender, EventArgs eventArgs) => NavigateToSite();

    private async void VerifyButtonOnClick(object? sender, EventArgs eventArgs)
    {
        _verifyButton.Enabled = false;
        _statusLabel.Text = Loc(
            en: "Checking the sign-in...",
            ja: "ログイン状態を確認しています...");
        try
        {
            var title = await _verifyAsync(CancellationToken.None);
            _statusLabel.Text = Loc(en: "Signed in", ja: "接続確認済み") + $": {title}";
            DialogResult = DialogResult.OK;
        }
        catch (Exception exception) when (exception is InvalidOperationException or
            InvalidDataException or JsonException or TimeoutException)
        {
            _statusLabel.Text = Loc(
                en: "The sign-in could not be confirmed. See the details shown.",
                ja: "接続確認に失敗しました。表示された詳細を確認してください。");
            MessageBox.Show(
                this,
                exception.Message,
                Loc(en: "Sign-in check", ja: "ログイン確認"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            _verifyButton.Enabled = true;
        }
    }

    private void RefreshVerifyState()
    {
        if (IsDisposed || _hasPendingRequests())
        {
            return;
        }

        var source = DocumentSource;
        if (SharePointOrigin.IsSameOrigin(_siteUri, source))
        {
            _verifyButton.Enabled = true;
            _statusLabel.Text = Loc(
                en: "Your OneDrive is showing. Press “Confirm sign-in”.",
                ja: "OneDriveを確認できました。「ログイン完了を確認」を押してください。");
            return;
        }

        _verifyButton.Enabled = false;
        _statusLabel.Text = Uri.TryCreate(source, UriKind.Absolute, out var current)
            ? SignInPrompt() +
              Loc(en: $" (currently: {current.Host}{current.AbsolutePath})",
                  ja: $"（現在: {current.Host}{current.AbsolutePath}）")
            : SignInPrompt();
    }

    private void NavigateToSite()
    {
        if (_webView.CoreWebView2 is not { } browser || !_initialized)
        {
            return;
        }

        SignedOut?.Invoke(this, EventArgs.Empty);
        _verifyButton.Enabled = false;
        _reloadButton.Enabled = false;
        _statusLabel.Text = Loc(
            en: "Opening your OneDrive page...",
            ja: "本人のOneDriveページを開いています...");
        browser.Navigate(_siteUri.AbsoluteUri);
    }
}
