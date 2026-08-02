using System.Diagnostics;
using System.Text;
using System.Text.Json;
using OneDriveVersionCleaner.App.Localization;
using OneDriveVersionCleaner.Core.Aggregation;
using OneDriveVersionCleaner.Core.Auditing;
using OneDriveVersionCleaner.Core.Caching;
using OneDriveVersionCleaner.Core.Deletion;
using OneDriveVersionCleaner.Core.Export;
using OneDriveVersionCleaner.Core.Formatting;
using OneDriveVersionCleaner.Core.Models;
using OneDriveVersionCleaner.Core.Paths;
using OneDriveVersionCleaner.Core.Retention;
using OneDriveVersionCleaner.Core.Scanning;
using OneDriveVersionCleaner.Infrastructure.OneDriveAccounts;
using static OneDriveVersionCleaner.App.Localization.Localization;

namespace OneDriveVersionCleaner.App;

internal sealed partial class MainForm : Form
{
    private const long Mebibyte = 1024L * 1024;

    /// <summary>
    /// The grid is sorted largest first, so anything past this point cannot matter for
    /// reclaiming space. The full set still reaches the CSV and JSON exports.
    /// </summary>
    private const int MaximumDisplayedVersions = 5_000;

    /// <summary>Splitter geometry, applied once the split area has its real size.</summary>
    private const int VersionsPanelMinimumHeight = 220;
    private const int FilesPanelMinimumHeight = 120;
    private const int PreferredSplitterDistance = 440;

    private readonly AppSettings _settings;
    private readonly string? _settingsReadFailure;
    private readonly OneDriveBusinessAccountDiscovery _accountDiscovery = new();
    private readonly VersionCacheStore _versionCache = new();

    /// <summary>Server-relative path per scanned file. Only these files can be acted on.</summary>
    private readonly Dictionary<string, string> _serverPathByItemId = new(StringComparer.Ordinal);

    private IReadOnlyList<FileEntry> _files = [];
    private RetentionEvaluationResult? _retention;
    private ScanAggregate? _aggregate;
    private CancellationTokenSource? _operationCancellation;
    private OneDriveBusinessAccount? _detectedAccount;
    private SharePointWebSession? _sharePointSession;
    private string? _libraryRoot;
    private bool _isBusy;

    /// <summary>Set when the window was closed during an operation; the close resumes after it.</summary>
    private bool _closeWhenIdle;

    /// <summary>Rebuilding the grid raises SelectionChanged once per row; ignore those.</summary>
    private bool _rebuildingVersionsGrid;

    /// <summary>What the versions heading says, before it is put into words.</summary>
    private sealed record VersionsHeader(int RowCount, long TotalBytes, int? DisplayedCount);

    private VersionsHeader? _versionsHeader;

    private sealed record VersionRow(
        FileEntry File,
        VersionEntry Version,
        VersionRetentionDecision? Decision)
    {
        public bool IsCandidate => Decision?.IsDeleteCandidate == true;
    }

    /// <summary>
    /// Designer only. Must never do anything a running instance would do: no settings, no
    /// account discovery, no file system, no network.
    /// </summary>
    private MainForm()
    {
        _settings = new AppSettings();
        InitializeComponent();
    }

    public MainForm(AppSettings settings, string? settingsReadFailure)
    {
        _settings = settings;
        _settingsReadFailure = settingsReadFailure;

        InitializeComponent();
        InitializeGridSchema();
        ApplyUiFont();
        ApplyLocalizedText();
        ApplySettings();
        WireRuntimeEvents();
        RefreshDerivedText();
    }

    /// <summary>
    /// The part of the grid schema the design surface cannot carry.
    /// A column's name is its component name in the form's container, so the two grids cannot
    /// both own "FileName" and "CloudPath"; the designer names the fields, and the names the
    /// code depends on are assigned here. <see cref="DataGridViewColumn.ValueType"/> is
    /// <c>Browsable(false)</c>, so the property grid neither shows it nor round-trips it, and
    /// losing one leaves the display correct while breaking sorting on that column alone.
    /// <see cref="DataGridView.AutoGenerateColumns"/> is likewise not a design-time property.
    /// </summary>
    /// <remarks>
    /// CreateCells fills cells by position and CellFormatting switches on the names, so the
    /// order, the names and the types below all have to stay exactly as they are.
    /// </remarks>
    private void InitializeGridSchema()
    {
        _versionsGrid.AutoGenerateColumns = false;
        _filesGrid.AutoGenerateColumns = false;

        _versionsFileNameColumn.Name = "FileName";
        _versionsFileNameColumn.ValueType = typeof(string);
        _versionsCloudPathColumn.Name = "CloudPath";
        _versionsCloudPathColumn.ValueType = typeof(string);
        _versionsVersionLabelColumn.Name = "VersionLabel";
        _versionsVersionLabelColumn.ValueType = typeof(string);
        _versionsCreatedColumn.Name = "Created";
        _versionsCreatedColumn.ValueType = typeof(DateTime);
        _versionsModifiedByColumn.Name = "ModifiedBy";
        _versionsModifiedByColumn.ValueType = typeof(string);
        _versionsSizeColumn.Name = "VersionSize";
        _versionsSizeColumn.ValueType = typeof(long);
        _versionsFileHistorySizeColumn.Name = "FileHistorySize";
        _versionsFileHistorySizeColumn.ValueType = typeof(long);
        _versionsDecisionColumn.Name = "Decision";
        _versionsDecisionColumn.ValueType = typeof(string);
        _versionsReasonsColumn.Name = "Reasons";
        _versionsReasonsColumn.ValueType = typeof(string);

        _filesFileNameColumn.Name = "FileName";
        _filesFileNameColumn.ValueType = typeof(string);
        _filesCloudPathColumn.Name = "CloudPath";
        _filesCloudPathColumn.ValueType = typeof(string);
        _filesCurrentSizeColumn.Name = "CurrentSize";
        _filesCurrentSizeColumn.ValueType = typeof(long);
        _filesHistoryCountColumn.Name = "HistoryCount";
        _filesHistoryCountColumn.ValueType = typeof(int);
        _filesHistorySizeColumn.Name = "HistorySize";
        _filesHistorySizeColumn.ValueType = typeof(long);
        _filesCandidateCountColumn.Name = "CandidateCount";
        _filesCandidateCountColumn.ValueType = typeof(int);
        _filesReclaimableColumn.Name = "Reclaimable";
        _filesReclaimableColumn.ValueType = typeof(long);
        _filesLastModifiedColumn.Name = "LastModified";
        _filesLastModifiedColumn.ValueType = typeof(DateTime);
        _filesStatusColumn.Name = "Status";
        _filesStatusColumn.ValueType = typeof(string);
    }

    /// <summary>
    /// The subscriptions that must not be live yet when <see cref="ApplySettings"/> runs. The
    /// designer wires its handlers inside InitializeComponent, which is over by then, so
    /// loading the stored policy into the inputs would raise ValueChanged and start a
    /// recalculation before the form has anything to recalculate.
    /// </summary>
    private void WireRuntimeEvents()
    {
        _keepVersionsInput.ValueChanged += PolicyInputOnValueChanged;
        _keepDaysInput.ValueChanged += PolicyInputOnValueChanged;
        _protectDaysInput.ValueChanged += PolicyInputOnValueChanged;
        _minimumHistoryInput.ValueChanged += PolicyInputOnValueChanged;
        _minimumFileSizeInput.ValueChanged += PolicyInputOnValueChanged;
    }

    protected override void OnShown(EventArgs eventArgs)
    {
        // Before the event is raised, so nothing observes a form whose split area is still
        // carrying the size it had during initialization.
        ApplySplitterLayout();
        base.OnShown(eventArgs);

        if (!string.IsNullOrWhiteSpace(_settingsReadFailure))
        {
            MessageBox.Show(
                this,
                Loc(en: "appsettings.json could not be read, so the built-in defaults are in use.",
                    ja: "appsettings.json を読み込めなかったため、既定値で動作しています。") +
                $"\n{_settingsReadFailure}",
                Loc(en: "Settings", ja: "設定の警告"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    /// <summary>
    /// Refuses to close while an operation is running, and closes itself once that operation has
    /// finished instead.
    /// </summary>
    /// <remarks>
    /// A recycle request is deliberately not cancellable: it is sent with
    /// <see cref="CancellationToken.None"/> so that stopping never abandons a request already in
    /// flight (FR-DELETE-009). Disposing the session here would tear the browser out from under
    /// exactly that request, leaving the run with no idea whether SharePoint carried it out, and
    /// no audit line for it. So the close waits for the same thing the stop button waits for.
    /// </remarks>
    protected override void OnFormClosing(FormClosingEventArgs eventArgs)
    {
        base.OnFormClosing(eventArgs);
        if (eventArgs.Cancel)
        {
            return;
        }

        if (_isBusy)
        {
            eventArgs.Cancel = true;
            if (!_closeWhenIdle)
            {
                _closeWhenIdle = true;
                RequestStop();
            }

            return;
        }

        _operationCancellation?.Cancel();
        _sharePointSession?.Dispose();
    }

    /// <summary>
    /// Sizes the two grid panes once the split area has its real height. Neither the splitter
    /// position nor the panel minimums can be set during initialization: they are validated
    /// against the size the control has at that moment, which is still the design-time one, and
    /// the form's own MinimumSize is no guide either because the panels above it grow with the
    /// display scaling and with text that wraps.
    /// </summary>
    private void ApplySplitterLayout()
    {
        var available = _splitContainer.ClientSize.Height - _splitContainer.SplitterWidth;
        if (available < VersionsPanelMinimumHeight + FilesPanelMinimumHeight)
        {
            return;
        }

        _splitContainer.Panel1MinSize = VersionsPanelMinimumHeight;
        _splitContainer.Panel2MinSize = FilesPanelMinimumHeight;
        _splitContainer.SplitterDistance = Math.Clamp(
            PreferredSplitterDistance,
            VersionsPanelMinimumHeight,
            available - FilesPanelMinimumHeight);
    }

    private void BrowseRootButtonOnClick(object? sender, EventArgs eventArgs) =>
        DetectOneDriveAccount(showFailure: true);

    private void BrowseTargetButtonOnClick(object? sender, EventArgs eventArgs) =>
        BrowseForTargetFolder();

    private void StopButtonOnClick(object? sender, EventArgs eventArgs) => RequestStop();

    private void CandidatesOnlyCheckOnCheckedChanged(object? sender, EventArgs eventArgs) =>
        RefreshVersionsGrid();

    private void PolicyInputOnValueChanged(object? sender, EventArgs eventArgs) => Recalculate();

    private void VersionsGridOnSelectionChanged(object? sender, EventArgs eventArgs)
    {
        if (!_rebuildingVersionsGrid)
        {
            RefreshDeleteState();
        }
    }

    private void FileContextMenuOnOpening(
        object? sender,
        System.ComponentModel.CancelEventArgs eventArgs)
    {
        var selected = SelectedFile();
        eventArgs.Cancel = selected is null;
        if (selected is not null)
        {
            _openFolderMenuItem.Enabled = File.Exists(selected.File.LocalPath);
            _openOnWebMenuItem.Enabled = !string.IsNullOrWhiteSpace(selected.File.WebUrl);
        }
    }

    private void OpenFolderMenuItemOnClick(object? sender, EventArgs eventArgs) =>
        OpenSelectedFileFolder();

    private void OpenOnWebMenuItemOnClick(object? sender, EventArgs eventArgs) =>
        OpenSelectedFileOnWeb();

    /// <summary>
    /// A right-click outside the current selection moves the selection to the clicked row, so
    /// the menu always acts on what the user pointed at.
    /// </summary>
    private void FilesGridOnCellMouseDown(object? sender, DataGridViewCellMouseEventArgs eventArgs)
    {
        if (eventArgs.Button != MouseButtons.Right || eventArgs.RowIndex < 0)
        {
            return;
        }

        var row = _filesGrid.Rows[eventArgs.RowIndex];
        if (!row.Selected)
        {
            _filesGrid.ClearSelection();
            row.Selected = true;
        }

        _filesGrid.CurrentCell = row.Cells[Math.Max(eventArgs.ColumnIndex, 0)];
    }

    private FileScanAggregate? SelectedFile() => _filesGrid.SelectedRows.Count == 0
        ? null
        : _filesGrid.SelectedRows[0].Tag as FileScanAggregate;

    private void OpenSelectedFileFolder()
    {
        var selected = SelectedFile();
        if (selected is null)
        {
            return;
        }

        var localPath = selected.File.LocalPath;
        try
        {
            // Opens the containing folder with the file highlighted. The path is passed as a
            // separate argument, so a name containing quotes or spaces cannot alter the command.
            if (File.Exists(localPath))
            {
                Process.Start(new ProcessStartInfo("explorer.exe")
                {
                    ArgumentList = { "/select,", localPath },
                    UseShellExecute = false,
                });
                return;
            }

            var directory = Path.GetDirectoryName(localPath);
            if (Directory.Exists(directory))
            {
                Process.Start(new ProcessStartInfo(directory) { UseShellExecute = true });
                return;
            }

            MessageBox.Show(
                this,
                Loc(en: "Not found locally.", ja: "ローカルに見つかりませんでした。") + $"\n{localPath}",
                Loc(en: "Open folder", ja: "フォルダーを開く"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception exception) when (exception is System.ComponentModel.Win32Exception or
            InvalidOperationException or IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(
                this,
                exception.Message,
                Loc(en: "The folder could not be opened", ja: "フォルダーを開けませんでした"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    private void OpenSelectedFileOnWeb()
    {
        var webUrl = SelectedFile()?.File.WebUrl;
        if (string.IsNullOrWhiteSpace(webUrl) ||
            !Uri.TryCreate(webUrl, UriKind.Absolute, out var uri) ||
            !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
        }
        catch (Exception exception) when (exception is System.ComponentModel.Win32Exception or
            InvalidOperationException)
        {
            MessageBox.Show(
                this,
                exception.Message,
                Loc(en: "The page could not be opened", ja: "Webで開けませんでした"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    private void ApplySettings()
    {
        _rootBox.Text = _settings.LocalOneDriveRoot;
        _targetBox.Text = NormalizeTargetFolder(_settings.TargetPath);
        DetectOneDriveAccount(showFailure: false);
        SetNumber(_keepVersionsInput, _settings.KeepLatestVersions);
        SetNumber(_keepDaysInput, _settings.KeepDays);
        SetNumber(_protectDaysInput, _settings.ProtectRecentlyModifiedFileDays);
        SetNumber(_minimumHistoryInput, _settings.MinimumHistorySizeMB);
        SetNumber(_minimumFileSizeInput, _settings.MinimumFileSizeMB);
        SetNumber(_cacheAgeInput, _settings.VersionCacheMinimumAgeDays);
    }

    /// <summary>
    /// Issues exactly one cheap request, so the user can find out whether SharePoint is still
    /// throttling them without starting a scan that would make the throttling worse.
    /// </summary>
    private async void ProbeButtonOnClick(object? sender, EventArgs eventArgs)
    {
        if (_isBusy)
        {
            return;
        }

        if (_detectedAccount is null)
        {
            MessageBox.Show(
                this,
                Loc(en: "No OneDrive for Business account has been detected. Press “Rescan”.",
                    ja: "OneDrive for Businessアカウントを検出できていません。「再検出」を押してください。"),
                Loc(en: "Connection test", ja: "接続テスト"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        using var cancellation = new CancellationTokenSource();
        _operationCancellation = cancellation;
        SetBusy(true, Loc(
            en: "Running the connection test... (1 request)",
            ja: "接続テストを実行しています... (1要求)"));
        try
        {
            var session = GetOrCreateSharePointSession(_detectedAccount);
            session.ResetThrottleState();
            if (!session.IsVerified && session.ShowDialog(this) != DialogResult.OK)
            {
                _statusLabel.Text = Loc(
                    en: "The connection test was cancelled.",
                    ja: "接続テストを中止しました。");
                return;
            }

            var stopwatch = Stopwatch.StartNew();
            var site = await session.VerifyAsync(cancellation.Token);
            stopwatch.Stop();
            RefreshAccountLabel();

            _statusLabel.Text = Loc(
                en: $"Connection test succeeded: {site.Title} " +
                    $"({stopwatch.ElapsedMilliseconds:N0} ms / " +
                    $"{session.ThrottleEventCount:N0} waits)",
                ja: $"接続テスト成功: {site.Title}（{stopwatch.ElapsedMilliseconds:N0} ミリ秒 / " +
                    $"待機 {session.ThrottleEventCount:N0}回）");
            MessageBox.Show(
                this,
                session.ThrottleEventCount == 0
                    ? Loc(en: "The request went through without being throttled. You can start " +
                              "scanning.\nBegin with a small subfolder.",
                          ja: "要求は制限なしで通りました。走査を始められます。\n" +
                              "まずは小さいサブフォルダーから試してください。")
                    : Loc(en: $"The request went through, but it waited " +
                              $"{session.ThrottleEventCount:N0} times.\n" +
                              "Throttling is still in effect. Give it a little longer.",
                          ja: $"要求は通りましたが、{session.ThrottleEventCount:N0}回の待機が入りました。\n" +
                              "まだ制限が残っています。もう少し時間をおいてください。"),
                Loc(en: "Connection test", ja: "接続テスト"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (SharePointThrottledException)
        {
            _statusLabel.Text = Loc(
                en: "Connection test: still throttled.",
                ja: "接続テスト: まだ制限中です。");
            MessageBox.Show(
                this,
                Loc(en: "SharePoint is still throttling requests.\n\n" +
                        "Double the wait and try again (1 hour → 2 hours → 4 hours).\n" +
                        "Do not scan in the meantime: repeated attempts extend the throttling.",
                    ja: "まだ SharePoint の要求制限が続いています。\n\n" +
                        "待ち時間を倍にして、あらためて試してください（1時間 → 2時間 → 4時間）。\n" +
                        "この間、走査は実行しないでください。試行を重ねると制限が延びます。"),
                Loc(en: "Connection test", ja: "接続テスト"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
        catch (OperationCanceledException)
        {
            _statusLabel.Text = Loc(
                en: "The connection test was stopped.",
                ja: "接続テストを停止しました。");
        }
        catch (Exception exception)
        {
            _statusLabel.Text = Loc(
                en: "The connection test failed.",
                ja: "接続テストに失敗しました。");
            MessageBox.Show(
                this,
                exception.Message,
                Loc(en: "Connection test", ja: "接続テスト"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            _operationCancellation = null;
            SetBusy(false, _statusLabel.Text);
        }
    }

    private async void ScanButtonOnClick(object? sender, EventArgs eventArgs)
    {
        if (_isBusy)
        {
            return;
        }

        using var cancellation = new CancellationTokenSource();
        try
        {
            var account = ValidateScanInputs();
            _operationCancellation = cancellation;
            SetBusy(true, Loc(
                en: "Opening your own OneDrive site...",
                ja: "本人のOneDriveサイトを開いています..."));

            var stopwatch = Stopwatch.StartNew();
            var session = GetOrCreateSharePointSession(account);
            if (!session.IsVerified)
            {
                if (session.ShowDialog(this) != DialogResult.OK || !session.IsVerified)
                {
                    session.Dispose();
                    if (ReferenceEquals(_sharePointSession, session))
                    {
                        _sharePointSession = null;
                    }

                    throw new OperationCanceledException(Loc(
                        en: "The Microsoft 365 sign-in was cancelled.",
                        ja: "Microsoft 365へのサインインを中止しました。"));
                }
            }
            else
            {
                await session.VerifyAsync(cancellation.Token);
            }

            RefreshAccountLabel();
            _statusLabel.Text = Loc(
                en: "Checking the OneDrive document root...",
                ja: "OneDriveのドキュメントルートを確認しています...");
            var library = await session.GetDefaultDocumentLibraryRootAsync(cancellation.Token);
            var libraryRoot = library.Path;

            var versioning = await session.GetVersioningInfoAsync(cancellation.Token);
            session.ResetThrottleState();
            if (!versioning.EnableVersioning)
            {
                _statusLabel.Text = Loc(
                    en: "Versioning is turned off for this library.",
                    ja: "このライブラリはバージョン管理が無効です。");
                MessageBox.Show(
                    this,
                    Loc(en: "Versioning is turned off for this document library.\n" +
                            "There are no past versions, so nothing is scanned.",
                        ja: "このドキュメントライブラリはバージョン管理が無効です。\n" +
                            "過去バージョンは存在しないため、走査は行いません。"),
                    Loc(en: "Versioning", ja: "バージョン管理"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            // The local walk decides the whole request budget, and it costs nothing: no
            // service call is made until the size threshold has already removed everything
            // that cannot be worth a request. Run off the UI thread so a large sync folder
            // does not freeze the form while it is counted.
            var thresholdBytes = CreatePolicy().MinimumCurrentFileBytes;
            var surveyProgress = new Progress<LocalSurveyProgress>(report => _statusLabel.Text = Loc(
                en: $"Walking the local folder... {report.FilesExamined:N0} examined / " +
                    $"{report.CandidatesFound:N0} of {SizeText.Of(thresholdBytes)} or more",
                ja: $"ローカルを検索しています... 検査 {report.FilesExamined:N0}件 / " +
                    $"{SizeText.Of(thresholdBytes)}以上 {report.CandidatesFound:N0}件"));
            var survey = await Task.Run(
                () => LocalFileSurvey.Run(
                    account.UserFolder,
                    _targetBox.Text,
                    thresholdBytes,
                    surveyProgress,
                    cancellation.Token),
                cancellation.Token);

            var progress = new Progress<ScanProgress>(report => _statusLabel.Text = Loc(
                en: $"Reading version history... {report.Completed:N0}/{report.Total:N0}",
                ja: $"バージョン履歴を取得しています... {report.Completed:N0}/{report.Total:N0}件"));

            void OnThrottleWait(object? _, TimeSpan wait) => _statusLabel.Text = ThrottleWaitText(wait);
            session.ThrottleWaitStarted += OnThrottleWait;
            ScanResult scanned;
            try
            {
                scanned = await new OneDriveScanner(session, account, libraryRoot)
                    .ScanAsync(
                        survey,
                        new VersionCacheOptions(
                            await _versionCache.LoadAsync(account.AccountKey, cancellation.Token),
                            TimeSpan.FromDays(decimal.ToDouble(_cacheAgeInput.Value))),
                        count => ConfirmVersionQuery(count, thresholdBytes),
                        progress,
                        cancellation.Token);
            }
            finally
            {
                session.ThrottleWaitStarted -= OnThrottleWait;
            }

            stopwatch.Stop();

            _libraryRoot = libraryRoot;
            _serverPathByItemId.Clear();
            foreach (var file in scanned.Files)
            {
                _serverPathByItemId[file.Entry.ItemId] = file.ServerRelativePath;
            }

            _files = scanned.Files.Select(file => file.Entry).ToArray();
            await SaveVersionCacheAsync(account.AccountKey, scanned.Files);
            ApplyResults(stopwatch.Elapsed);

            var reuse = scanned.ReusedFromCacheCount == 0
                ? ""
                : Loc(en: $" / {scanned.ReusedFromCacheCount:N0} reused from the last read",
                      ja: $" / 前回結果を再利用 {scanned.ReusedFromCacheCount:N0}件");
            _statusLabel.Text = Loc(
                en: $"Done: {_files.Count:N0} files / " +
                    $"{_files.Sum(file => file.PreviousVersionCount):N0} past versions / " +
                    $"{survey.FilesExamined:N0} examined locally → {survey.Candidates.Count:N0} selected / " +
                    $"{scanned.QueriedFileCount:N0} queried",
                ja: $"取得完了: {_files.Count:N0}ファイル / " +
                    $"過去版 {_files.Sum(file => file.PreviousVersionCount):N0}件 / " +
                    $"ローカル検査 {survey.FilesExamined:N0}件 → 対象 {survey.Candidates.Count:N0}件 / " +
                    $"問い合わせ {scanned.QueriedFileCount:N0}件") +
                reuse +
                Loc(en: $" ({stopwatch.Elapsed.TotalSeconds:N1} s)",
                    ja: $"（{stopwatch.Elapsed.TotalSeconds:N1}秒）");

            // Folders the walk could not read and files the service would not answer for leave
            // gaps in the same result, so they are reported together.
            ReportScanErrors([.. survey.Errors, .. scanned.Errors]);
        }
        catch (OperationCanceledException)
        {
            _statusLabel.Text = Loc(en: "Stopped.", ja: "処理を停止しました。");
        }
        catch (SharePointThrottledException exception)
        {
            _statusLabel.Text = Loc(
                en: "Interrupted by SharePoint throttling.",
                ja: "SharePointの要求制限により中断しました。");
            MessageBox.Show(
                this,
                $"{exception.Message}\n\n" +
                Loc(en: "What to do:\n" +
                        "• Wait some minutes to some tens of minutes, then run it again\n" +
                        "• Run one subfolder at a time instead of the whole sync root\n" +
                        "• Check that no other OneDrive activity (a large sync, say) is running",
                    ja: "対処:\n" +
                        "・数分から数十分おいてから再実行してください\n" +
                        "・同期ルート全体ではなく、サブフォルダーに分けて実行してください\n" +
                        "・他のOneDrive操作（大量同期など）が動いていないか確認してください"),
                Loc(en: "SharePoint throttling", ja: "SharePointの要求制限"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
        catch (Exception exception)
        {
            _statusLabel.Text = Loc(en: "The read failed.", ja: "取得に失敗しました。");
            MessageBox.Show(
                this,
                exception.Message,
                Loc(en: "Read error", ja: "取得エラー"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            _operationCancellation = null;
            SetBusy(false, _statusLabel.Text);
        }
    }

    private async void DeleteButtonOnClick(object? sender, EventArgs eventArgs)
    {
        if (_isBusy)
        {
            return;
        }

        var session = _sharePointSession;
        if (session is null || !session.IsVerified)
        {
            MessageBox.Show(
                this,
                Loc(en: "Press “Read history” and sign in first.",
                    ja: "先に「履歴を取得」でサインインしてください。"),
                Loc(en: "Move to recycle bin", ja: "ごみ箱へ移動"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        var selectedRows = GetSelectedRows();
        var selection = ToPlannedDeletions(selectedRows);
        if (selection.Count == 0)
        {
            MessageBox.Show(
                this,
                Loc(en: "Select the past versions to move in the upper grid. " +
                        "Shift or Ctrl selects several.",
                    ja: "移動する過去バージョンを表から選択してください。ShiftまたはCtrlで複数選択できます。"),
                Loc(en: "Move to recycle bin", ja: "ごみ箱へ移動"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        var policy = CreatePolicy();
        VersionDeletionPlan plan;
        try
        {
            plan = new VersionDeletionPlanner().Create(selection);
        }
        catch (InvalidOperationException exception)
        {
            MessageBox.Show(
                this,
                Loc(en: "The move could not be planned, so nothing was done.",
                    ja: "移動計画を作成できませんでした。処理を中止します。") +
                $"\n{exception.Message}",
                Loc(en: "Move plan", ja: "移動計画"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        if (plan.IsEmpty)
        {
            return;
        }

        using (var confirmation = new DeleteConfirmationForm(
            plan,
            CountPolicyProtected(plan, selectedRows)))
        {
            if (confirmation.ShowDialog(this) != DialogResult.OK)
            {
                _statusLabel.Text = Loc(
                    en: "The move to the recycle bin was cancelled.",
                    ja: "ごみ箱への移動を中止しました。");
                return;
            }
        }

        using var cancellation = new CancellationTokenSource();
        _operationCancellation = cancellation;
        SetBusy(true, Loc(
            en: $"Moving to the recycle bin... (0/{plan.VersionCount:N0})",
            ja: $"ごみ箱へ移動しています... (0/{plan.VersionCount:N0})"));
        try
        {
            var progress = new Progress<VersionDeletionProgress>(report =>
                _statusLabel.Text = string.IsNullOrEmpty(report.CloudPath)
                    ? Loc(en: $"Checking the outcome... ({report.CompletedCount:N0}/{report.TotalCount:N0})",
                          ja: $"結果を確認しています... ({report.CompletedCount:N0}/{report.TotalCount:N0})")
                    : Loc(en: $"Moving ({report.CompletedCount + 1:N0}/{report.TotalCount:N0}): " +
                              $"{report.CloudPath} {report.VersionLabel}",
                          ja: $"移動しています ({report.CompletedCount + 1:N0}/{report.TotalCount:N0}): " +
                              $"{report.CloudPath} {report.VersionLabel}"));

            void OnThrottleWait(object? _, TimeSpan wait) => _statusLabel.Text = ThrottleWaitText(wait);
            session.ResetThrottleState();
            session.ThrottleWaitStarted += OnThrottleWait;
            VersionDeletionRunOutput output;
            try
            {
                output = await new VersionDeletionRunner(
                        session,
                        file => _serverPathByItemId.GetValueOrDefault(file.ItemId))
                    .RunAsync(plan, progress, cancellation.Token);
            }
            finally
            {
                session.ThrottleWaitStarted -= OnThrottleWait;
            }

            // The record of what was recycled is written before anything else is touched.
            // Refreshing the grid and the cache is presentation; an exception in there must not
            // be able to take the only account of what happened with it.
            var logPath = await WriteDeletionLogAsync(policy, output.Result);
            await ApplyDeletionResultAsync(output.RefreshedFiles);
            ReportDeletionResult(output.Result, logPath);
        }
        catch (OperationCanceledException)
        {
            _statusLabel.Text = Loc(
                en: "The move to the recycle bin was stopped.",
                ja: "ごみ箱への移動を停止しました。");
        }
        catch (Exception exception)
        {
            _statusLabel.Text = Loc(
                en: "The move to the recycle bin failed.",
                ja: "ごみ箱への移動に失敗しました。");
            MessageBox.Show(
                this,
                exception.Message,
                Loc(en: "Move error", ja: "移動エラー"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            _operationCancellation = null;
            SetBusy(false, _statusLabel.Text);
        }
    }

    /// <summary>
    /// Publishes what the deletion run observed, to the grid and to the cache in one step.
    /// Both are built from the same array, so there is no order to get wrong. Recycling a
    /// version never touches the local file, so without the cache write the next scan would
    /// find nothing changed, reuse the stored answer, and put the versions that were just
    /// recycled straight back on the grid.
    /// </summary>
    private async Task ApplyDeletionResultAsync(
        IReadOnlyDictionary<string, RefreshedFile> refreshed)
    {
        if (refreshed.Count == 0 || _detectedAccount is null || _libraryRoot is null)
        {
            return;
        }

        var observed = new List<ScannedFile>(refreshed.Count);
        _files = _files
            .Select(file =>
            {
                if (!refreshed.TryGetValue(file.ItemId, out var refresh))
                {
                    return file;
                }

                // The local path is carried over rather than rebuilt: it is the cache's key.
                // The timestamp comes from the sample the run already took, so no file is
                // read from disk again here.
                var entry = OneDriveScanner.CreateEntry(
                    _detectedAccount,
                    _libraryRoot,
                    refresh.Probe,
                    file.LocalPath);
                observed.Add(new ScannedFile(
                    entry,
                    refresh.Probe.File.Path,
                    DateTimeOffset.UtcNow,
                    refresh.LocalFile.LastWriteTimeUtc));
                return entry;
            })
            .ToArray();

        ApplyResults(_aggregate?.Summary.ScanDuration);
        await SaveVersionCacheAsync(_detectedAccount.AccountKey, observed);
    }

    /// <summary>
    /// Records what was observed so the next scan can skip the files that did not change.
    /// A cache that cannot be written only costs requests later, so it never fails the caller.
    /// </summary>
    private async Task SaveVersionCacheAsync(string accountKey, IReadOnlyList<ScannedFile> files)
    {
        if (files.Count == 0)
        {
            return;
        }

        try
        {
            var existing = await _versionCache.LoadAsync(accountKey, CancellationToken.None);
            var merged = new Dictionary<string, CachedFileVersions>(existing, StringComparer.Ordinal);
            foreach (var file in files)
            {
                merged[file.Entry.ItemId] = OneDriveScanner.ToCacheEntry(file);
            }

            await _versionCache.SaveAsync(
                accountKey,
                merged.Values,
                DateTimeOffset.UtcNow,
                CancellationToken.None);
        }
        catch (Exception exception) when (exception is IOException or
            UnauthorizedAccessException or JsonException or ArgumentException or NotSupportedException)
        {
            _statusLabel.Text += Loc(
                en: " / the result could not be stored",
                ja: " / 取得結果を保存できませんでした");
        }
    }

    /// <summary>Shared by the scan and the move, which both count the same cooldown down.</summary>
    private static string ThrottleWaitText(TimeSpan wait) => Loc(
        en: $"SharePoint is throttling: waiting another {wait.TotalSeconds:N0} s...",
        ja: $"SharePointの要求制限により あと {wait.TotalSeconds:N0} 秒待機しています...");

    /// <summary>
    /// The version requests are the expensive part, and they are what got the tenant throttled
    /// before. The count is now known exactly before the first one goes out, so the user sees
    /// it and agrees to it.
    /// </summary>
    private bool ConfirmVersionQuery(int fileCount, long thresholdBytes)
    {
        if (fileCount <= 200)
        {
            return true;
        }

        var requestCount = fileCount * SharePointWebSession.RequestsPerVersionRead;
        return MessageBox.Show(
            this,
            Loc(en: $"{fileCount:N0} files of {SizeText.Of(thresholdBytes)} or more " +
                    "cannot be answered from the last read.\n" +
                    "Their version lists will be fetched one file at a time " +
                    $"(about {requestCount:N0} requests).\n\n" +
                    "A large number of these runs into SharePoint throttling.\n" +
                    "Cancelling and raising “File size” narrows it down.\n\n" +
                    "Continue?",
                ja: $"{SizeText.Of(thresholdBytes)} 以上のファイルが {fileCount:N0} 件あり、" +
                    "前回の結果では答えられません。\n" +
                    "これらのバージョン一覧を1ファイルずつ取得します" +
                    $"（およそ {requestCount:N0} 回の要求）。\n\n" +
                    "件数が多いと SharePoint の要求制限にかかります。\n" +
                    "中止して「ファイルサイズが」を大きくすると、件数を絞れます。\n\n" +
                    "このまま続けますか。"),
            Loc(en: "Confirm the number of requests", ja: "取得件数の確認"),
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning) == DialogResult.Yes;
    }

    /// <summary>
    /// Reports what could not be read: local folders the walk was refused, and files the
    /// service would not return history for. Both leave gaps in the same result, so they are
    /// surfaced together rather than as two separate warnings.
    /// </summary>
    private void ReportScanErrors(IReadOnlyList<string> errors)
    {
        if (errors.Count == 0)
        {
            return;
        }

        _statusLabel.Text += Loc(
            en: $" / {errors.Count:N0} items unreadable",
            ja: $" / 読み取れなかった項目 {errors.Count:N0}件");
        MessageBox.Show(
            this,
            Loc(en: $"{errors.Count:N0} items could not be read. They are not in the totals.",
                ja: $"{errors.Count:N0}件を読み取れませんでした。それらは集計に含まれていません。") +
            "\n\n" + string.Join("\n", errors.Take(10)),
            Loc(en: "Some items could not be read", ja: "一部を取得できませんでした"),
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning);
    }

    /// <summary>
    /// The selected rows that may actually be acted on: never the current version, and only
    /// files this scan resolved on the server.
    /// </summary>
    private IReadOnlyList<VersionRow> GetSelectedRows() => _versionsGrid.SelectedRows
        .Cast<DataGridViewRow>()
        .Select(row => row.Tag as VersionRow)
        .OfType<VersionRow>()
        .Where(row => !row.Version.IsCurrent && _serverPathByItemId.ContainsKey(row.File.ItemId))
        .ToArray();

    private static IReadOnlyList<PlannedVersionDeletion> ToPlannedDeletions(
        IEnumerable<VersionRow> rows) => rows
        .Select(row => new PlannedVersionDeletion(row.File, row.Version))
        .ToArray();

    /// <summary>
    /// Counts the planned versions that the current retention policy would have kept. Manual
    /// selection overrides the policy on purpose, so this is shown, not blocked.
    /// </summary>
    private static int CountPolicyProtected(
        VersionDeletionPlan plan,
        IEnumerable<VersionRow> selectedRows)
    {
        var protectedKeys = selectedRows
            .Where(row => !row.IsCandidate)
            .Select(row => (row.File.ItemId, row.Version.VersionId))
            .ToHashSet();

        return plan.Versions.Count(
            planned => protectedKeys.Contains((planned.File.ItemId, planned.Version.VersionId)));
    }

    private async Task<string?> WriteDeletionLogAsync(RetentionPolicy policy, DeletionRunResult result)
    {
        try
        {
            return await new DeletionAuditLog().WriteRunAsync(
                _detectedAccount?.AccountKey ?? "unknown",
                policy,
                result,
                CancellationToken.None);
        }
        catch (Exception exception) when (exception is IOException or
            UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            MessageBox.Show(
                this,
                Loc(en: "The move ran, but the log could not be written.",
                    ja: "移動は実行しましたが、ログを書き出せませんでした。") +
                $"\n{exception.Message}",
                Loc(en: "Move log", ja: "移動ログ"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return null;
        }
    }

    private void ReportDeletionResult(DeletionRunResult result, string? logPath)
    {
        var unknownSuffix = result.UnknownReclaimedSizeCount == 0
            ? ""
            : Loc(en: $" (excluding {result.UnknownReclaimedSizeCount:N0} of unknown size)",
                  ja: $"（サイズ不明 {result.UnknownReclaimedSizeCount:N0}件を除く）");

        var message = new StringBuilder()
            .AppendLine(Loc(
                en: $"Moved to the recycle bin: {result.DeletedCount:N0} / " +
                    $"already gone: {result.AlreadyDeletedCount:N0}",
                ja: $"ごみ箱へ移動: {result.DeletedCount:N0}件 / 既に削除済み: {result.AlreadyDeletedCount:N0}件"))
            .AppendLine(Loc(en: $"Failed: {result.FailureCount:N0}", ja: $"失敗: {result.FailureCount:N0}件"))
            .AppendLine(Loc(
                en: $"Space expected back: {SizeText.Of(result.KnownReclaimedBytes)}",
                ja: $"削減見込み: {SizeText.Of(result.KnownReclaimedBytes)}") + unknownSuffix);

        if (result.NotAttemptedCount > 0)
        {
            message.AppendLine(Loc(
                en: $"Not attempted: {result.NotAttemptedCount:N0}",
                ja: $"未実行: {result.NotAttemptedCount:N0}件"));
        }

        if (result.AbortReason is not null)
        {
            message.AppendLine().AppendLine(
                Loc(en: "Aborted", ja: "中止") + $": {result.AbortReason}");
        }

        message.AppendLine().AppendLine(Loc(
            en: "Local current files, checked:",
            ja: "ローカルの現行ファイル確認:"));
        foreach (var verification in result.Verifications)
        {
            var remaining = verification.RemainingPreviousVersionCount.HasValue
                ? Loc(en: $"{verification.RemainingPreviousVersionCount.Value:N0}",
                      ja: $"{verification.RemainingPreviousVersionCount.Value:N0}件")
                : Loc(en: "could not be checked", ja: "確認できません");
            message.AppendLine(
                $"　{verification.File.Name}: " +
                $"{VersionDeletionRunner.DescribeLocalChange(verification.LocalFile.Change)} / " +
                Loc(en: $"past versions left: {remaining}", ja: $"残り過去版 {remaining}"));
            if (verification.UnexpectedlyPresentVersionIds.Count > 0)
            {
                message.AppendLine(
                    "　　" +
                    Loc(en: "versions that should have moved are still there: ",
                        ja: "移動したはずのバージョンが残っています: ") +
                    string.Join(", ", verification.UnexpectedlyPresentVersionIds));
            }

            if (verification.Error is not null)
            {
                message.AppendLine(
                    "　　" + Loc(en: "re-check error", ja: "再確認エラー") + $": {verification.Error}");
            }
        }

        var failures = result.Outcomes.Where(outcome => !outcome.IsSuccess).Take(10).ToArray();
        if (failures.Length > 0)
        {
            message.AppendLine().AppendLine(Loc(en: "Failures:", ja: "失敗の内訳:"));
            foreach (var failure in failures)
            {
                message.AppendLine(
                    $"　{failure.File.Name} {failure.Version.Label ?? failure.Version.VersionId}: " +
                    $"{DisplayNames.Of(failure.Result.Status)} {failure.Result.Error}");
            }
        }

        if (logPath is not null)
        {
            message.AppendLine().AppendLine(Loc(en: "Log", ja: "ログ") + $": {logPath}");
        }

        var hasProblem = result.FailureCount > 0 ||
            result.AbortReason is not null ||
            result.Verifications.Any(verification => !verification.IsVerified);
        _statusLabel.Text = Loc(
                en: $"Moved to the recycle bin: {result.DeletedCount:N0} succeeded / " +
                    $"{result.FailureCount:N0} failed",
                ja: $"ごみ箱へ移動: 成功 {result.DeletedCount:N0}件 / 失敗 {result.FailureCount:N0}件") +
            (result.WasStopped ? Loc(en: " (stopped)", ja: "（停止しました）") : "");
        MessageBox.Show(
            this,
            message.ToString(),
            hasProblem
                ? Loc(en: "Needs a look", ja: "確認が必要です")
                : Loc(en: "Move result", ja: "移動結果"),
            MessageBoxButtons.OK,
            hasProblem ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
    }

    private void ApplyResults(TimeSpan? scanDuration)
    {
        _retention = new RetentionPolicyEvaluator().Evaluate(
            _files,
            CreatePolicy(),
            DateTimeOffset.UtcNow);
        _aggregate = new ScanAggregator().Aggregate(_files, _retention, scanDuration);
        RefreshSummary();
        RefreshVersionsGrid();
        RefreshFilesGrid();
        _exportButton.Enabled = !_isBusy && _aggregate is not null;
    }

    private void Recalculate()
    {
        if (_files.Count == 0 || _isBusy)
        {
            return;
        }

        try
        {
            ApplyResults(_aggregate?.Summary.ScanDuration);
            _statusLabel.Text = Loc(en: "Rules reapplied.", ja: "保持条件を再計算しました。");
        }
        catch (Exception exception) when (exception is ArgumentException or OverflowException)
        {
            _statusLabel.Text =
                Loc(en: "Rule error", ja: "保持条件エラー") + $": {exception.Message}";
        }
    }

    private void RefreshSummary()
    {
        if (_aggregate is null)
        {
            _summaryLabel.Text = Loc(en: "No data yet.", ja: "データはまだありません。");
            return;
        }

        var summary = _aggregate.Summary;
        var historyUnknown = summary.UnknownPreviousVersionSizeCount == 0
            ? ""
            : Loc(en: $" + {summary.UnknownPreviousVersionSizeCount:N0} of unknown size",
                  ja: $" + サイズ不明 {summary.UnknownPreviousVersionSizeCount:N0}件");
        var reclaimUnknown = summary.UnknownDeleteCandidateSizeCount == 0
            ? ""
            : Loc(en: $" + {summary.UnknownDeleteCandidateSizeCount:N0} of unknown size",
                  ja: $" + サイズ不明 {summary.UnknownDeleteCandidateSizeCount:N0}件");
        _summaryLabel.Text = Loc(
                en: $"Files {summary.TargetFileCount:N0} / " +
                    $"with history {summary.FilesWithPreviousVersions:N0} / " +
                    $"past versions {summary.PreviousVersionCount:N0} / " +
                    $"history {SizeText.Of(summary.KnownPreviousVersionBytes)}",
                ja: $"ファイル {summary.TargetFileCount:N0} / " +
                    $"履歴あり {summary.FilesWithPreviousVersions:N0} / " +
                    $"過去版 {summary.PreviousVersionCount:N0} / " +
                    $"履歴容量 {SizeText.Of(summary.KnownPreviousVersionBytes)}") +
            historyUnknown +
            Loc(en: $" / candidates {summary.DeleteCandidateCount:N0} / " +
                    $"reclaimable {SizeText.Of(summary.KnownReclaimableBytes)}",
                ja: $" / 候補 {summary.DeleteCandidateCount:N0} / " +
                    $"削減見込み {SizeText.Of(summary.KnownReclaimableBytes)}") +
            reclaimUnknown +
            Loc(en: $" / errors {summary.ErrorCount:N0}", ja: $" / エラー {summary.ErrorCount:N0}");
    }

    private void RefreshVersionsGrid()
    {
        _rebuildingVersionsGrid = true;
        _versionsGrid.SuspendLayout();
        try
        {
            _versionsGrid.Rows.Clear();
            if (_retention is null)
            {
                _versionsHeader = null;
                UpdateVersionsHeader();
                return;
            }

            var rows = _retention.Files
                .SelectMany(file => file.Decisions
                    .Where(decision => !decision.Version.IsCurrent)
                    .Select(decision => new VersionRow(file.File, decision.Version, decision)))
                .Where(row => !_candidatesOnlyCheck.Checked || row.IsCandidate)
                .OrderByDescending(row => row.Version.Size ?? -1)
                .ThenBy(row => row.File.CloudPath, StringComparer.OrdinalIgnoreCase)
                .ThenBy(row => row.Version.VersionId, StringComparer.Ordinal)
                .ToArray();

            var displayed = rows.Length <= MaximumDisplayedVersions
                ? rows
                : rows[..MaximumDisplayedVersions];
            _versionsGrid.Rows.AddRange(displayed.Select(CreateVersionGridRow).ToArray());

            var totalBytes = rows
                .Where(row => row.Version.Size is >= 0)
                .Aggregate(0L, (total, row) => checked(total + row.Version.Size!.Value));

            // Kept as numbers rather than as a finished sentence, so a language change can
            // rewrite the heading without rebuilding the rows underneath it.
            _versionsHeader = new VersionsHeader(
                rows.Length,
                totalBytes,
                rows.Length > displayed.Length ? displayed.Length : null);
            UpdateVersionsHeader();
        }
        finally
        {
            _rebuildingVersionsGrid = false;
            _versionsGrid.ResumeLayout();
        }

        RefreshDeleteState();
    }

    private DataGridViewRow CreateVersionGridRow(VersionRow source)
    {
        var row = new DataGridViewRow();

        // Missing values stay null rather than "": a string in a DateTime or long column makes
        // the grid's comparer throw the moment the user clicks that column header to sort.
        // CreateCells predates nullable annotations but accepts nulls inside the array.
        object?[] values =
        [
            source.File.Name,
            source.File.CloudPath,
            source.Version.Label ?? source.Version.VersionId,
            source.Version.Created?.ToLocalTime().DateTime,
            source.Version.ModifiedBy ?? "",
            source.Version.Size,
            source.File.KnownPreviousVersionTotalSize,
            source.Decision is null ? "-" : DisplayNames.Of(source.Decision.Disposition),
            source.Decision is null
                ? ""
                : string.Join(", ", source.Decision.Reasons.Select(DisplayNames.Of)),
        ];
        row.CreateCells(_versionsGrid, values!);
        row.Tag = source;
        if (source.IsCandidate)
        {
            row.DefaultCellStyle.BackColor = Color.MistyRose;
            row.DefaultCellStyle.ForeColor = Color.DarkRed;
        }

        return row;
    }

    private void RefreshFilesGrid()
    {
        _filesGrid.SuspendLayout();
        try
        {
            _filesGrid.Rows.Clear();
            if (_aggregate is null)
            {
                return;
            }

            var rows = _aggregate.Files
                .OrderByDescending(file => file.KnownPreviousVersionBytes)
                .ThenBy(file => file.File.CloudPath, StringComparer.OrdinalIgnoreCase)
                .Take(MaximumDisplayedVersions)
                .Select(CreateFileGridRow)
                .ToArray();
            _filesGrid.Rows.AddRange(rows);
        }
        finally
        {
            _filesGrid.ResumeLayout();
        }
    }

    private DataGridViewRow CreateFileGridRow(FileScanAggregate file)
    {
        var row = new DataGridViewRow();
        object?[] values =
        [
            file.File.Name,
            file.File.CloudPath,
            file.CurrentSizeBytes,
            file.PreviousVersionCount,
            file.KnownPreviousVersionBytes,
            file.DeleteCandidateCount,
            file.KnownReclaimableBytes,
            file.File.LastModified?.ToLocalTime().DateTime,
            DisplayNames.Of(file.File.RetrievalStatus),
        ];
        row.CreateCells(_filesGrid, values!);
        row.Tag = file;
        if (file.ErrorCount > 0)
        {
            row.DefaultCellStyle.BackColor = Color.MistyRose;
        }

        return row;
    }

    private void RefreshDeleteState()
    {
        var selected = _isBusy ? 0 : GetSelectedRows().Count;
        _deleteButton.Enabled = selected > 0 && _sharePointSession?.IsVerified == true;
        _deleteButton.Text = selected > 0
            ? Loc(en: $"Move selection to recycle bin ({selected:N0})",
                  ja: $"選択をごみ箱へ移動 ({selected:N0}件)")
            : Loc(en: "Move selection to recycle bin", ja: "選択をごみ箱へ移動");
    }

    private async void ExportButtonOnClick(object? sender, EventArgs eventArgs)
    {
        if (_aggregate is null || _isBusy)
        {
            return;
        }

        using var dialog = new FolderBrowserDialog
        {
            Description = Loc(
                en: "Choose where to write the CSV and JSON",
                ja: "CSV と JSON の保存先を選択してください"),
            UseDescriptionForTitle = true,
            ShowNewFolderButton = true,
        };
        try
        {
            if (!string.IsNullOrWhiteSpace(_settings.ExportDirectory))
            {
                var configuredDirectory = Path.GetFullPath(
                    Environment.ExpandEnvironmentVariables(_settings.ExportDirectory));
                if (Directory.Exists(configuredDirectory))
                {
                    dialog.InitialDirectory = configuredDirectory;
                }
            }
        }
        catch (Exception exception) when (exception is ArgumentException or
            IOException or NotSupportedException)
        {
            MessageBox.Show(
                this,
                Loc(en: "The configured export directory is not usable. Choose one by hand.",
                    ja: "既定の出力先が無効です。保存先を手動で選択してください。") +
                $"\n{exception.Message}",
                Loc(en: "Export directory", ja: "出力先の警告"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var aggregateSnapshot = _aggregate;
        using var cancellation = new CancellationTokenSource();
        _operationCancellation = cancellation;
        SetBusy(true, Loc(en: "Writing the CSV and JSON...", ja: "CSV / JSON を出力しています..."));
        try
        {
            var paths = await WriteExportPairAsync(
                dialog.SelectedPath,
                aggregateSnapshot,
                cancellation.Token);
            _statusLabel.Text =
                Loc(en: "Written", ja: "出力しました") + $": {paths.CsvPath} / {paths.JsonPath}";
        }
        catch (OperationCanceledException)
        {
            _statusLabel.Text = Loc(en: "The export was stopped.", ja: "出力を停止しました。");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or
            ArgumentException or NotSupportedException or JsonException)
        {
            _statusLabel.Text = Loc(en: "The export failed.", ja: "出力に失敗しました。");
            MessageBox.Show(
                this,
                exception.Message,
                Loc(en: "Export error", ja: "出力エラー"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            _operationCancellation = null;
            SetBusy(false, _statusLabel.Text);
        }
    }

    private static async Task<(string CsvPath, string JsonPath)> WriteExportPairAsync(
        string directory,
        ScanAggregate aggregate,
        CancellationToken cancellationToken)
    {
        var normalizedDirectory = Path.GetFullPath(directory);
        Directory.CreateDirectory(normalizedDirectory);
        var identifier = $"{DateTimeOffset.Now:yyyyMMdd-HHmmss-fff}-{Guid.NewGuid():N}"[..32];
        var csvPath = Path.Combine(normalizedDirectory, $"scan-{identifier}.csv");
        var jsonPath = Path.Combine(normalizedDirectory, $"scan-{identifier}.json");
        var csvTemporaryPath = csvPath + ".tmp";
        var jsonTemporaryPath = jsonPath + ".tmp";
        var csvCommitted = false;

        try
        {
            var exporter = new ScanResultExporter();
            await exporter.WriteCsvAsync(
                csvTemporaryPath,
                aggregate,
                cancellationToken: cancellationToken);
            await exporter.WriteJsonAsync(
                jsonTemporaryPath,
                aggregate,
                cancellationToken: cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            File.Move(csvTemporaryPath, csvPath);
            csvCommitted = true;
            File.Move(jsonTemporaryPath, jsonPath);
            return (csvPath, jsonPath);
        }
        catch
        {
            if (csvCommitted)
            {
                TryDeleteTemporaryFile(csvPath);
            }

            throw;
        }
        finally
        {
            TryDeleteTemporaryFile(csvTemporaryPath);
            TryDeleteTemporaryFile(jsonTemporaryPath);
        }
    }

    private static void TryDeleteTemporaryFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Best-effort cleanup. The original export result or exception remains authoritative.
        }
    }

    private SharePointWebSession GetOrCreateSharePointSession(OneDriveBusinessAccount account)
    {
        if (_sharePointSession is not null &&
            _detectedAccount is not null &&
            Uri.Compare(
                _detectedAccount.SiteUri,
                account.SiteUri,
                UriComponents.AbsoluteUri,
                UriFormat.SafeUnescaped,
                StringComparison.OrdinalIgnoreCase) == 0)
        {
            return _sharePointSession;
        }

        _sharePointSession?.Dispose();
        _sharePointSession = new SharePointWebSession(account.SiteUri, account.ServiceEndpointUri);
        _detectedAccount = account;
        return _sharePointSession;
    }

    private RetentionPolicy CreatePolicy() => new()
    {
        KeepLatestVersions = decimal.ToInt32(_keepVersionsInput.Value),
        KeepDays = decimal.ToInt32(_keepDaysInput.Value),
        ProtectRecentlyModifiedFileDays = decimal.ToInt32(_protectDaysInput.Value),
        MinimumHistoryBytes = checked(decimal.ToInt64(_minimumHistoryInput.Value) * Mebibyte),
        MinimumCurrentFileBytes = checked(decimal.ToInt64(_minimumFileSizeInput.Value) * Mebibyte),
    };

    private OneDriveBusinessAccount ValidateScanInputs()
    {
        if (!Directory.Exists(_targetBox.Text))
        {
            throw new ArgumentException(Loc(
                en: "Choose a folder inside the OneDrive sync folder.",
                ja: "OneDrive同期フォルダー配下のフォルダーを指定してください。"));
        }

        var account = _accountDiscovery.FindByLocalPath(_targetBox.Text)
            ?? throw new InvalidOperationException(Loc(
                en: "No OneDrive for Business account matches that folder.",
                ja: "指定フォルダーに対応するOneDrive for Businessアカウントを検出できませんでした。"));
        _ = LocalPathMapper.Map(account.UserFolder, _targetBox.Text);
        _rootBox.Text = account.UserFolder;
        return account;
    }

    private void DetectOneDriveAccount(bool showFailure)
    {
        var accounts = _accountDiscovery.Discover();
        var candidatePath = string.IsNullOrWhiteSpace(_targetBox.Text)
            ? _settings.LocalOneDriveRoot
            : _targetBox.Text;
        var account = OneDriveBusinessAccountDiscovery.FindBestMatch(accounts, candidatePath)
            ?? accounts.FirstOrDefault();
        if (account is null)
        {
            _detectedAccount = null;
            RefreshAccountLabel();
            if (showFailure)
            {
                MessageBox.Show(
                    this,
                    Loc(en: "No Business account was found in the Windows OneDrive settings. " +
                            "Check that OneDrive is running and has finished syncing.",
                        ja: "WindowsのOneDrive設定からBusinessアカウントを検出できませんでした。" +
                            "OneDriveが起動して同期済みか確認してください。"),
                    Loc(en: "OneDrive detection", ja: "OneDrive自動検出"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }

            return;
        }

        if (_detectedAccount is not null &&
            !string.Equals(
                _detectedAccount.AccountKey,
                account.AccountKey,
                StringComparison.OrdinalIgnoreCase))
        {
            _sharePointSession?.Dispose();
            _sharePointSession = null;
        }

        _detectedAccount = account;
        _rootBox.Text = account.UserFolder;
        if (string.IsNullOrWhiteSpace(_targetBox.Text))
        {
            _targetBox.Text = account.UserFolder;
        }

        RefreshAccountLabel();
    }

    private void BrowseForTargetFolder()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = Loc(
                en: "Choose the folder under OneDrive whose version history to look at",
                ja: "バージョン履歴を調べるOneDrive配下のフォルダーを選択してください"),
            UseDescriptionForTitle = true,
            ShowNewFolderButton = false,
        };
        if (Directory.Exists(_targetBox.Text))
        {
            dialog.InitialDirectory = _targetBox.Text;
        }

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        _targetBox.Text = dialog.SelectedPath;
        DetectOneDriveAccount(showFailure: true);
    }

    /// <summary>
    /// Accepts a folder, or the containing folder when a file is supplied, so a stale
    /// file-based setting still resolves to something scannable.
    /// </summary>
    private static string NormalizeTargetFolder(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        return Directory.Exists(path)
            ? path
            : File.Exists(path) ? Path.GetDirectoryName(path) ?? string.Empty : path;
    }

    private void TargetBoxOnDragEnter(object? sender, DragEventArgs eventArgs)
    {
        eventArgs.Effect = GetDroppedFolder(eventArgs.Data) is null
            ? DragDropEffects.None
            : DragDropEffects.Copy;
    }

    private void TargetBoxOnDragDrop(object? sender, DragEventArgs eventArgs)
    {
        var folder = GetDroppedFolder(eventArgs.Data);
        if (folder is not null)
        {
            _targetBox.Text = folder;
            DetectOneDriveAccount(showFailure: true);
        }
    }

    private static string? GetDroppedFolder(IDataObject? data)
    {
        if (data?.GetData(DataFormats.FileDrop) is not string[] paths)
        {
            return null;
        }

        return paths.Select(NormalizeTargetFolder).FirstOrDefault(Directory.Exists);
    }

    private void RequestStop()
    {
        _statusLabel.Text = Loc(
            en: "Stop requested. Waiting for the requests already sent to finish...",
            ja: "停止要求を送信しました。進行中の要求の完了を待っています...");
        _operationCancellation?.Cancel();
        _stopButton.Enabled = false;
    }

    private void SetBusy(bool busy, string status)
    {
        _isBusy = busy;
        _probeButton.Enabled = !busy;
        _scanButton.Enabled = !busy;
        _browseRootButton.Enabled = !busy;
        _browseTargetButton.Enabled = !busy;
        _rootBox.Enabled = !busy;
        _targetBox.Enabled = !busy;
        _keepVersionsInput.Enabled = !busy;
        _keepDaysInput.Enabled = !busy;
        _protectDaysInput.Enabled = !busy;
        _minimumHistoryInput.Enabled = !busy;
        _minimumFileSizeInput.Enabled = !busy;
        _cacheAgeInput.Enabled = !busy;
        _candidatesOnlyCheck.Enabled = !busy;

        // Changing language mid-run would rewrite the progress line under the operation that is
        // writing it, and re-translate results that are still being produced.
        _languageMenu.Enabled = !busy;
        _stopButton.Enabled = busy;
        _exportButton.Enabled = !busy && _aggregate is not null;
        _progressBar.Visible = busy;
        _statusLabel.Text = status;
        RefreshDeleteState();

        // The close was held back until the operation finished; finish it now. Posted rather
        // than called, so the operation's own finally block unwinds first.
        if (!busy && _closeWhenIdle && !IsDisposed)
        {
            BeginInvoke(Close);
        }
    }

    private void VersionsGridOnCellFormatting(object? sender, DataGridViewCellFormattingEventArgs eventArgs)
    {
        if (eventArgs.RowIndex < 0)
        {
            return;
        }

        switch (_versionsGrid.Columns[eventArgs.ColumnIndex].Name)
        {
            case "VersionSize":
                eventArgs.Value = _versionsGrid.Rows[eventArgs.RowIndex].Tag is VersionRow row
                    ? SizeText.Of(row.Version.Size)
                    : Loc(en: "Unknown", ja: "不明");
                eventArgs.FormattingApplied = true;
                break;
            case "FileHistorySize" when eventArgs.Value is long historyBytes:
                eventArgs.Value = SizeText.Of(historyBytes);
                eventArgs.FormattingApplied = true;
                break;
            case "Created" when eventArgs.Value is DateTime date:
                eventArgs.Value = date.ToString("yyyy-MM-dd HH:mm:ss");
                eventArgs.FormattingApplied = true;
                break;
        }
    }

    private void FilesGridOnCellFormatting(object? sender, DataGridViewCellFormattingEventArgs eventArgs)
    {
        if (eventArgs.RowIndex < 0 ||
            _filesGrid.Rows[eventArgs.RowIndex].Tag is not FileScanAggregate file)
        {
            return;
        }

        switch (_filesGrid.Columns[eventArgs.ColumnIndex].Name)
        {
            case "CurrentSize":
                eventArgs.Value = SizeText.Of(file.CurrentSizeBytes);
                eventArgs.FormattingApplied = true;
                break;
            case "HistorySize":
                eventArgs.Value = SizeText.Of(file.KnownPreviousVersionBytes) +
                                  (file.UnknownPreviousVersionSizeCount == 0
                                      ? ""
                                      : $" +?({file.UnknownPreviousVersionSizeCount})");
                eventArgs.FormattingApplied = true;
                break;
            case "Reclaimable":
                eventArgs.Value = SizeText.Of(file.KnownReclaimableBytes) +
                                  (file.UnknownDeleteCandidateSizeCount == 0
                                      ? ""
                                      : $" +?({file.UnknownDeleteCandidateSizeCount})");
                eventArgs.FormattingApplied = true;
                break;
            case "LastModified" when eventArgs.Value is DateTime date:
                eventArgs.Value = date.ToString("yyyy-MM-dd HH:mm");
                eventArgs.FormattingApplied = true;
                break;
        }
    }

    private static void SetNumber(NumericUpDown input, long value)
    {
        input.Value = Math.Clamp((decimal)value, input.Minimum, input.Maximum);
    }
}
