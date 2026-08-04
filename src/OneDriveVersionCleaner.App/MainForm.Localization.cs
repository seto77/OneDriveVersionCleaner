using OneDriveVersionCleaner.App.Localization;
using OneDriveVersionCleaner.Core.Aggregation;
using static OneDriveVersionCleaner.App.Localization.Localization;

namespace OneDriveVersionCleaner.App;

/// <summary>
/// The interface language of the main window: the translations that go over the English the
/// design surface carries, the font that goes with the language, and the switch itself.
/// </summary>
internal sealed partial class MainForm
{
    /// <summary>
    /// One font per supported language, kept for the life of the process. The set is bounded by
    /// <see cref="SupportedCultures.All"/>, and the control tree keeps referencing whichever one
    /// it was last given, so these are deliberately never disposed.
    /// </summary>
    private static readonly Dictionary<string, Font> BaseFonts = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, Font> HeadingFonts = new(StringComparer.Ordinal);

    /// <summary>Gives the window the font the current language reads best in.</summary>
    private void ApplyUiFont()
    {
        var family = SupportedCultures.Current.FontFamily;
        if (!BaseFonts.TryGetValue(family, out var baseFont))
        {
            baseFont = new Font(family, 10F);
            BaseFonts[family] = baseFont;
            HeadingFonts[family] = new Font(baseFont, FontStyle.Bold);
        }

        Font = baseFont;

        // The two group headings are the ambient font in bold. Deriving them here rather than on
        // the design surface keeps one machine's font family out of the layout.
        _exclusionsHeadingLabel.Font = HeadingFonts[family];
        _targetsHeadingLabel.Font = HeadingFonts[family];
    }

    /// <summary>
    /// Applies the current language to everything the design surface placed. Text that is built
    /// from data — the summary, the grids, the status line — is produced by the methods that own
    /// it and is refreshed separately by <see cref="ChangeLanguage"/>.
    /// </summary>
    private void ApplyLocalizedText()
    {
        // The version is on the title bar because it is the one place a user can read it off a
        // build they downloaded, and a bug report that cannot name a build is guesswork.
        Text = Loc(
            en: "OneDrive Version History Cleaner",
            ja: "OneDrive バージョン履歴クリーナー") + $" {AppVersion.Display}";

        _languageMenu.Text = Loc(en: "&Language", ja: "言語(&L)");
        _englishMenuItem.Checked = SupportedCultures.Current.Name == "en";
        _japaneseMenuItem.Checked = SupportedCultures.Current.Name == "ja";

        _connectionGroup.Text = Loc(
            en: "Connection and target (no admin center, your own OneDrive only)",
            ja: "接続と対象（管理センター不要／自分の OneDrive のみ）");
        _rootLabel.Text = Loc(en: "Sync root", ja: "同期ルート");
        _browseRootButton.Text = Loc(en: "Rescan", ja: "再検出");
        _targetLabel.Text = Loc(en: "Target folder", ja: "対象フォルダー");
        _browseTargetButton.Text = Loc(en: "Choose folder...", ja: "フォルダー選択...");
        _probeButton.Text = Loc(en: "Connect", ja: "接続");
        _scanButton.Text = Loc(en: "Read history", ja: "履歴を取得");
        _stopButton.Text = Loc(en: "Stop", ja: "停止");
        _exportButton.Text = Loc(en: "Export CSV / JSON", ja: "CSV / JSON 出力");

        _policyGroup.Text = Loc(
            en: "Rules (recalculated as you change them)",
            ja: "条件（変更すると即時再計算）");
        _exclusionsHeadingLabel.Text = Loc(en: "Exclusions", ja: "除外条件");
        _targetsHeadingLabel.Text = Loc(en: "Targets", ja: "対象条件");
        _keepVersionsLabel.Text = Loc(en: "Newest versions", ja: "最近の履歴");
        _keepVersionsSuffix.Text = Loc(en: "kept", ja: "件");
        _keepDaysLabel.Text = Loc(en: "Back from last modified", ja: "最終更新日から遡って");
        _keepDaysSuffix.Text = Loc(en: "days", ja: "日以内");
        _protectDaysLabel.Text = Loc(en: "Modified within the last", ja: "更新日が現在から");
        _protectDaysSuffix.Text = Loc(en: "days", ja: "日以内");
        _minimumFileSizeLabel.Text = Loc(en: "File size", ja: "ファイルサイズが");
        _minimumFileSizeSuffix.Text = Loc(en: "MB or more", ja: "MB以上");
        _minimumHistoryLabel.Text = Loc(en: "History total", ja: "総履歴サイズが");
        _minimumHistorySuffix.Text = Loc(en: "MB or more", ja: "MB以上");
        _cacheAgeLabel.Text = Loc(en: "Re-query after", ja: "問合わせ間隔");
        _cacheAgeSuffix.Text = Loc(en: "days or more", ja: "日以上");
        _queryLimitLabel.Text = Loc(en: "Query limit", ja: "問合わせ上限");
        _queryLimitSuffix.Text = Loc(en: "files per run", ja: "件まで");

        _candidatesOnlyCheck.Text = Loc(en: "Candidates only", ja: "削除候補のみ表示");
        _filesHeaderLabel.Text = Loc(en: "History size by file", ja: "ファイル別の履歴容量");
        _openFolderMenuItem.Text = Loc(en: "Open folder", ja: "フォルダーを開く");
        _openOnWebMenuItem.Text = Loc(en: "Open on the web", ja: "Webで開く");

        ApplyLocalizedToolTips();
        ApplyLocalizedColumnHeaders();
    }

    /// <summary>
    /// Re-renders the text that is built from data rather than placed by the designer. Run once
    /// at start-up, so the empty grids and the summary are not left reading the English the
    /// design surface carries, and again whenever the language changes.
    /// </summary>
    /// <remarks>
    /// The status line is the one thing that cannot be re-derived: it describes an operation
    /// that has already finished. It goes back to the idle wording rather than being left in the
    /// language it was written in.
    /// </remarks>
    private void RefreshDerivedText()
    {
        _statusLabel.Text = Loc(en: "Ready", ja: "準備完了");
        RefreshSummary();
        UpdateVersionsHeader();
        RelabelGridRows();
        RefreshDeleteState();
    }

    /// <summary>Puts the versions heading into words from the numbers behind it.</summary>
    private void UpdateVersionsHeader()
    {
        if (_versionsHeader is not { } header)
        {
            _versionsHeaderLabel.Text = Loc(en: "Past versions", ja: "過去バージョン");
            return;
        }

        var truncated = header.DisplayedCount is { } displayed
            ? Loc(en: $" / showing the largest {displayed:N0}",
                  ja: $" / 表示は上位{displayed:N0}件")
            : "";
        _versionsHeaderLabel.Text = Loc(
                en: $"Past versions: {header.RowCount:N0} / " +
                    $"{SizeText.Of(header.TotalBytes)} in total",
                ja: $"過去バージョン {header.RowCount:N0}件 / " +
                    $"合計 {SizeText.Of(header.TotalBytes)}") +
            truncated +
            Loc(en: " — Shift or Ctrl selects several",
                ja: " — ShiftまたはCtrlで複数選択");
    }

    /// <summary>
    /// Rewrites only the cells whose text is a translated enumeration, leaving the rows where
    /// they are.
    /// </summary>
    /// <remarks>
    /// Rebuilding the grids instead would be simpler, but this application acts on what the user
    /// has selected: dropping a selection of past versions because the language changed is not
    /// something a destructive tool should do. Sort order and scroll position survive too.
    /// </remarks>
    private void RelabelGridRows()
    {
        foreach (DataGridViewRow row in _versionsGrid.Rows)
        {
            if (row.Tag is not VersionRow source)
            {
                continue;
            }

            row.Cells[_versionsDecisionColumn.Index].Value = source.Decision is null
                ? "-"
                : DisplayNames.Of(source.Decision.Disposition);
            row.Cells[_versionsReasonsColumn.Index].Value = source.Decision is null
                ? ""
                : string.Join(", ", source.Decision.Reasons.Select(DisplayNames.Of));
        }

        foreach (DataGridViewRow row in _filesGrid.Rows)
        {
            if (row.Tag is FileScanAggregate file)
            {
                row.Cells[_filesStatusColumn.Index].Value =
                    DisplayNames.Of(file.File.RetrievalStatus);
            }
        }

        // The size and date columns are rendered by CellFormatting, which reads the current
        // language every time the grid paints.
        _versionsGrid.Invalidate();
        _filesGrid.Invalidate();
    }

    /// <summary>
    /// The wording here has been adjusted in response to what users misread; translating it is
    /// fine, rewriting what it claims is not.
    /// </summary>
    private void ApplyLocalizedToolTips()
    {
        SetPolicyToolTip(
            Loc(en: "Keeps this many past versions of each file, counting from the newest. " +
                    "0 turns the rule off.",
                ja: "各ファイルについて、新しいものから数えてこの件数の過去バージョンを残します。0で無効。"),
            _keepVersionsLabel, _keepVersionsInput, _keepVersionsSuffix);

        SetPolicyToolTip(
            Loc(en: "Keeps past versions created within this many days back from the file's own " +
                    "last modification.\nThe reference point is the file's timestamp, not the " +
                    "current time. 0 turns the rule off.",
                ja: "そのファイルの最終更新日時から遡ってこの日数以内に作られた過去バージョンを残します。\n" +
                    "現在時刻ではなくファイル自身の更新日時が基準です。0で無効。"),
            _keepDaysLabel, _keepDaysInput, _keepDaysSuffix);

        SetPolicyToolTip(
            Loc(en: "Leaves out entirely any file modified within this many days of now. " +
                    "0 turns the rule off.",
                ja: "現在時刻からこの日数以内に更新されたファイルは、丸ごと処理対象から外します。0で無効。"),
            _protectDaysLabel, _protectDaysInput, _protectDaysSuffix);

        SetPolicyToolTip(
            Loc(en: "Only files whose current size is at least this are processed.\n" +
                    "A small file cannot free much however many versions it has, so it is never " +
                    "asked about.\nThe size is read from the local sync folder, so this filter " +
                    "costs no requests. 0 turns the rule off.",
                ja: "現行サイズがこの値以上のファイルだけを処理します。\n" +
                    "小さいファイルは何世代あっても削減量が知れているため、サーバーに問い合わせません。\n" +
                    "サイズはローカルの同期フォルダーから読むので、この絞り込みに通信は発生しません。0で無効。"),
            _minimumFileSizeLabel, _minimumFileSizeInput, _minimumFileSizeSuffix);

        SetPolicyToolTip(
            Loc(en: "Only files whose past versions add up to at least this are processed.\n" +
                    "Anything below that is left out entirely. 0 turns the rule off.",
                ja: "過去バージョンの合計がこの値以上のファイルだけを処理します。\n" +
                    "これ未満のファイルは丸ごと対象外です。0で無効。"),
            _minimumHistoryLabel, _minimumHistoryInput, _minimumHistorySuffix);

        SetPolicyToolTip(
            Loc(en: "Version history that has been read once is stored and reused.\n" +
                    "A file is asked about again only once this many days have passed since that " +
                    "read\nand the file itself has changed.\nHistory only grows when a file is " +
                    "edited, so an unchanged file is never asked about. 0 asks every time.",
                ja: "一度取得したバージョン履歴は保存され、次回以降は再利用します。\n" +
                    "同じファイルを問い合わせ直すのは、前回の取得からこの日数が経ち、かつ\n" +
                    "ファイルが更新されている場合だけです。\n" +
                    "履歴は編集したときにしか増えないため、更新のないファイルは問い合わせません。0で毎回。"),
            _cacheAgeLabel, _cacheAgeInput, _cacheAgeSuffix);

        SetPolicyToolTip(
            Loc(en: "The most files one run asks the service about.\n" +
                    "The largest files are asked about first; the rest wait for the next run, " +
                    "which picks them up\nbecause nothing was stored for them.\n" +
                    "Several thousand in one run runs into SharePoint's request limits: about " +
                    "2,000 goes through, 10,000 does not.\n0 lifts the limit.",
                ja: "1回の実行でサーバーに問い合わせるファイル数の上限です。\n" +
                    "サイズの大きいファイルから順に問い合わせ、残りは次回以降の実行に回します。\n" +
                    "見送ったファイルは何も保存されないため、次回の実行が改めて選び直します。\n" +
                    "1回に数千件を問い合わせると SharePoint の要求制限にかかります。" +
                    "2,000件程度までは通りますが、10,000件では失敗します。0で無制限。"),
            _queryLimitLabel, _queryLimitInput, _queryLimitSuffix);
    }

    /// <summary>
    /// The label, the box and the unit read as one control, so the explanation follows the
    /// pointer across all three.
    /// </summary>
    private void SetPolicyToolTip(string text, params Control[] controls)
    {
        foreach (var control in controls)
        {
            _toolTip.SetToolTip(control, text);
        }
    }

    /// <summary>
    /// Assigned through the designer's fields rather than through the column names, because
    /// <see cref="InitializeGridSchema"/> renames the columns and both grids then own a
    /// "FileName" and a "CloudPath".
    /// </summary>
    private void ApplyLocalizedColumnHeaders()
    {
        _versionsFileNameColumn.HeaderText = Loc(en: "File", ja: "ファイル名");
        _versionsCloudPathColumn.HeaderText = Loc(en: "Path", ja: "パス");
        _versionsVersionLabelColumn.HeaderText = Loc(en: "Version", ja: "バージョン");
        _versionsCreatedColumn.HeaderText = Loc(en: "Created", ja: "作成日時");
        _versionsModifiedByColumn.HeaderText = Loc(en: "Modified by", ja: "更新者");
        _versionsSizeColumn.HeaderText = Loc(en: "Size", ja: "サイズ");
        _versionsFileHistorySizeColumn.HeaderText = Loc(en: "File history total", ja: "ファイル履歴計");
        _versionsDecisionColumn.HeaderText = Loc(en: "Verdict", ja: "判定");
        _versionsReasonsColumn.HeaderText = Loc(en: "Reasons", ja: "理由");

        _filesFileNameColumn.HeaderText = Loc(en: "File", ja: "ファイル名");
        _filesCloudPathColumn.HeaderText = Loc(en: "Path", ja: "パス");
        _filesCurrentSizeColumn.HeaderText = Loc(en: "Current size", ja: "現行サイズ");
        _filesHistoryCountColumn.HeaderText = Loc(en: "Versions", ja: "履歴数");
        _filesHistorySizeColumn.HeaderText = Loc(en: "History size", ja: "履歴容量");
        _filesCandidateCountColumn.HeaderText = Loc(en: "Candidates", ja: "候補数");
        _filesReclaimableColumn.HeaderText = Loc(en: "Reclaimable", ja: "削減見込み");
        _filesLastModifiedColumn.HeaderText = Loc(en: "Last modified", ja: "最終更新");
        _filesStatusColumn.HeaderText = Loc(en: "Status", ja: "状態");
    }

    private void EnglishMenuItemOnClick(object? sender, EventArgs eventArgs) =>
        ChangeLanguage("en");

    private void JapaneseMenuItemOnClick(object? sender, EventArgs eventArgs) =>
        ChangeLanguage("ja");

    /// <summary>
    /// Switches language without restarting. Everything on screen is either re-translated or
    /// rebuilt from the data behind it; the status line is the one exception, because a sentence
    /// describing an operation that has already finished cannot be re-derived, so it goes back to
    /// the idle wording rather than staying in the previous language.
    /// </summary>
    private void ChangeLanguage(string cultureName)
    {
        var culture = SupportedCultures.Resolve(cultureName);

        // SetBusy already greys the menu out; this is the second lock on the same door, because
        // what is behind it is a running deletion.
        if (_isBusy || culture.Name == SupportedCultures.Current.Name)
        {
            return;
        }

        UiLanguage.Apply(culture);
        UiLanguage.Save(culture);

        SuspendLayout();
        try
        {
            ApplyUiFont();
            ApplyLocalizedText();
            RefreshAccountLabel();
            RefreshDerivedText();
        }
        finally
        {
            ResumeLayout(performLayout: true);
        }

        // The sign-in window outlives its own dialog, so one that is already open has to be
        // re-translated in place rather than at its next construction.
        _sharePointSession?.ApplyLocalizedText();
    }

    /// <summary>
    /// Rebuilds the account line from what is currently known, so a language change does not
    /// have to re-read the registry and does not lose the fact that the session is confirmed.
    /// </summary>
    private void RefreshAccountLabel()
    {
        if (_detectedAccount is null)
        {
            _accountLabel.Text = Loc(
                en: "No OneDrive for Business account was found.",
                ja: "OneDrive for Businessアカウントを検出できませんでした。");
            return;
        }

        var account = $"{_detectedAccount.DisplayName} ({_detectedAccount.AccountKey})";
        _accountLabel.Text = _sharePointSession?.IsVerified == true
            ? Loc(en: $"Found: {account} / signed in as yourself",
                  ja: $"検出済み: {account} / 本人セッション確認済み")
            : Loc(en: $"Detected: {account} / no admin center setup needed",
                  ja: $"自動検出: {account} / 管理センター設定不要");
    }
}
