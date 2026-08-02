using System.Diagnostics;
using System.Text;
using System.Text.Json;
using OneDriveVersionCleaner.Core.Aggregation;
using OneDriveVersionCleaner.Core.Auditing;
using OneDriveVersionCleaner.Core.Caching;
using OneDriveVersionCleaner.Core.Deletion;
using OneDriveVersionCleaner.Core.Export;
using OneDriveVersionCleaner.Core.Formatting;
using OneDriveVersionCleaner.Core.Models;
using OneDriveVersionCleaner.Core.Paths;
using OneDriveVersionCleaner.Core.Retention;
using OneDriveVersionCleaner.Infrastructure.OneDriveAccounts;

namespace OneDriveVersionCleaner.App;

internal sealed class MainForm : Form
{
    private const long Mebibyte = 1024L * 1024;

    /// <summary>
    /// The grid is sorted largest first, so anything past this point cannot matter for
    /// reclaiming space. The full set still reaches the CSV and JSON exports.
    /// </summary>
    private const int MaximumDisplayedVersions = 5_000;

    private readonly AppSettings _settings;
    private readonly string? _settingsWarning;
    private readonly OneDriveBusinessAccountDiscovery _accountDiscovery = new();
    private readonly TextBox _rootBox = new();
    private readonly TextBox _targetBox = new();
    private readonly NumericUpDown _keepVersionsInput = new();
    private readonly NumericUpDown _keepDaysInput = new();
    private readonly NumericUpDown _protectDaysInput = new();
    private readonly NumericUpDown _minimumHistoryInput = new();
    private readonly NumericUpDown _minimumFileSizeInput = new();
    private readonly NumericUpDown _cacheAgeInput = new();
    private readonly VersionCacheStore _versionCache = new();
    private readonly NumericUpDown _maximumCandidatesInput = new();
    private readonly Button _browseRootButton = new();
    private readonly Button _browseTargetButton = new();
    private readonly Button _probeButton = new();
    private readonly Button _scanButton = new();
    private readonly Button _stopButton = new();
    private readonly Button _exportButton = new();
    private readonly Button _deleteButton = new();
    private readonly CheckBox _candidatesOnlyCheck = new();
    private readonly ToolTip _toolTip = new() { AutoPopDelay = 20_000 };
    private readonly Label _accountLabel = new();
    private readonly Label _summaryLabel = new();
    private readonly Label _versionsHeaderLabel = new();
    private readonly Label _statusLabel = new();
    private readonly ProgressBar _progressBar = new();
    private readonly DataGridView _versionsGrid = new();
    private readonly DataGridView _filesGrid = new();

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

    /// <summary>Rebuilding the grid raises SelectionChanged once per row; ignore those.</summary>
    private bool _rebuildingVersionsGrid;

    private sealed record VersionRow(
        FileEntry File,
        VersionEntry Version,
        VersionRetentionDecision? Decision)
    {
        public bool IsCandidate => Decision?.IsDeleteCandidate == true;
    }

    public MainForm(AppSettings settings, string? settingsWarning)
    {
        _settings = settings;
        _settingsWarning = settingsWarning;

        Text = "OneDrive Version History Cleaner";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1100, 720);
        Size = new Size(1360, 860);
        AutoScaleMode = AutoScaleMode.Dpi;

        BuildInterface();
        ApplySettings();
        WireEvents();
        RefreshDeleteState();

        Shown += (_, _) =>
        {
            if (!string.IsNullOrWhiteSpace(_settingsWarning))
            {
                MessageBox.Show(
                    this,
                    _settingsWarning,
                    "設定の警告",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        };
    }

    private void BuildInterface()
    {
        var rootLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(10),
        };
        rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        rootLayout.Controls.Add(BuildConnectionGroup(), 0, 0);
        rootLayout.Controls.Add(BuildPolicyGroup(), 0, 1);
        rootLayout.Controls.Add(BuildSummaryPanel(), 0, 2);
        rootLayout.Controls.Add(BuildGridArea(), 0, 3);
        rootLayout.Controls.Add(BuildStatusPanel(), 0, 4);
        Controls.Add(rootLayout);
    }

    private Control BuildConnectionGroup()
    {
        var group = new GroupBox
        {
            Text = "接続と対象（管理センター不要／自分の OneDrive のみ）",
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(8),
        };
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 3,
            RowCount = 4,
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        _accountLabel.AutoSize = true;
        _accountLabel.Text = "OneDriveアカウントを自動検出しています...";
        _accountLabel.Padding = new Padding(0, 2, 0, 6);
        layout.Controls.Add(_accountLabel, 0, 0);
        layout.SetColumnSpan(_accountLabel, 3);

        ConfigureTextBox(_rootBox);
        _rootBox.ReadOnly = true;
        _browseRootButton.Text = "再検出";
        _browseRootButton.AutoSize = true;
        AddLabeledRow(layout, 1, "同期ルート", _rootBox, _browseRootButton);

        ConfigureTextBox(_targetBox);
        _targetBox.AllowDrop = true;
        _browseTargetButton.Text = "フォルダー選択...";
        _browseTargetButton.AutoSize = true;
        AddLabeledRow(layout, 2, "対象フォルダー", _targetBox, _browseTargetButton);

        var actions = new FlowLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(0, 6, 0, 0),
        };
        _probeButton.Text = "接続テスト (1要求)";
        _probeButton.AutoSize = true;
        _scanButton.Text = "Webでサインインして全履歴を取得";
        _scanButton.AutoSize = true;
        _stopButton.Text = "停止";
        _stopButton.AutoSize = true;
        _stopButton.Enabled = false;
        _exportButton.Text = "CSV / JSON 出力";
        _exportButton.AutoSize = true;
        _exportButton.Enabled = false;
        _deleteButton.Text = "選択をごみ箱へ移動";
        _deleteButton.AutoSize = true;
        _deleteButton.Enabled = false;
        _deleteButton.ForeColor = Color.Firebrick;
        actions.Controls.AddRange(
            [_probeButton, _scanButton, _stopButton, _exportButton, _deleteButton]);
        layout.Controls.Add(actions, 1, 3);
        layout.SetColumnSpan(actions, 2);

        group.Controls.Add(layout);
        return group;
    }

    private Control BuildPolicyGroup()
    {
        var group = new GroupBox
        {
            Text = "保持条件（変更すると即時再計算）",
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(8),
        };
        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
        };

        ConfigureNumber(_keepVersionsInput, 0, 100000);
        ConfigureNumber(_keepDaysInput, 0, 36500);
        ConfigureNumber(_protectDaysInput, 0, 36500);
        ConfigureNumber(_minimumHistoryInput, 0, 10_000_000);
        ConfigureNumber(_minimumFileSizeInput, 0, 10_000_000);
        ConfigureNumber(_cacheAgeInput, 0, 36500);
        ConfigureNumber(_maximumCandidatesInput, 0, 1_000_000);

        AddPolicyControl(
            flow, "保持する履歴数", _keepVersionsInput, "件",
            "各ファイルについて、新しいものから数えてこの件数の過去バージョンを残します。0で無効。");
        AddPolicyControl(
            flow, "最終更新から保持", _keepDaysInput, "日",
            "そのファイルの最終更新日時から遡ってこの日数以内に作られた過去バージョンを残します。\n" +
            "現在時刻ではなくファイル自身の更新日時が基準です。0で無効。");
        AddPolicyControl(
            flow, "更新直後は対象外", _protectDaysInput, "日以内",
            "現在時刻からこの日数以内に更新されたファイルは、丸ごと処理対象から外します。0で無効。");
        AddPolicyControl(
            flow, "処理する最小ファイルサイズ", _minimumFileSizeInput, "MB以上",
            "現行サイズがこの値以上のファイルだけを処理します。\n" +
            "小さいファイルは何世代あっても削減量が知れているため、サーバーに問い合わせません。\n" +
            "サイズはローカルの同期フォルダーから読むので、この絞り込みに通信は発生しません。0で無効。");
        AddPolicyControl(
            flow, "処理する最小履歴容量", _minimumHistoryInput, "MB以上",
            "過去バージョンの合計がこの値以上のファイルだけを処理します。\n" +
            "これ未満のファイルは丸ごと対象外です。0で無効。");
        AddPolicyControl(
            flow, "再問い合わせの間隔", _cacheAgeInput, "日以上",
            "一度取得したバージョン履歴は保存され、次回以降は再利用します。\n" +
            "同じファイルを問い合わせ直すのは、前回の取得からこの日数が経ち、かつ\n" +
            "ファイルが更新されている場合だけです。\n" +
            "履歴は編集したときにしか増えないため、更新のないファイルは問い合わせません。0で毎回。");
        AddPolicyControl(
            flow, "1回の最大移動数", _maximumCandidatesInput, "件",
            "1回の実行でごみ箱へ移動する上限です。超えた分は次回に見送ります。");

        _candidatesOnlyCheck.Text = "削除候補のみ表示";
        _candidatesOnlyCheck.AutoSize = true;
        _candidatesOnlyCheck.Checked = true;
        _candidatesOnlyCheck.Margin = new Padding(16, 7, 3, 3);
        flow.Controls.Add(_candidatesOnlyCheck);

        group.Controls.Add(flow);
        return group;
    }

    private Control BuildSummaryPanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 1,
            RowCount = 1,
            Padding = new Padding(4, 8, 4, 8),
        };
        _summaryLabel.AutoSize = true;
        _summaryLabel.Text = "データはまだありません。";
        panel.Controls.Add(_summaryLabel, 0, 0);
        return panel;
    }

    private Control BuildGridArea()
    {
        ConfigureVersionsGrid();
        ConfigureFilesGrid();

        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterDistance = 440,
            Panel1MinSize = 220,
            Panel2MinSize = 120,
        };

        _versionsHeaderLabel.Text = "過去バージョン";
        split.Panel1.Controls.Add(WrapGrid(_versionsHeaderLabel, _versionsGrid));
        split.Panel2.Controls.Add(WrapGrid(
            new Label { Text = "ファイル別の履歴容量" },
            _filesGrid));
        return split;
    }

    private Control BuildStatusPanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 1,
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
        _statusLabel.AutoSize = true;
        _statusLabel.Text = "準備完了";
        _statusLabel.Anchor = AnchorStyles.Left;
        _progressBar.Dock = DockStyle.Fill;
        _progressBar.Style = ProgressBarStyle.Marquee;
        _progressBar.Visible = false;
        panel.Controls.Add(_statusLabel, 0, 0);
        panel.Controls.Add(_progressBar, 1, 0);
        return panel;
    }

    private void ConfigureVersionsGrid()
    {
        ConfigureGrid(_versionsGrid);
        _versionsGrid.MultiSelect = true;
        _versionsGrid.Columns.Add(CreateTextColumn("FileName", "ファイル名", 180));
        _versionsGrid.Columns.Add(CreateTextColumn("CloudPath", "パス", 320));
        _versionsGrid.Columns.Add(CreateTextColumn("VersionLabel", "バージョン", 90));
        _versionsGrid.Columns.Add(CreateTextColumn("Created", "作成日時", 150, valueType: typeof(DateTime)));
        _versionsGrid.Columns.Add(CreateTextColumn("ModifiedBy", "更新者", 130));
        _versionsGrid.Columns.Add(CreateTextColumn("VersionSize", "サイズ", 105, valueType: typeof(long)));
        _versionsGrid.Columns.Add(
            CreateTextColumn("FileHistorySize", "ファイル履歴計", 115, valueType: typeof(long)));
        _versionsGrid.Columns.Add(CreateTextColumn("Decision", "判定", 110));
        _versionsGrid.Columns.Add(CreateTextColumn("Reasons", "理由", 260));
        _versionsGrid.CellFormatting += VersionsGridOnCellFormatting;
    }

    private void ConfigureFilesGrid()
    {
        ConfigureGrid(_filesGrid);
        _filesGrid.ContextMenuStrip = BuildFileContextMenu();
        _filesGrid.CellMouseDown += FilesGridOnCellMouseDown;
        _filesGrid.Columns.Add(CreateTextColumn("FileName", "ファイル名", 180));
        _filesGrid.Columns.Add(CreateTextColumn("CloudPath", "パス", 320));
        _filesGrid.Columns.Add(CreateTextColumn("CurrentSize", "現行サイズ", 100, valueType: typeof(long)));
        _filesGrid.Columns.Add(CreateTextColumn("HistoryCount", "履歴数", 75, valueType: typeof(int)));
        _filesGrid.Columns.Add(CreateTextColumn("HistorySize", "履歴容量", 115, valueType: typeof(long)));
        _filesGrid.Columns.Add(CreateTextColumn("CandidateCount", "候補数", 75, valueType: typeof(int)));
        _filesGrid.Columns.Add(CreateTextColumn("Reclaimable", "削減見込み", 115, valueType: typeof(long)));
        _filesGrid.Columns.Add(CreateTextColumn("LastModified", "最終更新", 140, valueType: typeof(DateTime)));
        _filesGrid.Columns.Add(CreateTextColumn("Status", "状態", 90));
        _filesGrid.CellFormatting += FilesGridOnCellFormatting;
    }

    private ContextMenuStrip BuildFileContextMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("フォルダーを開く", null, (_, _) => OpenSelectedFileFolder());
        menu.Items.Add("Webで開く", null, (_, _) => OpenSelectedFileOnWeb());
        menu.Opening += (_, eventArgs) =>
        {
            var selected = SelectedFile();
            eventArgs.Cancel = selected is null;
            if (selected is not null)
            {
                menu.Items[0].Enabled = File.Exists(selected.File.LocalPath);
                menu.Items[1].Enabled = !string.IsNullOrWhiteSpace(selected.File.WebUrl);
            }
        };
        return menu;
    }

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
                $"ローカルに見つかりませんでした。\n{localPath}",
                "フォルダーを開く",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception exception) when (exception is System.ComponentModel.Win32Exception or
            InvalidOperationException or IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(
                this,
                exception.Message,
                "フォルダーを開けませんでした",
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
                "Webで開けませんでした",
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
        SetNumber(_maximumCandidatesInput, _settings.MaximumDeletesPerRun);
    }

    private void WireEvents()
    {
        _browseRootButton.Click += (_, _) => DetectOneDriveAccount(showFailure: true);
        _browseTargetButton.Click += (_, _) => BrowseForTargetFolder();
        _probeButton.Click += ProbeButtonOnClick;
        _scanButton.Click += ScanButtonOnClick;
        _stopButton.Click += (_, _) => RequestStop();
        _exportButton.Click += ExportButtonOnClick;
        _deleteButton.Click += DeleteButtonOnClick;
        _versionsGrid.SelectionChanged += (_, _) =>
        {
            if (!_rebuildingVersionsGrid)
            {
                RefreshDeleteState();
            }
        };
        _candidatesOnlyCheck.CheckedChanged += (_, _) => RefreshVersionsGrid();
        _targetBox.DragEnter += TargetBoxOnDragEnter;
        _targetBox.DragDrop += TargetBoxOnDragDrop;
        _keepVersionsInput.ValueChanged += (_, _) => Recalculate();
        _keepDaysInput.ValueChanged += (_, _) => Recalculate();
        _protectDaysInput.ValueChanged += (_, _) => Recalculate();
        _minimumHistoryInput.ValueChanged += (_, _) => Recalculate();
        _minimumFileSizeInput.ValueChanged += (_, _) => Recalculate();
        FormClosing += (_, _) =>
        {
            _operationCancellation?.Cancel();
            _sharePointSession?.Dispose();
        };
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
                "OneDrive for Businessアカウントを検出できていません。「再検出」を押してください。",
                "接続テスト",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        using var cancellation = new CancellationTokenSource();
        _operationCancellation = cancellation;
        SetBusy(true, "接続テストを実行しています... (1要求)");
        try
        {
            var session = GetOrCreateSharePointSession(_detectedAccount);
            session.ResetThrottleState();
            if (!session.IsVerified && session.ShowDialog(this) != DialogResult.OK)
            {
                _statusLabel.Text = "接続テストを中止しました。";
                return;
            }

            var stopwatch = Stopwatch.StartNew();
            var site = await session.VerifyAsync(cancellation.Token);
            stopwatch.Stop();

            _statusLabel.Text =
                $"接続テスト成功: {site.Title}（{stopwatch.ElapsedMilliseconds:N0} ミリ秒 / " +
                $"待機 {session.ThrottleEventCount:N0}回）";
            MessageBox.Show(
                this,
                session.ThrottleEventCount == 0
                    ? "要求は制限なしで通りました。走査を始められます。\n" +
                      "まずは小さいサブフォルダーから試してください。"
                    : $"要求は通りましたが、{session.ThrottleEventCount:N0}回の待機が入りました。\n" +
                      "まだ制限が残っています。もう少し時間をおいてください。",
                "接続テスト",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (SharePointThrottledException)
        {
            _statusLabel.Text = "接続テスト: まだ制限中です。";
            MessageBox.Show(
                this,
                "まだ SharePoint の要求制限が続いています。\n\n" +
                "待ち時間を倍にして、あらためて試してください（1時間 → 2時間 → 4時間）。\n" +
                "この間、走査は実行しないでください。試行を重ねると制限が延びます。",
                "接続テスト",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
        catch (OperationCanceledException)
        {
            _statusLabel.Text = "接続テストを停止しました。";
        }
        catch (Exception exception)
        {
            _statusLabel.Text = "接続テストに失敗しました。";
            MessageBox.Show(
                this,
                exception.Message,
                "接続テスト",
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
            SetBusy(true, "本人のOneDriveサイトを開いています...");

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

                    throw new OperationCanceledException("Microsoft 365へのサインインを中止しました。");
                }
            }
            else
            {
                await session.VerifyAsync(cancellation.Token);
            }

            _accountLabel.Text =
                $"検出済み: {account.DisplayName} ({account.AccountKey}) / 本人セッション確認済み";
            _statusLabel.Text = "OneDriveのドキュメントルートを確認しています...";
            var library = await session.GetDefaultDocumentLibraryRootAsync(cancellation.Token);
            var libraryRoot = library.Path;
            var mapping = LocalPathMapper.Map(account.UserFolder, _targetBox.Text);

            // Only the library root arrives with its unique id. A chosen subfolder is addressed
            // by path for the first read; every folder below it comes from a listing that does
            // carry an id, so the fallback is available from the second level down.
            var scanRoot = mapping.CloudPathSegments.Count == 0
                ? library
                : new SharePointFolderInfo
                {
                    ServerRelativeUrl = CombineServerRelativePath(libraryRoot, mapping.CloudPathSegments),
                };

            var versioning = await session.GetVersioningInfoAsync(cancellation.Token);
            session.ResetThrottleState();
            var progress = new Progress<ScanProgress>(report => _statusLabel.Text =
                $"バージョン履歴を取得しています... フォルダー {report.FoldersCompleted:N0}/{report.FoldersDiscovered:N0} / " +
                $"ファイル {report.FilesFound:N0}件");

            void OnThrottleWait(object? _, TimeSpan wait) => _statusLabel.Text =
                $"SharePointの要求制限により あと {wait.TotalSeconds:N0} 秒待機しています...";
            session.ThrottleWaitStarted += OnThrottleWait;
            ScanResult scanned;
            try
            {
                scanned = await new OneDriveScanner(session, account, libraryRoot, versioning)
                    .ScanBulkAsync(
                        scanRoot.Path,
                        CreatePolicy().MinimumHistoryBytes,
                        CreatePolicy().MinimumCurrentFileBytes,
                        new VersionCacheOptions(
                            await _versionCache.LoadAsync(account.AccountKey, cancellation.Token),
                            TimeSpan.FromDays(decimal.ToDouble(_cacheAgeInput.Value))),
                        ConfirmDetailPhase,
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
            await SaveVersionCacheAsync(account.AccountKey, scanned);
            ApplyResults(stopwatch.Elapsed);

            var reuse = scanned.ReusedFromCacheCount == 0
                ? ""
                : $" / 前回結果を再利用 {scanned.ReusedFromCacheCount:N0}件";
            _statusLabel.Text =
                $"取得完了: {_files.Count:N0}ファイル / 過去版 {_files.Sum(file => file.PreviousVersionCount):N0}件 / " +
                $"問い合わせ {scanned.DetailedFileCount:N0}件{reuse}（{stopwatch.Elapsed.TotalSeconds:N1}秒" +
                $"{(scanned.UsedBulkListing ? "" : " / 一覧取得は使えず全巡回")}）";
            ReportScanErrors(scanned.FolderErrors);
        }
        catch (OperationCanceledException)
        {
            _statusLabel.Text = "処理を停止しました。";
        }
        catch (SharePointThrottledException exception)
        {
            _statusLabel.Text = "SharePointの要求制限により中断しました。";
            MessageBox.Show(
                this,
                $"{exception.Message}\n\n" +
                "対処:\n" +
                "・数分から数十分おいてから再実行してください\n" +
                "・同期ルート全体ではなく、サブフォルダーに分けて実行してください\n" +
                "・他のOneDrive操作（大量同期など）が動いていないか確認してください",
                "SharePointの要求制限",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
        catch (Exception exception)
        {
            _statusLabel.Text = "取得に失敗しました。";
            MessageBox.Show(
                this,
                exception.Message,
                "取得エラー",
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
                "先に「Webでサインインして全履歴を取得」を実行してください。",
                "ごみ箱へ移動",
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
                "移動する過去バージョンを表から選択してください。ShiftまたはCtrlで複数選択できます。",
                "ごみ箱へ移動",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        var policy = CreatePolicy();
        VersionDeletionPlan plan;
        try
        {
            plan = new VersionDeletionPlanner().Create(selection, policy.MaximumDeletesPerRun);
        }
        catch (InvalidOperationException exception)
        {
            MessageBox.Show(
                this,
                $"移動計画を作成できませんでした。処理を中止します。\n{exception.Message}",
                "移動計画",
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
                _statusLabel.Text = "ごみ箱への移動を中止しました。";
                return;
            }
        }

        using var cancellation = new CancellationTokenSource();
        _operationCancellation = cancellation;
        SetBusy(true, $"ごみ箱へ移動しています... (0/{plan.VersionCount:N0})");
        try
        {
            var progress = new Progress<VersionDeletionProgress>(report =>
                _statusLabel.Text = string.IsNullOrEmpty(report.CloudPath)
                    ? $"結果を確認しています... ({report.CompletedCount:N0}/{report.TotalCount:N0})"
                    : $"移動しています ({report.CompletedCount + 1:N0}/{report.TotalCount:N0}): " +
                      $"{report.CloudPath} {report.VersionLabel}");

            void OnThrottleWait(object? _, TimeSpan wait) => _statusLabel.Text =
                $"SharePointの要求制限により あと {wait.TotalSeconds:N0} 秒待機しています...";
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

            ApplyRefreshedFiles(output.RefreshedFiles);
            var logPath = await WriteDeletionLogAsync(policy, output.Result);
            ReportDeletionResult(output.Result, logPath);
        }
        catch (OperationCanceledException)
        {
            _statusLabel.Text = "ごみ箱への移動を停止しました。";
        }
        catch (Exception exception)
        {
            _statusLabel.Text = "ごみ箱への移動に失敗しました。";
            MessageBox.Show(
                this,
                exception.Message,
                "移動エラー",
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
    /// Records what this scan learned so the next one can skip the files that did not change.
    /// A cache that cannot be written only costs requests later, so it never fails the scan.
    /// </summary>
    private async Task SaveVersionCacheAsync(string accountKey, ScanResult scanned)
    {
        if (scanned.Files.Count == 0)
        {
            return;
        }

        try
        {
            var existing = await _versionCache.LoadAsync(accountKey, CancellationToken.None);
            var merged = new Dictionary<string, CachedFileVersions>(existing, StringComparer.Ordinal);
            foreach (var file in scanned.Files)
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
            _statusLabel.Text += " / 取得結果を保存できませんでした";
        }
    }

    /// <summary>
    /// The per-file phase is the expensive one, and it is what got the tenant throttled before.
    /// The user sees its size and agrees to it before a single request goes out.
    /// </summary>
    private bool ConfirmDetailPhase(int fileCount, long thresholdBytes)
    {
        if (fileCount <= 200)
        {
            return true;
        }

        return MessageBox.Show(
            this,
            $"履歴が {ByteSizeFormatter.Format(thresholdBytes)} 以上のファイルが {fileCount:N0} 件ありました。\n" +
            $"これらのバージョン一覧を1ファイルずつ取得します（およそ {fileCount:N0} 回の要求）。\n\n" +
            "件数が多いと SharePoint の要求制限にかかります。\n" +
            "中止して「処理する最小履歴容量」を大きくすると、件数を絞れます。\n\n" +
            "このまま続けますか。",
            "取得件数の確認",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning) == DialogResult.Yes;
    }

    private void ReportScanErrors(IReadOnlyList<string> folderErrors)
    {
        if (folderErrors.Count == 0)
        {
            return;
        }

        _statusLabel.Text += $" / 読めなかったフォルダー {folderErrors.Count:N0}件";
        MessageBox.Show(
            this,
            $"{folderErrors.Count:N0}個のフォルダーを読み取れませんでした。" +
            "その配下は集計に含まれていません。\n\n" +
            string.Join("\n", folderErrors.Take(10)),
            "一部のフォルダーを取得できませんでした",
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
                $"移動は実行しましたが、ログを書き出せませんでした。\n{exception.Message}",
                "移動ログ",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return null;
        }
    }

    private void ApplyRefreshedFiles(IReadOnlyDictionary<string, SharePointFileProbe> refreshed)
    {
        if (refreshed.Count == 0 || _detectedAccount is null || _libraryRoot is null)
        {
            return;
        }

        _files = _files
            .Select(file => refreshed.TryGetValue(file.ItemId, out var probe)
                ? OneDriveScanner.CreateEntry(_detectedAccount, _libraryRoot, probe)
                : file)
            .ToArray();
        ApplyResults(_aggregate?.Summary.ScanDuration);
    }

    private void ReportDeletionResult(DeletionRunResult result, string? logPath)
    {
        var unknownSuffix = result.UnknownReclaimedSizeCount == 0
            ? ""
            : $"（サイズ不明 {result.UnknownReclaimedSizeCount:N0}件を除く）";

        var message = new StringBuilder()
            .AppendLine($"ごみ箱へ移動: {result.DeletedCount:N0}件 / 既に削除済み: {result.AlreadyDeletedCount:N0}件")
            .AppendLine($"失敗: {result.FailureCount:N0}件")
            .AppendLine(
                $"削減見込み: {ByteSizeFormatter.Format(result.KnownReclaimedBytes)}{unknownSuffix}");

        if (result.NotAttemptedCount > 0)
        {
            message.AppendLine($"未実行: {result.NotAttemptedCount:N0}件");
        }

        if (result.AbortReason is not null)
        {
            message.AppendLine().AppendLine($"中止: {result.AbortReason}");
        }

        message.AppendLine().AppendLine("ローカルの現行ファイル確認:");
        foreach (var verification in result.Verifications)
        {
            var remaining = verification.RemainingPreviousVersionCount.HasValue
                ? $"{verification.RemainingPreviousVersionCount.Value:N0}件"
                : "確認できません";
            message.AppendLine(
                $"　{verification.File.Name}: " +
                $"{VersionDeletionRunner.DescribeLocalChange(verification.LocalFile.Change)} / " +
                $"残り過去版 {remaining}");
            if (verification.UnexpectedlyPresentVersionIds.Count > 0)
            {
                message.AppendLine(
                    "　　移動したはずのバージョンが残っています: " +
                    string.Join(", ", verification.UnexpectedlyPresentVersionIds));
            }

            if (verification.Error is not null)
            {
                message.AppendLine($"　　再確認エラー: {verification.Error}");
            }
        }

        var failures = result.Outcomes.Where(outcome => !outcome.IsSuccess).Take(10).ToArray();
        if (failures.Length > 0)
        {
            message.AppendLine().AppendLine("失敗の内訳:");
            foreach (var failure in failures)
            {
                message.AppendLine(
                    $"　{failure.File.Name} {failure.Version.Label ?? failure.Version.VersionId}: " +
                    $"{failure.Result.Status} {failure.Result.Error}");
            }
        }

        if (logPath is not null)
        {
            message.AppendLine().AppendLine($"ログ: {logPath}");
        }

        var hasProblem = result.FailureCount > 0 ||
            result.AbortReason is not null ||
            result.Verifications.Any(verification => !verification.IsVerified);
        _statusLabel.Text =
            $"ごみ箱へ移動: 成功 {result.DeletedCount:N0}件 / 失敗 {result.FailureCount:N0}件" +
            (result.WasStopped ? "（停止しました）" : "");
        MessageBox.Show(
            this,
            message.ToString(),
            hasProblem ? "確認が必要です" : "移動結果",
            MessageBoxButtons.OK,
            hasProblem ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
    }

    private void ApplyResults(TimeSpan? scanDuration)
    {
        // The run limit belongs to the move, not to the analysis: raising or lowering it must
        // not reshuffle what the table shows. The planner applies it to the actual selection.
        _retention = new RetentionPolicyEvaluator().Evaluate(
            _files,
            CreatePolicy() with { MaximumDeletesPerRun = int.MaxValue },
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
            _statusLabel.Text = "保持条件を再計算しました。";
        }
        catch (Exception exception) when (exception is ArgumentException or OverflowException)
        {
            _statusLabel.Text = $"保持条件エラー: {exception.Message}";
        }
    }

    private void RefreshSummary()
    {
        if (_aggregate is null)
        {
            _summaryLabel.Text = "データはまだありません。";
            return;
        }

        var summary = _aggregate.Summary;
        var historyUnknown = summary.UnknownPreviousVersionSizeCount == 0
            ? ""
            : $" + サイズ不明 {summary.UnknownPreviousVersionSizeCount:N0}件";
        var reclaimUnknown = summary.UnknownDeleteCandidateSizeCount == 0
            ? ""
            : $" + サイズ不明 {summary.UnknownDeleteCandidateSizeCount:N0}件";
        _summaryLabel.Text =
            $"ファイル {summary.TargetFileCount:N0} / 履歴あり {summary.FilesWithPreviousVersions:N0} / " +
            $"過去版 {summary.PreviousVersionCount:N0} / 履歴容量 {ByteSizeFormatter.Format(summary.KnownPreviousVersionBytes)}{historyUnknown} / " +
            $"候補 {summary.DeleteCandidateCount:N0} / 削減見込み {ByteSizeFormatter.Format(summary.KnownReclaimableBytes)}{reclaimUnknown} / " +
            $"エラー {summary.ErrorCount:N0}";
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
                _versionsHeaderLabel.Text = "過去バージョン";
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
            var truncated = rows.Length > displayed.Length
                ? $" / 表示は上位{displayed.Length:N0}件"
                : "";
            _versionsHeaderLabel.Text =
                $"過去バージョン {rows.Length:N0}件 / 合計 {ByteSizeFormatter.Format(totalBytes)}{truncated}" +
                " — ShiftまたはCtrlで複数選択";
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
            source.Decision?.Disposition.ToString() ?? "-",
            source.Decision is null ? "" : string.Join(", ", source.Decision.Reasons),
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
            file.File.RetrievalStatus.ToString(),
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
            ? $"選択をごみ箱へ移動 ({selected:N0}件)"
            : "選択をごみ箱へ移動";
    }

    private async void ExportButtonOnClick(object? sender, EventArgs eventArgs)
    {
        if (_aggregate is null || _isBusy)
        {
            return;
        }

        using var dialog = new FolderBrowserDialog
        {
            Description = "CSV と JSON の保存先を選択してください",
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
                $"既定の出力先が無効です。保存先を手動で選択してください。\n{exception.Message}",
                "出力先の警告",
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
        SetBusy(true, "CSV / JSON を出力しています...");
        try
        {
            var paths = await WriteExportPairAsync(
                dialog.SelectedPath,
                aggregateSnapshot,
                cancellation.Token);
            _statusLabel.Text = $"出力しました: {paths.CsvPath} / {paths.JsonPath}";
        }
        catch (OperationCanceledException)
        {
            _statusLabel.Text = "出力を停止しました。";
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or
            ArgumentException or NotSupportedException or JsonException)
        {
            _statusLabel.Text = "出力に失敗しました。";
            MessageBox.Show(this, exception.Message, "出力エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

    private static string CombineServerRelativePath(
        string libraryRoot,
        IReadOnlyList<string> relativeSegments)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(libraryRoot);
        if (!libraryRoot.StartsWith('/'))
        {
            throw new InvalidDataException("OneDriveルートがサーバー相対パスではありません。");
        }

        var normalizedRoot = libraryRoot.TrimEnd('/');
        return relativeSegments.Count == 0
            ? normalizedRoot
            : normalizedRoot + "/" + string.Join('/', relativeSegments);
    }

    private RetentionPolicy CreatePolicy() => new()
    {
        KeepLatestVersions = decimal.ToInt32(_keepVersionsInput.Value),
        KeepDays = decimal.ToInt32(_keepDaysInput.Value),
        ProtectRecentlyModifiedFileDays = decimal.ToInt32(_protectDaysInput.Value),
        MinimumHistoryBytes = checked(decimal.ToInt64(_minimumHistoryInput.Value) * Mebibyte),
        MinimumCurrentFileBytes = checked(decimal.ToInt64(_minimumFileSizeInput.Value) * Mebibyte),
        MaximumDeletesPerRun = decimal.ToInt32(_maximumCandidatesInput.Value),
    };

    private OneDriveBusinessAccount ValidateScanInputs()
    {
        if (!Directory.Exists(_targetBox.Text))
        {
            throw new ArgumentException("OneDrive同期フォルダー配下のフォルダーを指定してください。");
        }

        var account = _accountDiscovery.FindByLocalPath(_targetBox.Text)
            ?? throw new InvalidOperationException(
                "指定フォルダーに対応するOneDrive for Businessアカウントを検出できませんでした。");
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
            _accountLabel.Text = "OneDrive for Businessアカウントを検出できませんでした。";
            if (showFailure)
            {
                MessageBox.Show(
                    this,
                    "WindowsのOneDrive設定からBusinessアカウントを検出できませんでした。" +
                    "OneDriveが起動して同期済みか確認してください。",
                    "OneDrive自動検出",
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

        _accountLabel.Text =
            $"自動検出: {account.DisplayName} ({account.AccountKey}) / 管理センター設定不要";
    }

    private void BrowseForTargetFolder()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "バージョン履歴を調べるOneDrive配下のフォルダーを選択してください",
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

    private static void TargetBoxOnDragEnter(object? sender, DragEventArgs eventArgs)
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
        _statusLabel.Text = "停止要求を送信しました。進行中の要求の完了を待っています...";
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
        _maximumCandidatesInput.Enabled = !busy;
        _candidatesOnlyCheck.Enabled = !busy;
        _stopButton.Enabled = busy;
        _exportButton.Enabled = !busy && _aggregate is not null;
        _progressBar.Visible = busy;
        _statusLabel.Text = status;
        RefreshDeleteState();
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
                    ? ByteSizeFormatter.Format(row.Version.Size)
                    : "不明";
                eventArgs.FormattingApplied = true;
                break;
            case "FileHistorySize" when eventArgs.Value is long historyBytes:
                eventArgs.Value = ByteSizeFormatter.Format(historyBytes);
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
                eventArgs.Value = ByteSizeFormatter.Format(file.CurrentSizeBytes);
                eventArgs.FormattingApplied = true;
                break;
            case "HistorySize":
                eventArgs.Value = ByteSizeFormatter.Format(file.KnownPreviousVersionBytes) +
                                  (file.UnknownPreviousVersionSizeCount == 0
                                      ? ""
                                      : $" +?({file.UnknownPreviousVersionSizeCount})");
                eventArgs.FormattingApplied = true;
                break;
            case "Reclaimable":
                eventArgs.Value = ByteSizeFormatter.Format(file.KnownReclaimableBytes) +
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

    private static void ConfigureTextBox(TextBox textBox)
    {
        textBox.Dock = DockStyle.Fill;
        textBox.Margin = new Padding(4);
    }

    private static void AddLabeledRow(
        TableLayoutPanel layout,
        int row,
        string label,
        Control input,
        Control? trailing = null)
    {
        var labelControl = new Label
        {
            AutoSize = true,
            Text = label,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(3, 8, 8, 3),
        };
        layout.Controls.Add(labelControl, 0, row);
        layout.Controls.Add(input, 1, row);
        if (trailing is null)
        {
            layout.SetColumnSpan(input, 2);
        }
        else
        {
            layout.Controls.Add(trailing, 2, row);
        }
    }

    private static void ConfigureNumber(NumericUpDown input, decimal minimum, decimal maximum)
    {
        input.Minimum = minimum;
        input.Maximum = maximum;
        input.Width = 85;
        input.TextAlign = HorizontalAlignment.Right;
        input.ThousandsSeparator = true;
    }

    private void AddPolicyControl(
        FlowLayoutPanel panel,
        string label,
        NumericUpDown input,
        string suffix,
        string description)
    {
        var wrapper = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0, 0, 18, 0),
        };
        var labelControl = new Label
        {
            AutoSize = true,
            Text = label,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(3, 7, 5, 3),
        };
        var suffixControl = new Label
        {
            AutoSize = true,
            Text = suffix,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(3, 7, 3, 3),
        };

        wrapper.Controls.Add(labelControl);
        wrapper.Controls.Add(input);
        wrapper.Controls.Add(suffixControl);
        panel.Controls.Add(wrapper);

        foreach (var control in new Control[] { labelControl, input, suffixControl })
        {
            _toolTip.SetToolTip(control, description);
        }
    }

    private static Control WrapGrid(Label header, DataGridView grid)
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        header.AutoSize = true;
        header.Dock = DockStyle.Top;
        header.Padding = new Padding(2, 2, 2, 4);
        layout.Controls.Add(header, 0, 0);
        layout.Controls.Add(grid, 0, 1);
        return layout;
    }

    private static void ConfigureGrid(DataGridView grid)
    {
        grid.Dock = DockStyle.Fill;
        grid.ReadOnly = true;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.AllowUserToResizeRows = false;
        grid.AllowUserToResizeColumns = true;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
        grid.AutoGenerateColumns = false;
        grid.MultiSelect = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.RowHeadersVisible = false;
        grid.BackgroundColor = SystemColors.Window;
        grid.BorderStyle = BorderStyle.Fixed3D;
        grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
    }

    /// <summary>
    /// Every column keeps <see cref="DataGridViewAutoSizeColumnMode.None"/> so the user can
    /// drag any of them. A Fill column recomputes its width to take the remaining space, which
    /// makes it snap straight back and look like it cannot be resized at all.
    /// </summary>
    private static DataGridViewTextBoxColumn CreateTextColumn(
        string name,
        string header,
        int width,
        Type? valueType = null) => new()
    {
        Name = name,
        HeaderText = header,
        Width = width,
        AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
        Resizable = DataGridViewTriState.True,
        ValueType = valueType ?? typeof(string),
        SortMode = DataGridViewColumnSortMode.Automatic,
    };

    private static void SetNumber(NumericUpDown input, long value)
    {
        input.Value = Math.Clamp((decimal)value, input.Minimum, input.Maximum);
    }
}
