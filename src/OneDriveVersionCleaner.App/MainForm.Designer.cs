// The designer regenerates this file; nullable annotations it cannot emit would be lost.
#nullable disable

namespace OneDriveVersionCleaner.App
{
    partial class MainForm
    {
        /// <summary>Required designer variable.</summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>Clean up any resources being used.</summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null)
            {
                components.Dispose();
            }

            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            _rootLayout = new TableLayoutPanel();
            _connectionGroup = new GroupBox();
            _connectionLayout = new TableLayoutPanel();
            _accountLabel = new Label();
            _rootLabel = new Label();
            _rootBox = new TextBox();
            _browseRootButton = new Button();
            _targetLabel = new Label();
            _targetBox = new TextBox();
            _browseTargetButton = new Button();
            _actionsPanel = new FlowLayoutPanel();
            _probeButton = new Button();
            _scanButton = new Button();
            _stopButton = new Button();
            _exportButton = new Button();
            _deleteButton = new Button();
            _policyGroup = new GroupBox();
            _policyFlow = new FlowLayoutPanel();
            _keepVersionsPanel = new FlowLayoutPanel();
            _keepVersionsLabel = new Label();
            _keepVersionsInput = new NumericUpDown();
            _keepVersionsSuffix = new Label();
            _keepDaysPanel = new FlowLayoutPanel();
            _keepDaysLabel = new Label();
            _keepDaysInput = new NumericUpDown();
            _keepDaysSuffix = new Label();
            _protectDaysPanel = new FlowLayoutPanel();
            _protectDaysLabel = new Label();
            _protectDaysInput = new NumericUpDown();
            _protectDaysSuffix = new Label();
            _minimumFileSizePanel = new FlowLayoutPanel();
            _minimumFileSizeLabel = new Label();
            _minimumFileSizeInput = new NumericUpDown();
            _minimumFileSizeSuffix = new Label();
            _minimumHistoryPanel = new FlowLayoutPanel();
            _minimumHistoryLabel = new Label();
            _minimumHistoryInput = new NumericUpDown();
            _minimumHistorySuffix = new Label();
            _cacheAgePanel = new FlowLayoutPanel();
            _cacheAgeLabel = new Label();
            _cacheAgeInput = new NumericUpDown();
            _cacheAgeSuffix = new Label();
            _maximumCandidatesPanel = new FlowLayoutPanel();
            _maximumCandidatesLabel = new Label();
            _maximumCandidatesInput = new NumericUpDown();
            _maximumCandidatesSuffix = new Label();
            _candidatesOnlyCheck = new CheckBox();
            _summaryPanel = new TableLayoutPanel();
            _summaryLabel = new Label();
            _splitContainer = new SplitContainer();
            _versionsLayout = new TableLayoutPanel();
            _versionsHeaderLabel = new Label();
            _versionsGrid = new DataGridView();
            _versionsFileNameColumn = new DataGridViewTextBoxColumn();
            _versionsCloudPathColumn = new DataGridViewTextBoxColumn();
            _versionsVersionLabelColumn = new DataGridViewTextBoxColumn();
            _versionsCreatedColumn = new DataGridViewTextBoxColumn();
            _versionsModifiedByColumn = new DataGridViewTextBoxColumn();
            _versionsSizeColumn = new DataGridViewTextBoxColumn();
            _versionsFileHistorySizeColumn = new DataGridViewTextBoxColumn();
            _versionsDecisionColumn = new DataGridViewTextBoxColumn();
            _versionsReasonsColumn = new DataGridViewTextBoxColumn();
            _filesLayout = new TableLayoutPanel();
            _filesHeaderLabel = new Label();
            _filesGrid = new DataGridView();
            _filesFileNameColumn = new DataGridViewTextBoxColumn();
            _filesCloudPathColumn = new DataGridViewTextBoxColumn();
            _filesCurrentSizeColumn = new DataGridViewTextBoxColumn();
            _filesHistoryCountColumn = new DataGridViewTextBoxColumn();
            _filesHistorySizeColumn = new DataGridViewTextBoxColumn();
            _filesCandidateCountColumn = new DataGridViewTextBoxColumn();
            _filesReclaimableColumn = new DataGridViewTextBoxColumn();
            _filesLastModifiedColumn = new DataGridViewTextBoxColumn();
            _filesStatusColumn = new DataGridViewTextBoxColumn();
            _statusPanel = new TableLayoutPanel();
            _statusLabel = new Label();
            _progressBar = new ProgressBar();
            _fileContextMenu = new ContextMenuStrip(components);
            _openFolderMenuItem = new ToolStripMenuItem();
            _openOnWebMenuItem = new ToolStripMenuItem();
            _toolTip = new ToolTip(components);
            _rootLayout.SuspendLayout();
            _connectionGroup.SuspendLayout();
            _connectionLayout.SuspendLayout();
            _actionsPanel.SuspendLayout();
            _policyGroup.SuspendLayout();
            _policyFlow.SuspendLayout();
            _keepVersionsPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_keepVersionsInput).BeginInit();
            _keepDaysPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_keepDaysInput).BeginInit();
            _protectDaysPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_protectDaysInput).BeginInit();
            _minimumFileSizePanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_minimumFileSizeInput).BeginInit();
            _minimumHistoryPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_minimumHistoryInput).BeginInit();
            _cacheAgePanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_cacheAgeInput).BeginInit();
            _maximumCandidatesPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_maximumCandidatesInput).BeginInit();
            _summaryPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_splitContainer).BeginInit();
            _splitContainer.Panel1.SuspendLayout();
            _splitContainer.Panel2.SuspendLayout();
            _splitContainer.SuspendLayout();
            _versionsLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_versionsGrid).BeginInit();
            _filesLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_filesGrid).BeginInit();
            _statusPanel.SuspendLayout();
            _fileContextMenu.SuspendLayout();
            SuspendLayout();
            //
            // _rootLayout
            //
            _rootLayout.ColumnCount = 1;
            _rootLayout.Dock = DockStyle.Fill;
            _rootLayout.Name = "_rootLayout";
            _rootLayout.Padding = new Padding(10);
            _rootLayout.RowCount = 5;
            _rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            _rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _rootLayout.Controls.Add(_connectionGroup, 0, 0);
            _rootLayout.Controls.Add(_policyGroup, 0, 1);
            _rootLayout.Controls.Add(_summaryPanel, 0, 2);
            _rootLayout.Controls.Add(_splitContainer, 0, 3);
            _rootLayout.Controls.Add(_statusPanel, 0, 4);
            //
            // _connectionGroup
            //
            _connectionGroup.AutoSize = true;
            _connectionGroup.Dock = DockStyle.Top;
            _connectionGroup.Name = "_connectionGroup";
            _connectionGroup.Padding = new Padding(8);
            _connectionGroup.Text = "接続と対象（管理センター不要／自分の OneDrive のみ）";
            _connectionGroup.Controls.Add(_connectionLayout);
            //
            // _connectionLayout
            //
            _connectionLayout.AutoSize = true;
            _connectionLayout.ColumnCount = 3;
            _connectionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            _connectionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            _connectionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            _connectionLayout.Dock = DockStyle.Top;
            _connectionLayout.Name = "_connectionLayout";
            _connectionLayout.RowCount = 4;
            _connectionLayout.Controls.Add(_accountLabel, 0, 0);
            _connectionLayout.SetColumnSpan(_accountLabel, 3);
            _connectionLayout.Controls.Add(_rootLabel, 0, 1);
            _connectionLayout.Controls.Add(_rootBox, 1, 1);
            _connectionLayout.Controls.Add(_browseRootButton, 2, 1);
            _connectionLayout.Controls.Add(_targetLabel, 0, 2);
            _connectionLayout.Controls.Add(_targetBox, 1, 2);
            _connectionLayout.Controls.Add(_browseTargetButton, 2, 2);
            _connectionLayout.Controls.Add(_actionsPanel, 1, 3);
            _connectionLayout.SetColumnSpan(_actionsPanel, 2);
            //
            // _accountLabel
            //
            _accountLabel.AutoSize = true;
            _accountLabel.Name = "_accountLabel";
            _accountLabel.Padding = new Padding(0, 2, 0, 6);
            _accountLabel.Text = "OneDriveアカウントを自動検出しています...";
            //
            // _rootLabel
            //
            _rootLabel.Anchor = AnchorStyles.Left;
            _rootLabel.AutoSize = true;
            _rootLabel.Margin = new Padding(3, 8, 8, 3);
            _rootLabel.Name = "_rootLabel";
            _rootLabel.Text = "同期ルート";
            //
            // _rootBox
            //
            _rootBox.Dock = DockStyle.Fill;
            _rootBox.Margin = new Padding(4);
            _rootBox.Name = "_rootBox";
            _rootBox.ReadOnly = true;
            //
            // _browseRootButton
            //
            _browseRootButton.AutoSize = true;
            _browseRootButton.Name = "_browseRootButton";
            _browseRootButton.Text = "再検出";
            _browseRootButton.Click += BrowseRootButtonOnClick;
            //
            // _targetLabel
            //
            _targetLabel.Anchor = AnchorStyles.Left;
            _targetLabel.AutoSize = true;
            _targetLabel.Margin = new Padding(3, 8, 8, 3);
            _targetLabel.Name = "_targetLabel";
            _targetLabel.Text = "対象フォルダー";
            //
            // _targetBox
            //
            _targetBox.AllowDrop = true;
            _targetBox.Dock = DockStyle.Fill;
            _targetBox.Margin = new Padding(4);
            _targetBox.Name = "_targetBox";
            _targetBox.DragDrop += TargetBoxOnDragDrop;
            _targetBox.DragEnter += TargetBoxOnDragEnter;
            //
            // _browseTargetButton
            //
            _browseTargetButton.AutoSize = true;
            _browseTargetButton.Name = "_browseTargetButton";
            _browseTargetButton.Text = "フォルダー選択...";
            _browseTargetButton.Click += BrowseTargetButtonOnClick;
            //
            // _actionsPanel
            //
            _actionsPanel.AutoSize = true;
            _actionsPanel.Dock = DockStyle.Fill;
            _actionsPanel.FlowDirection = FlowDirection.LeftToRight;
            _actionsPanel.Name = "_actionsPanel";
            _actionsPanel.Padding = new Padding(0, 6, 0, 0);
            _actionsPanel.WrapContents = false;
            _actionsPanel.Controls.Add(_probeButton);
            _actionsPanel.Controls.Add(_scanButton);
            _actionsPanel.Controls.Add(_stopButton);
            _actionsPanel.Controls.Add(_exportButton);
            _actionsPanel.Controls.Add(_deleteButton);
            //
            // _probeButton
            //
            _probeButton.AutoSize = true;
            _probeButton.Name = "_probeButton";
            _probeButton.Text = "接続テスト (1要求)";
            _probeButton.Click += ProbeButtonOnClick;
            //
            // _scanButton
            //
            _scanButton.AutoSize = true;
            _scanButton.Name = "_scanButton";
            _scanButton.Text = "Webでサインインして全履歴を取得";
            _scanButton.Click += ScanButtonOnClick;
            //
            // _stopButton
            //
            _stopButton.AutoSize = true;
            _stopButton.Enabled = false;
            _stopButton.Name = "_stopButton";
            _stopButton.Text = "停止";
            _stopButton.Click += StopButtonOnClick;
            //
            // _exportButton
            //
            _exportButton.AutoSize = true;
            _exportButton.Enabled = false;
            _exportButton.Name = "_exportButton";
            _exportButton.Text = "CSV / JSON 出力";
            _exportButton.Click += ExportButtonOnClick;
            //
            // _deleteButton
            //
            _deleteButton.AutoSize = true;
            _deleteButton.Enabled = false;
            _deleteButton.ForeColor = Color.Firebrick;
            _deleteButton.Name = "_deleteButton";
            _deleteButton.Text = "選択をごみ箱へ移動";
            _deleteButton.Click += DeleteButtonOnClick;
            //
            // _policyGroup
            //
            _policyGroup.AutoSize = true;
            _policyGroup.Dock = DockStyle.Top;
            _policyGroup.Name = "_policyGroup";
            _policyGroup.Padding = new Padding(8);
            _policyGroup.Text = "保持条件（変更すると即時再計算）";
            _policyGroup.Controls.Add(_policyFlow);
            //
            // _policyFlow
            //
            _policyFlow.AutoSize = true;
            _policyFlow.Dock = DockStyle.Top;
            _policyFlow.FlowDirection = FlowDirection.LeftToRight;
            _policyFlow.Name = "_policyFlow";
            _policyFlow.WrapContents = true;
            _policyFlow.Controls.Add(_keepVersionsPanel);
            _policyFlow.Controls.Add(_keepDaysPanel);
            _policyFlow.Controls.Add(_protectDaysPanel);
            _policyFlow.Controls.Add(_minimumFileSizePanel);
            _policyFlow.Controls.Add(_minimumHistoryPanel);
            _policyFlow.Controls.Add(_cacheAgePanel);
            _policyFlow.Controls.Add(_maximumCandidatesPanel);
            _policyFlow.Controls.Add(_candidatesOnlyCheck);
            //
            // _keepVersionsPanel
            //
            _keepVersionsPanel.AutoSize = true;
            _keepVersionsPanel.FlowDirection = FlowDirection.LeftToRight;
            _keepVersionsPanel.Margin = new Padding(0, 0, 18, 0);
            _keepVersionsPanel.Name = "_keepVersionsPanel";
            _keepVersionsPanel.WrapContents = false;
            _keepVersionsPanel.Controls.Add(_keepVersionsLabel);
            _keepVersionsPanel.Controls.Add(_keepVersionsInput);
            _keepVersionsPanel.Controls.Add(_keepVersionsSuffix);
            //
            // _keepVersionsLabel
            //
            _keepVersionsLabel.Anchor = AnchorStyles.Left;
            _keepVersionsLabel.AutoSize = true;
            _keepVersionsLabel.Margin = new Padding(3, 7, 5, 3);
            _keepVersionsLabel.Name = "_keepVersionsLabel";
            _keepVersionsLabel.Text = "保持する履歴数";
            _toolTip.SetToolTip(_keepVersionsLabel, "各ファイルについて、新しいものから数えてこの件数の過去バージョンを残します。0で無効。");
            //
            // _keepVersionsInput
            //
            _keepVersionsInput.Maximum = new decimal(new int[] { 100000, 0, 0, 0 });
            _keepVersionsInput.Name = "_keepVersionsInput";
            _keepVersionsInput.TextAlign = HorizontalAlignment.Right;
            _keepVersionsInput.ThousandsSeparator = true;
            _keepVersionsInput.Width = 85;
            _toolTip.SetToolTip(_keepVersionsInput, "各ファイルについて、新しいものから数えてこの件数の過去バージョンを残します。0で無効。");
            //
            // _keepVersionsSuffix
            //
            _keepVersionsSuffix.Anchor = AnchorStyles.Left;
            _keepVersionsSuffix.AutoSize = true;
            _keepVersionsSuffix.Margin = new Padding(3, 7, 3, 3);
            _keepVersionsSuffix.Name = "_keepVersionsSuffix";
            _keepVersionsSuffix.Text = "件";
            _toolTip.SetToolTip(_keepVersionsSuffix, "各ファイルについて、新しいものから数えてこの件数の過去バージョンを残します。0で無効。");
            //
            // _keepDaysPanel
            //
            _keepDaysPanel.AutoSize = true;
            _keepDaysPanel.FlowDirection = FlowDirection.LeftToRight;
            _keepDaysPanel.Margin = new Padding(0, 0, 18, 0);
            _keepDaysPanel.Name = "_keepDaysPanel";
            _keepDaysPanel.WrapContents = false;
            _keepDaysPanel.Controls.Add(_keepDaysLabel);
            _keepDaysPanel.Controls.Add(_keepDaysInput);
            _keepDaysPanel.Controls.Add(_keepDaysSuffix);
            //
            // _keepDaysLabel
            //
            _keepDaysLabel.Anchor = AnchorStyles.Left;
            _keepDaysLabel.AutoSize = true;
            _keepDaysLabel.Margin = new Padding(3, 7, 5, 3);
            _keepDaysLabel.Name = "_keepDaysLabel";
            _keepDaysLabel.Text = "最終更新から保持";
            _toolTip.SetToolTip(_keepDaysLabel, "そのファイルの最終更新日時から遡ってこの日数以内に作られた過去バージョンを残します。\n現在時刻ではなくファイル自身の更新日時が基準です。0で無効。");
            //
            // _keepDaysInput
            //
            _keepDaysInput.Maximum = new decimal(new int[] { 36500, 0, 0, 0 });
            _keepDaysInput.Name = "_keepDaysInput";
            _keepDaysInput.TextAlign = HorizontalAlignment.Right;
            _keepDaysInput.ThousandsSeparator = true;
            _keepDaysInput.Width = 85;
            _toolTip.SetToolTip(_keepDaysInput, "そのファイルの最終更新日時から遡ってこの日数以内に作られた過去バージョンを残します。\n現在時刻ではなくファイル自身の更新日時が基準です。0で無効。");
            //
            // _keepDaysSuffix
            //
            _keepDaysSuffix.Anchor = AnchorStyles.Left;
            _keepDaysSuffix.AutoSize = true;
            _keepDaysSuffix.Margin = new Padding(3, 7, 3, 3);
            _keepDaysSuffix.Name = "_keepDaysSuffix";
            _keepDaysSuffix.Text = "日";
            _toolTip.SetToolTip(_keepDaysSuffix, "そのファイルの最終更新日時から遡ってこの日数以内に作られた過去バージョンを残します。\n現在時刻ではなくファイル自身の更新日時が基準です。0で無効。");
            //
            // _protectDaysPanel
            //
            _protectDaysPanel.AutoSize = true;
            _protectDaysPanel.FlowDirection = FlowDirection.LeftToRight;
            _protectDaysPanel.Margin = new Padding(0, 0, 18, 0);
            _protectDaysPanel.Name = "_protectDaysPanel";
            _protectDaysPanel.WrapContents = false;
            _protectDaysPanel.Controls.Add(_protectDaysLabel);
            _protectDaysPanel.Controls.Add(_protectDaysInput);
            _protectDaysPanel.Controls.Add(_protectDaysSuffix);
            //
            // _protectDaysLabel
            //
            _protectDaysLabel.Anchor = AnchorStyles.Left;
            _protectDaysLabel.AutoSize = true;
            _protectDaysLabel.Margin = new Padding(3, 7, 5, 3);
            _protectDaysLabel.Name = "_protectDaysLabel";
            _protectDaysLabel.Text = "更新直後は対象外";
            _toolTip.SetToolTip(_protectDaysLabel, "現在時刻からこの日数以内に更新されたファイルは、丸ごと処理対象から外します。0で無効。");
            //
            // _protectDaysInput
            //
            _protectDaysInput.Maximum = new decimal(new int[] { 36500, 0, 0, 0 });
            _protectDaysInput.Name = "_protectDaysInput";
            _protectDaysInput.TextAlign = HorizontalAlignment.Right;
            _protectDaysInput.ThousandsSeparator = true;
            _protectDaysInput.Width = 85;
            _toolTip.SetToolTip(_protectDaysInput, "現在時刻からこの日数以内に更新されたファイルは、丸ごと処理対象から外します。0で無効。");
            //
            // _protectDaysSuffix
            //
            _protectDaysSuffix.Anchor = AnchorStyles.Left;
            _protectDaysSuffix.AutoSize = true;
            _protectDaysSuffix.Margin = new Padding(3, 7, 3, 3);
            _protectDaysSuffix.Name = "_protectDaysSuffix";
            _protectDaysSuffix.Text = "日以内";
            _toolTip.SetToolTip(_protectDaysSuffix, "現在時刻からこの日数以内に更新されたファイルは、丸ごと処理対象から外します。0で無効。");
            //
            // _minimumFileSizePanel
            //
            _minimumFileSizePanel.AutoSize = true;
            _minimumFileSizePanel.FlowDirection = FlowDirection.LeftToRight;
            _minimumFileSizePanel.Margin = new Padding(0, 0, 18, 0);
            _minimumFileSizePanel.Name = "_minimumFileSizePanel";
            _minimumFileSizePanel.WrapContents = false;
            _minimumFileSizePanel.Controls.Add(_minimumFileSizeLabel);
            _minimumFileSizePanel.Controls.Add(_minimumFileSizeInput);
            _minimumFileSizePanel.Controls.Add(_minimumFileSizeSuffix);
            //
            // _minimumFileSizeLabel
            //
            _minimumFileSizeLabel.Anchor = AnchorStyles.Left;
            _minimumFileSizeLabel.AutoSize = true;
            _minimumFileSizeLabel.Margin = new Padding(3, 7, 5, 3);
            _minimumFileSizeLabel.Name = "_minimumFileSizeLabel";
            _minimumFileSizeLabel.Text = "処理する最小ファイルサイズ";
            _toolTip.SetToolTip(_minimumFileSizeLabel, "現行サイズがこの値以上のファイルだけを処理します。\n小さいファイルは何世代あっても削減量が知れているため、サーバーに問い合わせません。\nサイズはローカルの同期フォルダーから読むので、この絞り込みに通信は発生しません。0で無効。");
            //
            // _minimumFileSizeInput
            //
            _minimumFileSizeInput.Maximum = new decimal(new int[] { 10000000, 0, 0, 0 });
            _minimumFileSizeInput.Name = "_minimumFileSizeInput";
            _minimumFileSizeInput.TextAlign = HorizontalAlignment.Right;
            _minimumFileSizeInput.ThousandsSeparator = true;
            _minimumFileSizeInput.Width = 85;
            _toolTip.SetToolTip(_minimumFileSizeInput, "現行サイズがこの値以上のファイルだけを処理します。\n小さいファイルは何世代あっても削減量が知れているため、サーバーに問い合わせません。\nサイズはローカルの同期フォルダーから読むので、この絞り込みに通信は発生しません。0で無効。");
            //
            // _minimumFileSizeSuffix
            //
            _minimumFileSizeSuffix.Anchor = AnchorStyles.Left;
            _minimumFileSizeSuffix.AutoSize = true;
            _minimumFileSizeSuffix.Margin = new Padding(3, 7, 3, 3);
            _minimumFileSizeSuffix.Name = "_minimumFileSizeSuffix";
            _minimumFileSizeSuffix.Text = "MB以上";
            _toolTip.SetToolTip(_minimumFileSizeSuffix, "現行サイズがこの値以上のファイルだけを処理します。\n小さいファイルは何世代あっても削減量が知れているため、サーバーに問い合わせません。\nサイズはローカルの同期フォルダーから読むので、この絞り込みに通信は発生しません。0で無効。");
            //
            // _minimumHistoryPanel
            //
            _minimumHistoryPanel.AutoSize = true;
            _minimumHistoryPanel.FlowDirection = FlowDirection.LeftToRight;
            _minimumHistoryPanel.Margin = new Padding(0, 0, 18, 0);
            _minimumHistoryPanel.Name = "_minimumHistoryPanel";
            _minimumHistoryPanel.WrapContents = false;
            _minimumHistoryPanel.Controls.Add(_minimumHistoryLabel);
            _minimumHistoryPanel.Controls.Add(_minimumHistoryInput);
            _minimumHistoryPanel.Controls.Add(_minimumHistorySuffix);
            //
            // _minimumHistoryLabel
            //
            _minimumHistoryLabel.Anchor = AnchorStyles.Left;
            _minimumHistoryLabel.AutoSize = true;
            _minimumHistoryLabel.Margin = new Padding(3, 7, 5, 3);
            _minimumHistoryLabel.Name = "_minimumHistoryLabel";
            _minimumHistoryLabel.Text = "処理する最小履歴容量";
            _toolTip.SetToolTip(_minimumHistoryLabel, "過去バージョンの合計がこの値以上のファイルだけを処理します。\nこれ未満のファイルは丸ごと対象外です。0で無効。");
            //
            // _minimumHistoryInput
            //
            _minimumHistoryInput.Maximum = new decimal(new int[] { 10000000, 0, 0, 0 });
            _minimumHistoryInput.Name = "_minimumHistoryInput";
            _minimumHistoryInput.TextAlign = HorizontalAlignment.Right;
            _minimumHistoryInput.ThousandsSeparator = true;
            _minimumHistoryInput.Width = 85;
            _toolTip.SetToolTip(_minimumHistoryInput, "過去バージョンの合計がこの値以上のファイルだけを処理します。\nこれ未満のファイルは丸ごと対象外です。0で無効。");
            //
            // _minimumHistorySuffix
            //
            _minimumHistorySuffix.Anchor = AnchorStyles.Left;
            _minimumHistorySuffix.AutoSize = true;
            _minimumHistorySuffix.Margin = new Padding(3, 7, 3, 3);
            _minimumHistorySuffix.Name = "_minimumHistorySuffix";
            _minimumHistorySuffix.Text = "MB以上";
            _toolTip.SetToolTip(_minimumHistorySuffix, "過去バージョンの合計がこの値以上のファイルだけを処理します。\nこれ未満のファイルは丸ごと対象外です。0で無効。");
            //
            // _cacheAgePanel
            //
            _cacheAgePanel.AutoSize = true;
            _cacheAgePanel.FlowDirection = FlowDirection.LeftToRight;
            _cacheAgePanel.Margin = new Padding(0, 0, 18, 0);
            _cacheAgePanel.Name = "_cacheAgePanel";
            _cacheAgePanel.WrapContents = false;
            _cacheAgePanel.Controls.Add(_cacheAgeLabel);
            _cacheAgePanel.Controls.Add(_cacheAgeInput);
            _cacheAgePanel.Controls.Add(_cacheAgeSuffix);
            //
            // _cacheAgeLabel
            //
            _cacheAgeLabel.Anchor = AnchorStyles.Left;
            _cacheAgeLabel.AutoSize = true;
            _cacheAgeLabel.Margin = new Padding(3, 7, 5, 3);
            _cacheAgeLabel.Name = "_cacheAgeLabel";
            _cacheAgeLabel.Text = "再問い合わせの間隔";
            _toolTip.SetToolTip(_cacheAgeLabel, "一度取得したバージョン履歴は保存され、次回以降は再利用します。\n同じファイルを問い合わせ直すのは、前回の取得からこの日数が経ち、かつ\nファイルが更新されている場合だけです。\n履歴は編集したときにしか増えないため、更新のないファイルは問い合わせません。0で毎回。");
            //
            // _cacheAgeInput
            //
            _cacheAgeInput.Maximum = new decimal(new int[] { 36500, 0, 0, 0 });
            _cacheAgeInput.Name = "_cacheAgeInput";
            _cacheAgeInput.TextAlign = HorizontalAlignment.Right;
            _cacheAgeInput.ThousandsSeparator = true;
            _cacheAgeInput.Width = 85;
            _toolTip.SetToolTip(_cacheAgeInput, "一度取得したバージョン履歴は保存され、次回以降は再利用します。\n同じファイルを問い合わせ直すのは、前回の取得からこの日数が経ち、かつ\nファイルが更新されている場合だけです。\n履歴は編集したときにしか増えないため、更新のないファイルは問い合わせません。0で毎回。");
            //
            // _cacheAgeSuffix
            //
            _cacheAgeSuffix.Anchor = AnchorStyles.Left;
            _cacheAgeSuffix.AutoSize = true;
            _cacheAgeSuffix.Margin = new Padding(3, 7, 3, 3);
            _cacheAgeSuffix.Name = "_cacheAgeSuffix";
            _cacheAgeSuffix.Text = "日以上";
            _toolTip.SetToolTip(_cacheAgeSuffix, "一度取得したバージョン履歴は保存され、次回以降は再利用します。\n同じファイルを問い合わせ直すのは、前回の取得からこの日数が経ち、かつ\nファイルが更新されている場合だけです。\n履歴は編集したときにしか増えないため、更新のないファイルは問い合わせません。0で毎回。");
            //
            // _maximumCandidatesPanel
            //
            _maximumCandidatesPanel.AutoSize = true;
            _maximumCandidatesPanel.FlowDirection = FlowDirection.LeftToRight;
            _maximumCandidatesPanel.Margin = new Padding(0, 0, 18, 0);
            _maximumCandidatesPanel.Name = "_maximumCandidatesPanel";
            _maximumCandidatesPanel.WrapContents = false;
            _maximumCandidatesPanel.Controls.Add(_maximumCandidatesLabel);
            _maximumCandidatesPanel.Controls.Add(_maximumCandidatesInput);
            _maximumCandidatesPanel.Controls.Add(_maximumCandidatesSuffix);
            //
            // _maximumCandidatesLabel
            //
            _maximumCandidatesLabel.Anchor = AnchorStyles.Left;
            _maximumCandidatesLabel.AutoSize = true;
            _maximumCandidatesLabel.Margin = new Padding(3, 7, 5, 3);
            _maximumCandidatesLabel.Name = "_maximumCandidatesLabel";
            _maximumCandidatesLabel.Text = "1回の最大移動数";
            _toolTip.SetToolTip(_maximumCandidatesLabel, "1回の実行でごみ箱へ移動する上限です。超えた分は次回に見送ります。");
            //
            // _maximumCandidatesInput
            //
            _maximumCandidatesInput.Maximum = new decimal(new int[] { 1000000, 0, 0, 0 });
            _maximumCandidatesInput.Name = "_maximumCandidatesInput";
            _maximumCandidatesInput.TextAlign = HorizontalAlignment.Right;
            _maximumCandidatesInput.ThousandsSeparator = true;
            _maximumCandidatesInput.Width = 85;
            _toolTip.SetToolTip(_maximumCandidatesInput, "1回の実行でごみ箱へ移動する上限です。超えた分は次回に見送ります。");
            //
            // _maximumCandidatesSuffix
            //
            _maximumCandidatesSuffix.Anchor = AnchorStyles.Left;
            _maximumCandidatesSuffix.AutoSize = true;
            _maximumCandidatesSuffix.Margin = new Padding(3, 7, 3, 3);
            _maximumCandidatesSuffix.Name = "_maximumCandidatesSuffix";
            _maximumCandidatesSuffix.Text = "件";
            _toolTip.SetToolTip(_maximumCandidatesSuffix, "1回の実行でごみ箱へ移動する上限です。超えた分は次回に見送ります。");
            //
            // _candidatesOnlyCheck
            //
            _candidatesOnlyCheck.AutoSize = true;
            _candidatesOnlyCheck.Checked = true;
            _candidatesOnlyCheck.CheckState = CheckState.Checked;
            _candidatesOnlyCheck.Margin = new Padding(16, 7, 3, 3);
            _candidatesOnlyCheck.Name = "_candidatesOnlyCheck";
            _candidatesOnlyCheck.Text = "削除候補のみ表示";
            _candidatesOnlyCheck.CheckedChanged += CandidatesOnlyCheckOnCheckedChanged;
            //
            // _summaryPanel
            //
            _summaryPanel.AutoSize = true;
            _summaryPanel.ColumnCount = 1;
            _summaryPanel.Dock = DockStyle.Top;
            _summaryPanel.Name = "_summaryPanel";
            _summaryPanel.Padding = new Padding(4, 8, 4, 8);
            _summaryPanel.RowCount = 1;
            _summaryPanel.Controls.Add(_summaryLabel, 0, 0);
            //
            // _summaryLabel
            //
            _summaryLabel.AutoSize = true;
            _summaryLabel.Name = "_summaryLabel";
            _summaryLabel.Text = "データはまだありません。";
            //
            // _splitContainer
            //
            // The real splitter geometry is applied once the form has a real size; a distance or
            // a minimum size fixed here would fight the size this control has during
            // initialization. See MainForm.ApplySplitterLayout.
            _splitContainer.Dock = DockStyle.Fill;
            _splitContainer.Name = "_splitContainer";
            _splitContainer.Orientation = Orientation.Horizontal;
            _splitContainer.Panel1.Controls.Add(_versionsLayout);
            _splitContainer.Panel2.Controls.Add(_filesLayout);
            _splitContainer.SplitterDistance = 50;
            //
            // _versionsLayout
            //
            _versionsLayout.ColumnCount = 1;
            _versionsLayout.Dock = DockStyle.Fill;
            _versionsLayout.Name = "_versionsLayout";
            _versionsLayout.RowCount = 2;
            _versionsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _versionsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            _versionsLayout.Controls.Add(_versionsHeaderLabel, 0, 0);
            _versionsLayout.Controls.Add(_versionsGrid, 0, 1);
            //
            // _versionsHeaderLabel
            //
            _versionsHeaderLabel.AutoSize = true;
            _versionsHeaderLabel.Dock = DockStyle.Top;
            _versionsHeaderLabel.Name = "_versionsHeaderLabel";
            _versionsHeaderLabel.Padding = new Padding(2, 2, 2, 4);
            _versionsHeaderLabel.Text = "過去バージョン";
            //
            // _versionsGrid
            //
            _versionsGrid.AllowUserToAddRows = false;
            _versionsGrid.AllowUserToDeleteRows = false;
            _versionsGrid.AllowUserToResizeColumns = true;
            _versionsGrid.AllowUserToResizeRows = false;
            _versionsGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            _versionsGrid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            _versionsGrid.BackgroundColor = SystemColors.Window;
            _versionsGrid.BorderStyle = BorderStyle.Fixed3D;
            _versionsGrid.Columns.AddRange(new DataGridViewColumn[]
            {
                _versionsFileNameColumn,
                _versionsCloudPathColumn,
                _versionsVersionLabelColumn,
                _versionsCreatedColumn,
                _versionsModifiedByColumn,
                _versionsSizeColumn,
                _versionsFileHistorySizeColumn,
                _versionsDecisionColumn,
                _versionsReasonsColumn,
            });
            _versionsGrid.Dock = DockStyle.Fill;
            _versionsGrid.MultiSelect = true;
            _versionsGrid.Name = "_versionsGrid";
            _versionsGrid.ReadOnly = true;
            _versionsGrid.RowHeadersVisible = false;
            _versionsGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _versionsGrid.CellFormatting += VersionsGridOnCellFormatting;
            _versionsGrid.SelectionChanged += VersionsGridOnSelectionChanged;
            //
            // _versionsFileNameColumn
            //
            _versionsFileNameColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            _versionsFileNameColumn.HeaderText = "ファイル名";
            _versionsFileNameColumn.Name = "_versionsFileNameColumn";
            _versionsFileNameColumn.Resizable = DataGridViewTriState.True;
            _versionsFileNameColumn.SortMode = DataGridViewColumnSortMode.Automatic;
            _versionsFileNameColumn.Width = 180;
            //
            // _versionsCloudPathColumn
            //
            _versionsCloudPathColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            _versionsCloudPathColumn.HeaderText = "パス";
            _versionsCloudPathColumn.Name = "_versionsCloudPathColumn";
            _versionsCloudPathColumn.Resizable = DataGridViewTriState.True;
            _versionsCloudPathColumn.SortMode = DataGridViewColumnSortMode.Automatic;
            _versionsCloudPathColumn.Width = 320;
            //
            // _versionsVersionLabelColumn
            //
            _versionsVersionLabelColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            _versionsVersionLabelColumn.HeaderText = "バージョン";
            _versionsVersionLabelColumn.Name = "_versionsVersionLabelColumn";
            _versionsVersionLabelColumn.Resizable = DataGridViewTriState.True;
            _versionsVersionLabelColumn.SortMode = DataGridViewColumnSortMode.Automatic;
            _versionsVersionLabelColumn.Width = 90;
            //
            // _versionsCreatedColumn
            //
            _versionsCreatedColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            _versionsCreatedColumn.HeaderText = "作成日時";
            _versionsCreatedColumn.Name = "_versionsCreatedColumn";
            _versionsCreatedColumn.Resizable = DataGridViewTriState.True;
            _versionsCreatedColumn.SortMode = DataGridViewColumnSortMode.Automatic;
            _versionsCreatedColumn.Width = 150;
            //
            // _versionsModifiedByColumn
            //
            _versionsModifiedByColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            _versionsModifiedByColumn.HeaderText = "更新者";
            _versionsModifiedByColumn.Name = "_versionsModifiedByColumn";
            _versionsModifiedByColumn.Resizable = DataGridViewTriState.True;
            _versionsModifiedByColumn.SortMode = DataGridViewColumnSortMode.Automatic;
            _versionsModifiedByColumn.Width = 130;
            //
            // _versionsSizeColumn
            //
            _versionsSizeColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            _versionsSizeColumn.HeaderText = "サイズ";
            _versionsSizeColumn.Name = "_versionsSizeColumn";
            _versionsSizeColumn.Resizable = DataGridViewTriState.True;
            _versionsSizeColumn.SortMode = DataGridViewColumnSortMode.Automatic;
            _versionsSizeColumn.Width = 105;
            //
            // _versionsFileHistorySizeColumn
            //
            _versionsFileHistorySizeColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            _versionsFileHistorySizeColumn.HeaderText = "ファイル履歴計";
            _versionsFileHistorySizeColumn.Name = "_versionsFileHistorySizeColumn";
            _versionsFileHistorySizeColumn.Resizable = DataGridViewTriState.True;
            _versionsFileHistorySizeColumn.SortMode = DataGridViewColumnSortMode.Automatic;
            _versionsFileHistorySizeColumn.Width = 115;
            //
            // _versionsDecisionColumn
            //
            _versionsDecisionColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            _versionsDecisionColumn.HeaderText = "判定";
            _versionsDecisionColumn.Name = "_versionsDecisionColumn";
            _versionsDecisionColumn.Resizable = DataGridViewTriState.True;
            _versionsDecisionColumn.SortMode = DataGridViewColumnSortMode.Automatic;
            _versionsDecisionColumn.Width = 110;
            //
            // _versionsReasonsColumn
            //
            _versionsReasonsColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            _versionsReasonsColumn.HeaderText = "理由";
            _versionsReasonsColumn.Name = "_versionsReasonsColumn";
            _versionsReasonsColumn.Resizable = DataGridViewTriState.True;
            _versionsReasonsColumn.SortMode = DataGridViewColumnSortMode.Automatic;
            _versionsReasonsColumn.Width = 260;
            //
            // _filesLayout
            //
            _filesLayout.ColumnCount = 1;
            _filesLayout.Dock = DockStyle.Fill;
            _filesLayout.Name = "_filesLayout";
            _filesLayout.RowCount = 2;
            _filesLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _filesLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            _filesLayout.Controls.Add(_filesHeaderLabel, 0, 0);
            _filesLayout.Controls.Add(_filesGrid, 0, 1);
            //
            // _filesHeaderLabel
            //
            _filesHeaderLabel.AutoSize = true;
            _filesHeaderLabel.Dock = DockStyle.Top;
            _filesHeaderLabel.Name = "_filesHeaderLabel";
            _filesHeaderLabel.Padding = new Padding(2, 2, 2, 4);
            _filesHeaderLabel.Text = "ファイル別の履歴容量";
            //
            // _filesGrid
            //
            _filesGrid.AllowUserToAddRows = false;
            _filesGrid.AllowUserToDeleteRows = false;
            _filesGrid.AllowUserToResizeColumns = true;
            _filesGrid.AllowUserToResizeRows = false;
            _filesGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            _filesGrid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            _filesGrid.BackgroundColor = SystemColors.Window;
            _filesGrid.BorderStyle = BorderStyle.Fixed3D;
            _filesGrid.Columns.AddRange(new DataGridViewColumn[]
            {
                _filesFileNameColumn,
                _filesCloudPathColumn,
                _filesCurrentSizeColumn,
                _filesHistoryCountColumn,
                _filesHistorySizeColumn,
                _filesCandidateCountColumn,
                _filesReclaimableColumn,
                _filesLastModifiedColumn,
                _filesStatusColumn,
            });
            _filesGrid.ContextMenuStrip = _fileContextMenu;
            _filesGrid.Dock = DockStyle.Fill;
            _filesGrid.MultiSelect = false;
            _filesGrid.Name = "_filesGrid";
            _filesGrid.ReadOnly = true;
            _filesGrid.RowHeadersVisible = false;
            _filesGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _filesGrid.CellFormatting += FilesGridOnCellFormatting;
            _filesGrid.CellMouseDown += FilesGridOnCellMouseDown;
            //
            // _filesFileNameColumn
            //
            _filesFileNameColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            _filesFileNameColumn.HeaderText = "ファイル名";
            _filesFileNameColumn.Name = "_filesFileNameColumn";
            _filesFileNameColumn.Resizable = DataGridViewTriState.True;
            _filesFileNameColumn.SortMode = DataGridViewColumnSortMode.Automatic;
            _filesFileNameColumn.Width = 180;
            //
            // _filesCloudPathColumn
            //
            _filesCloudPathColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            _filesCloudPathColumn.HeaderText = "パス";
            _filesCloudPathColumn.Name = "_filesCloudPathColumn";
            _filesCloudPathColumn.Resizable = DataGridViewTriState.True;
            _filesCloudPathColumn.SortMode = DataGridViewColumnSortMode.Automatic;
            _filesCloudPathColumn.Width = 320;
            //
            // _filesCurrentSizeColumn
            //
            _filesCurrentSizeColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            _filesCurrentSizeColumn.HeaderText = "現行サイズ";
            _filesCurrentSizeColumn.Name = "_filesCurrentSizeColumn";
            _filesCurrentSizeColumn.Resizable = DataGridViewTriState.True;
            _filesCurrentSizeColumn.SortMode = DataGridViewColumnSortMode.Automatic;
            _filesCurrentSizeColumn.Width = 100;
            //
            // _filesHistoryCountColumn
            //
            _filesHistoryCountColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            _filesHistoryCountColumn.HeaderText = "履歴数";
            _filesHistoryCountColumn.Name = "_filesHistoryCountColumn";
            _filesHistoryCountColumn.Resizable = DataGridViewTriState.True;
            _filesHistoryCountColumn.SortMode = DataGridViewColumnSortMode.Automatic;
            _filesHistoryCountColumn.Width = 75;
            //
            // _filesHistorySizeColumn
            //
            _filesHistorySizeColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            _filesHistorySizeColumn.HeaderText = "履歴容量";
            _filesHistorySizeColumn.Name = "_filesHistorySizeColumn";
            _filesHistorySizeColumn.Resizable = DataGridViewTriState.True;
            _filesHistorySizeColumn.SortMode = DataGridViewColumnSortMode.Automatic;
            _filesHistorySizeColumn.Width = 115;
            //
            // _filesCandidateCountColumn
            //
            _filesCandidateCountColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            _filesCandidateCountColumn.HeaderText = "候補数";
            _filesCandidateCountColumn.Name = "_filesCandidateCountColumn";
            _filesCandidateCountColumn.Resizable = DataGridViewTriState.True;
            _filesCandidateCountColumn.SortMode = DataGridViewColumnSortMode.Automatic;
            _filesCandidateCountColumn.Width = 75;
            //
            // _filesReclaimableColumn
            //
            _filesReclaimableColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            _filesReclaimableColumn.HeaderText = "削減見込み";
            _filesReclaimableColumn.Name = "_filesReclaimableColumn";
            _filesReclaimableColumn.Resizable = DataGridViewTriState.True;
            _filesReclaimableColumn.SortMode = DataGridViewColumnSortMode.Automatic;
            _filesReclaimableColumn.Width = 115;
            //
            // _filesLastModifiedColumn
            //
            _filesLastModifiedColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            _filesLastModifiedColumn.HeaderText = "最終更新";
            _filesLastModifiedColumn.Name = "_filesLastModifiedColumn";
            _filesLastModifiedColumn.Resizable = DataGridViewTriState.True;
            _filesLastModifiedColumn.SortMode = DataGridViewColumnSortMode.Automatic;
            _filesLastModifiedColumn.Width = 140;
            //
            // _filesStatusColumn
            //
            _filesStatusColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            _filesStatusColumn.HeaderText = "状態";
            _filesStatusColumn.Name = "_filesStatusColumn";
            _filesStatusColumn.Resizable = DataGridViewTriState.True;
            _filesStatusColumn.SortMode = DataGridViewColumnSortMode.Automatic;
            _filesStatusColumn.Width = 90;
            //
            // _statusPanel
            //
            _statusPanel.AutoSize = true;
            _statusPanel.ColumnCount = 2;
            _statusPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            _statusPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220F));
            _statusPanel.Dock = DockStyle.Fill;
            _statusPanel.Name = "_statusPanel";
            _statusPanel.RowCount = 1;
            _statusPanel.Controls.Add(_statusLabel, 0, 0);
            _statusPanel.Controls.Add(_progressBar, 1, 0);
            //
            // _statusLabel
            //
            _statusLabel.Anchor = AnchorStyles.Left;
            _statusLabel.AutoSize = true;
            _statusLabel.Name = "_statusLabel";
            _statusLabel.Text = "準備完了";
            //
            // _progressBar
            //
            _progressBar.Dock = DockStyle.Fill;
            _progressBar.Name = "_progressBar";
            _progressBar.Style = ProgressBarStyle.Marquee;
            _progressBar.Visible = false;
            //
            // _fileContextMenu
            //
            _fileContextMenu.Items.AddRange(new ToolStripItem[]
            {
                _openFolderMenuItem,
                _openOnWebMenuItem,
            });
            _fileContextMenu.Name = "_fileContextMenu";
            _fileContextMenu.Opening += FileContextMenuOnOpening;
            //
            // _openFolderMenuItem
            //
            _openFolderMenuItem.Name = "_openFolderMenuItem";
            _openFolderMenuItem.Text = "フォルダーを開く";
            _openFolderMenuItem.Click += OpenFolderMenuItemOnClick;
            //
            // _openOnWebMenuItem
            //
            _openOnWebMenuItem.Name = "_openOnWebMenuItem";
            _openOnWebMenuItem.Text = "Webで開く";
            _openOnWebMenuItem.Click += OpenOnWebMenuItemOnClick;
            //
            // _toolTip
            //
            _toolTip.AutoPopDelay = 20000;
            //
            // MainForm
            //
            AutoScaleMode = AutoScaleMode.Dpi;
            MinimumSize = new Size(1100, 720);
            Name = "MainForm";
            Size = new Size(1360, 860);
            StartPosition = FormStartPosition.CenterScreen;
            Text = "OneDrive Version History Cleaner";
            Controls.Add(_rootLayout);
            _rootLayout.ResumeLayout(false);
            _rootLayout.PerformLayout();
            _connectionGroup.ResumeLayout(false);
            _connectionGroup.PerformLayout();
            _connectionLayout.ResumeLayout(false);
            _connectionLayout.PerformLayout();
            _actionsPanel.ResumeLayout(false);
            _actionsPanel.PerformLayout();
            _policyGroup.ResumeLayout(false);
            _policyGroup.PerformLayout();
            _policyFlow.ResumeLayout(false);
            _policyFlow.PerformLayout();
            _keepVersionsPanel.ResumeLayout(false);
            _keepVersionsPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)_keepVersionsInput).EndInit();
            _keepDaysPanel.ResumeLayout(false);
            _keepDaysPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)_keepDaysInput).EndInit();
            _protectDaysPanel.ResumeLayout(false);
            _protectDaysPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)_protectDaysInput).EndInit();
            _minimumFileSizePanel.ResumeLayout(false);
            _minimumFileSizePanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)_minimumFileSizeInput).EndInit();
            _minimumHistoryPanel.ResumeLayout(false);
            _minimumHistoryPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)_minimumHistoryInput).EndInit();
            _cacheAgePanel.ResumeLayout(false);
            _cacheAgePanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)_cacheAgeInput).EndInit();
            _maximumCandidatesPanel.ResumeLayout(false);
            _maximumCandidatesPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)_maximumCandidatesInput).EndInit();
            _summaryPanel.ResumeLayout(false);
            _summaryPanel.PerformLayout();
            _splitContainer.Panel1.ResumeLayout(false);
            _splitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)_splitContainer).EndInit();
            _splitContainer.ResumeLayout(false);
            _versionsLayout.ResumeLayout(false);
            _versionsLayout.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)_versionsGrid).EndInit();
            _filesLayout.ResumeLayout(false);
            _filesLayout.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)_filesGrid).EndInit();
            _statusPanel.ResumeLayout(false);
            _statusPanel.PerformLayout();
            _fileContextMenu.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private TableLayoutPanel _rootLayout;
        private GroupBox _connectionGroup;
        private TableLayoutPanel _connectionLayout;
        private Label _accountLabel;
        private Label _rootLabel;
        private TextBox _rootBox;
        private Button _browseRootButton;
        private Label _targetLabel;
        private TextBox _targetBox;
        private Button _browseTargetButton;
        private FlowLayoutPanel _actionsPanel;
        private Button _probeButton;
        private Button _scanButton;
        private Button _stopButton;
        private Button _exportButton;
        private Button _deleteButton;
        private GroupBox _policyGroup;
        private FlowLayoutPanel _policyFlow;
        private FlowLayoutPanel _keepVersionsPanel;
        private Label _keepVersionsLabel;
        private NumericUpDown _keepVersionsInput;
        private Label _keepVersionsSuffix;
        private FlowLayoutPanel _keepDaysPanel;
        private Label _keepDaysLabel;
        private NumericUpDown _keepDaysInput;
        private Label _keepDaysSuffix;
        private FlowLayoutPanel _protectDaysPanel;
        private Label _protectDaysLabel;
        private NumericUpDown _protectDaysInput;
        private Label _protectDaysSuffix;
        private FlowLayoutPanel _minimumFileSizePanel;
        private Label _minimumFileSizeLabel;
        private NumericUpDown _minimumFileSizeInput;
        private Label _minimumFileSizeSuffix;
        private FlowLayoutPanel _minimumHistoryPanel;
        private Label _minimumHistoryLabel;
        private NumericUpDown _minimumHistoryInput;
        private Label _minimumHistorySuffix;
        private FlowLayoutPanel _cacheAgePanel;
        private Label _cacheAgeLabel;
        private NumericUpDown _cacheAgeInput;
        private Label _cacheAgeSuffix;
        private FlowLayoutPanel _maximumCandidatesPanel;
        private Label _maximumCandidatesLabel;
        private NumericUpDown _maximumCandidatesInput;
        private Label _maximumCandidatesSuffix;
        private CheckBox _candidatesOnlyCheck;
        private TableLayoutPanel _summaryPanel;
        private Label _summaryLabel;
        private SplitContainer _splitContainer;
        private TableLayoutPanel _versionsLayout;
        private Label _versionsHeaderLabel;
        private DataGridView _versionsGrid;
        private DataGridViewTextBoxColumn _versionsFileNameColumn;
        private DataGridViewTextBoxColumn _versionsCloudPathColumn;
        private DataGridViewTextBoxColumn _versionsVersionLabelColumn;
        private DataGridViewTextBoxColumn _versionsCreatedColumn;
        private DataGridViewTextBoxColumn _versionsModifiedByColumn;
        private DataGridViewTextBoxColumn _versionsSizeColumn;
        private DataGridViewTextBoxColumn _versionsFileHistorySizeColumn;
        private DataGridViewTextBoxColumn _versionsDecisionColumn;
        private DataGridViewTextBoxColumn _versionsReasonsColumn;
        private TableLayoutPanel _filesLayout;
        private Label _filesHeaderLabel;
        private DataGridView _filesGrid;
        private DataGridViewTextBoxColumn _filesFileNameColumn;
        private DataGridViewTextBoxColumn _filesCloudPathColumn;
        private DataGridViewTextBoxColumn _filesCurrentSizeColumn;
        private DataGridViewTextBoxColumn _filesHistoryCountColumn;
        private DataGridViewTextBoxColumn _filesHistorySizeColumn;
        private DataGridViewTextBoxColumn _filesCandidateCountColumn;
        private DataGridViewTextBoxColumn _filesReclaimableColumn;
        private DataGridViewTextBoxColumn _filesLastModifiedColumn;
        private DataGridViewTextBoxColumn _filesStatusColumn;
        private TableLayoutPanel _statusPanel;
        private Label _statusLabel;
        private ProgressBar _progressBar;
        private ContextMenuStrip _fileContextMenu;
        private ToolStripMenuItem _openFolderMenuItem;
        private ToolStripMenuItem _openOnWebMenuItem;
        private ToolTip _toolTip;
    }
}
