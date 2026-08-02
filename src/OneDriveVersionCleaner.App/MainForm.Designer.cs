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
            flowLayoutPanel1 = new FlowLayoutPanel();
            label1 = new Label();
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
            _policyFlow = new FlowLayoutPanel();
            label2 = new Label();
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
            _summaryPanel = new TableLayoutPanel();
            _summaryLabel = new Label();
            _splitContainer = new SplitContainer();
            _versionsLayout = new TableLayoutPanel();
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
            panel1 = new Panel();
            _versionsHeaderLabel = new Label();
            _candidatesOnlyCheck = new CheckBox();
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
            _fileContextMenu = new ContextMenuStrip(components);
            _openFolderMenuItem = new ToolStripMenuItem();
            _openOnWebMenuItem = new ToolStripMenuItem();
            _statusPanel = new TableLayoutPanel();
            _statusLabel = new Label();
            _progressBar = new ProgressBar();
            _toolTip = new ToolTip(components);
            _rootLayout.SuspendLayout();
            _connectionGroup.SuspendLayout();
            _connectionLayout.SuspendLayout();
            _actionsPanel.SuspendLayout();
            _policyGroup.SuspendLayout();
            flowLayoutPanel1.SuspendLayout();
            _keepVersionsPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_keepVersionsInput).BeginInit();
            _keepDaysPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_keepDaysInput).BeginInit();
            _protectDaysPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_protectDaysInput).BeginInit();
            _policyFlow.SuspendLayout();
            _minimumFileSizePanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_minimumFileSizeInput).BeginInit();
            _minimumHistoryPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_minimumHistoryInput).BeginInit();
            _cacheAgePanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_cacheAgeInput).BeginInit();
            _summaryPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_splitContainer).BeginInit();
            _splitContainer.Panel1.SuspendLayout();
            _splitContainer.Panel2.SuspendLayout();
            _splitContainer.SuspendLayout();
            _versionsLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_versionsGrid).BeginInit();
            panel1.SuspendLayout();
            _filesLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_filesGrid).BeginInit();
            _fileContextMenu.SuspendLayout();
            _statusPanel.SuspendLayout();
            SuspendLayout();
            // 
            // _rootLayout
            // 
            _rootLayout.ColumnCount = 1;
            _rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
            _rootLayout.Controls.Add(_connectionGroup, 0, 0);
            _rootLayout.Controls.Add(_policyGroup, 0, 1);
            _rootLayout.Controls.Add(_summaryPanel, 0, 2);
            _rootLayout.Controls.Add(_splitContainer, 0, 3);
            _rootLayout.Controls.Add(_statusPanel, 0, 4);
            _rootLayout.Dock = DockStyle.Fill;
            _rootLayout.Location = new Point(0, 0);
            _rootLayout.Name = "_rootLayout";
            _rootLayout.Padding = new Padding(10);
            _rootLayout.RowCount = 5;
            _rootLayout.RowStyles.Add(new RowStyle());
            _rootLayout.RowStyles.Add(new RowStyle());
            _rootLayout.RowStyles.Add(new RowStyle());
            _rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            _rootLayout.RowStyles.Add(new RowStyle());
            _rootLayout.Size = new Size(1344, 1067);
            _rootLayout.TabIndex = 1;
            // 
            // _connectionGroup
            // 
            _connectionGroup.AutoSize = true;
            _connectionGroup.Controls.Add(_connectionLayout);
            _connectionGroup.Dock = DockStyle.Top;
            _connectionGroup.Location = new Point(13, 13);
            _connectionGroup.Name = "_connectionGroup";
            _connectionGroup.Padding = new Padding(8);
            _connectionGroup.Size = new Size(1318, 178);
            _connectionGroup.TabIndex = 0;
            _connectionGroup.TabStop = false;
            _connectionGroup.Text = "接続と対象（管理センター不要／自分の OneDrive のみ）";
            // 
            // _connectionLayout
            // 
            _connectionLayout.AutoSize = true;
            _connectionLayout.ColumnCount = 3;
            _connectionLayout.ColumnStyles.Add(new ColumnStyle());
            _connectionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            _connectionLayout.ColumnStyles.Add(new ColumnStyle());
            _connectionLayout.Controls.Add(_accountLabel, 0, 0);
            _connectionLayout.Controls.Add(_rootLabel, 0, 1);
            _connectionLayout.Controls.Add(_rootBox, 1, 1);
            _connectionLayout.Controls.Add(_browseRootButton, 2, 1);
            _connectionLayout.Controls.Add(_targetLabel, 0, 2);
            _connectionLayout.Controls.Add(_targetBox, 1, 2);
            _connectionLayout.Controls.Add(_browseTargetButton, 2, 2);
            _connectionLayout.Controls.Add(_actionsPanel, 1, 3);
            _connectionLayout.Dock = DockStyle.Top;
            _connectionLayout.Location = new Point(8, 26);
            _connectionLayout.Name = "_connectionLayout";
            _connectionLayout.RowCount = 4;
            _connectionLayout.RowStyles.Add(new RowStyle());
            _connectionLayout.RowStyles.Add(new RowStyle());
            _connectionLayout.RowStyles.Add(new RowStyle());
            _connectionLayout.RowStyles.Add(new RowStyle());
            _connectionLayout.Size = new Size(1302, 144);
            _connectionLayout.TabIndex = 0;
            // 
            // _accountLabel
            // 
            _accountLabel.AutoSize = true;
            _connectionLayout.SetColumnSpan(_accountLabel, 3);
            _accountLabel.Location = new Point(3, 0);
            _accountLabel.Name = "_accountLabel";
            _accountLabel.Padding = new Padding(0, 2, 0, 6);
            _accountLabel.Size = new Size(247, 27);
            _accountLabel.TabIndex = 0;
            _accountLabel.Text = "OneDriveアカウントを自動検出しています...";
            // 
            // _rootLabel
            // 
            _rootLabel.Anchor = AnchorStyles.Left;
            _rootLabel.AutoSize = true;
            _rootLabel.Location = new Point(3, 37);
            _rootLabel.Margin = new Padding(3, 8, 8, 3);
            _rootLabel.Name = "_rootLabel";
            _rootLabel.Size = new Size(68, 19);
            _rootLabel.TabIndex = 1;
            _rootLabel.Text = "同期ルート";
            // 
            // _rootBox
            // 
            _rootBox.Dock = DockStyle.Fill;
            _rootBox.Location = new Point(104, 31);
            _rootBox.Margin = new Padding(4);
            _rootBox.Name = "_rootBox";
            _rootBox.ReadOnly = true;
            _rootBox.Size = new Size(1080, 25);
            _rootBox.TabIndex = 2;
            // 
            // _browseRootButton
            // 
            _browseRootButton.AutoSize = true;
            _browseRootButton.Location = new Point(1191, 30);
            _browseRootButton.Name = "_browseRootButton";
            _browseRootButton.Size = new Size(75, 29);
            _browseRootButton.TabIndex = 3;
            _browseRootButton.Text = "再検出";
            _browseRootButton.Click += BrowseRootButtonOnClick;
            // 
            // _targetLabel
            // 
            _targetLabel.Anchor = AnchorStyles.Left;
            _targetLabel.AutoSize = true;
            _targetLabel.Location = new Point(3, 72);
            _targetLabel.Margin = new Padding(3, 8, 8, 3);
            _targetLabel.Name = "_targetLabel";
            _targetLabel.Size = new Size(89, 19);
            _targetLabel.TabIndex = 4;
            _targetLabel.Text = "対象フォルダー";
            // 
            // _targetBox
            // 
            _targetBox.AllowDrop = true;
            _targetBox.Dock = DockStyle.Fill;
            _targetBox.Location = new Point(104, 66);
            _targetBox.Margin = new Padding(4);
            _targetBox.Name = "_targetBox";
            _targetBox.Size = new Size(1080, 25);
            _targetBox.TabIndex = 5;
            _targetBox.DragDrop += TargetBoxOnDragDrop;
            _targetBox.DragEnter += TargetBoxOnDragEnter;
            // 
            // _browseTargetButton
            // 
            _browseTargetButton.AutoSize = true;
            _browseTargetButton.Location = new Point(1191, 65);
            _browseTargetButton.Name = "_browseTargetButton";
            _browseTargetButton.Size = new Size(108, 29);
            _browseTargetButton.TabIndex = 6;
            _browseTargetButton.Text = "フォルダー選択...";
            _browseTargetButton.Click += BrowseTargetButtonOnClick;
            // 
            // _actionsPanel
            // 
            _actionsPanel.AutoSize = true;
            _connectionLayout.SetColumnSpan(_actionsPanel, 2);
            _actionsPanel.Controls.Add(_probeButton);
            _actionsPanel.Controls.Add(_scanButton);
            _actionsPanel.Controls.Add(_stopButton);
            _actionsPanel.Controls.Add(_exportButton);
            _actionsPanel.Controls.Add(_deleteButton);
            _actionsPanel.Dock = DockStyle.Fill;
            _actionsPanel.Location = new Point(103, 100);
            _actionsPanel.Name = "_actionsPanel";
            _actionsPanel.Padding = new Padding(0, 6, 0, 0);
            _actionsPanel.Size = new Size(1196, 41);
            _actionsPanel.TabIndex = 7;
            _actionsPanel.WrapContents = false;
            // 
            // _probeButton
            // 
            _probeButton.AutoSize = true;
            _probeButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            _probeButton.Location = new Point(3, 9);
            _probeButton.Name = "_probeButton";
            _probeButton.Size = new Size(47, 29);
            _probeButton.TabIndex = 0;
            _probeButton.Text = "接続";
            _probeButton.Click += ProbeButtonOnClick;
            // 
            // _scanButton
            // 
            _scanButton.AutoSize = true;
            _scanButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            _scanButton.Location = new Point(56, 9);
            _scanButton.Name = "_scanButton";
            _scanButton.Size = new Size(86, 29);
            _scanButton.TabIndex = 1;
            _scanButton.Text = "履歴を取得";
            _scanButton.Click += ScanButtonOnClick;
            // 
            // _stopButton
            // 
            _stopButton.AutoSize = true;
            _stopButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            _stopButton.Enabled = false;
            _stopButton.Location = new Point(148, 9);
            _stopButton.Name = "_stopButton";
            _stopButton.Size = new Size(47, 29);
            _stopButton.TabIndex = 2;
            _stopButton.Text = "停止";
            _stopButton.Click += StopButtonOnClick;
            // 
            // _exportButton
            // 
            _exportButton.AutoSize = true;
            _exportButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            _exportButton.Enabled = false;
            _exportButton.Location = new Point(201, 9);
            _exportButton.Name = "_exportButton";
            _exportButton.Size = new Size(122, 29);
            _exportButton.TabIndex = 3;
            _exportButton.Text = "CSV / JSON 出力";
            _exportButton.Click += ExportButtonOnClick;
            // 
            // _deleteButton
            // 
            _deleteButton.AutoSize = true;
            _deleteButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            _deleteButton.Enabled = false;
            _deleteButton.ForeColor = Color.Firebrick;
            _deleteButton.Location = new Point(329, 9);
            _deleteButton.Name = "_deleteButton";
            _deleteButton.Size = new Size(132, 29);
            _deleteButton.TabIndex = 4;
            _deleteButton.Text = "選択をごみ箱へ移動";
            _deleteButton.Click += DeleteButtonOnClick;
            // 
            // _policyGroup
            // 
            _policyGroup.AutoSize = true;
            _policyGroup.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            _policyGroup.Controls.Add(flowLayoutPanel1);
            _policyGroup.Controls.Add(_policyFlow);
            _policyGroup.Dock = DockStyle.Top;
            _policyGroup.Location = new Point(13, 197);
            _policyGroup.Name = "_policyGroup";
            _policyGroup.Padding = new Padding(8);
            _policyGroup.Size = new Size(1318, 127);
            _policyGroup.TabIndex = 1;
            _policyGroup.TabStop = false;
            _policyGroup.Text = "条件（変更すると即時再計算）";
            //
            // flowLayoutPanel1
            //
            flowLayoutPanel1.AutoSize = true;
            flowLayoutPanel1.Controls.Add(label1);
            flowLayoutPanel1.Controls.Add(_keepVersionsPanel);
            flowLayoutPanel1.Controls.Add(_keepDaysPanel);
            flowLayoutPanel1.Controls.Add(_protectDaysPanel);
            flowLayoutPanel1.Dock = DockStyle.Top;
            flowLayoutPanel1.Location = new Point(8, 57);
            flowLayoutPanel1.Name = "flowLayoutPanel1";
            flowLayoutPanel1.Size = new Size(1302, 31);
            flowLayoutPanel1.TabIndex = 1;
            flowLayoutPanel1.WrapContents = false;
            // 
            // label1
            // 
            label1.Anchor = AnchorStyles.Left;
            label1.AutoSize = true;
            label1.Font = new Font("Yu Gothic UI", 10F, FontStyle.Bold);
            label1.Location = new Point(3, 8);
            label1.Margin = new Padding(3, 8, 8, 3);
            label1.Name = "label1";
            label1.Size = new Size(65, 19);
            label1.TabIndex = 4;
            label1.Text = "除外条件";
            // 
            // _keepVersionsPanel
            // 
            _keepVersionsPanel.AutoSize = true;
            _keepVersionsPanel.Controls.Add(_keepVersionsLabel);
            _keepVersionsPanel.Controls.Add(_keepVersionsInput);
            _keepVersionsPanel.Controls.Add(_keepVersionsSuffix);
            _keepVersionsPanel.Location = new Point(76, 0);
            _keepVersionsPanel.Margin = new Padding(0, 0, 6, 0);
            _keepVersionsPanel.Name = "_keepVersionsPanel";
            _keepVersionsPanel.Size = new Size(152, 31);
            _keepVersionsPanel.TabIndex = 0;
            _keepVersionsPanel.WrapContents = false;
            // 
            // _keepVersionsLabel
            // 
            _keepVersionsLabel.Anchor = AnchorStyles.Left;
            _keepVersionsLabel.AutoSize = true;
            _keepVersionsLabel.Location = new Point(3, 8);
            _keepVersionsLabel.Margin = new Padding(3, 7, 0, 3);
            _keepVersionsLabel.Name = "_keepVersionsLabel";
            _keepVersionsLabel.Size = new Size(76, 19);
            _keepVersionsLabel.TabIndex = 0;
            _keepVersionsLabel.Text = "最近の履歴";
            _toolTip.SetToolTip(_keepVersionsLabel, "各ファイルについて、新しいものから数えてこの件数の過去バージョンを残します。0で無効。");
            // 
            // _keepVersionsInput
            // 
            _keepVersionsInput.Location = new Point(79, 3);
            _keepVersionsInput.Margin = new Padding(0, 3, 0, 3);
            _keepVersionsInput.Maximum = new decimal(new int[] { 100000, 0, 0, 0 });
            _keepVersionsInput.Name = "_keepVersionsInput";
            _keepVersionsInput.Size = new Size(47, 25);
            _keepVersionsInput.TabIndex = 1;
            _keepVersionsInput.TextAlign = HorizontalAlignment.Right;
            _keepVersionsInput.ThousandsSeparator = true;
            _toolTip.SetToolTip(_keepVersionsInput, "各ファイルについて、新しいものから数えてこの件数の過去バージョンを残します。0で無効。");
            // 
            // _keepVersionsSuffix
            // 
            _keepVersionsSuffix.Anchor = AnchorStyles.Left;
            _keepVersionsSuffix.AutoSize = true;
            _keepVersionsSuffix.Location = new Point(126, 8);
            _keepVersionsSuffix.Margin = new Padding(0, 7, 3, 3);
            _keepVersionsSuffix.Name = "_keepVersionsSuffix";
            _keepVersionsSuffix.Size = new Size(23, 19);
            _keepVersionsSuffix.TabIndex = 2;
            _keepVersionsSuffix.Text = "件";
            _toolTip.SetToolTip(_keepVersionsSuffix, "各ファイルについて、新しいものから数えてこの件数の過去バージョンを残します。0で無効。");
            // 
            // _keepDaysPanel
            // 
            _keepDaysPanel.AutoSize = true;
            _keepDaysPanel.Controls.Add(_keepDaysLabel);
            _keepDaysPanel.Controls.Add(_keepDaysInput);
            _keepDaysPanel.Controls.Add(_keepDaysSuffix);
            _keepDaysPanel.Location = new Point(234, 0);
            _keepDaysPanel.Margin = new Padding(0, 0, 6, 0);
            _keepDaysPanel.Name = "_keepDaysPanel";
            _keepDaysPanel.Size = new Size(236, 31);
            _keepDaysPanel.TabIndex = 1;
            _keepDaysPanel.WrapContents = false;
            // 
            // _keepDaysLabel
            // 
            _keepDaysLabel.Anchor = AnchorStyles.Left;
            _keepDaysLabel.AutoSize = true;
            _keepDaysLabel.Location = new Point(3, 8);
            _keepDaysLabel.Margin = new Padding(3, 7, 0, 3);
            _keepDaysLabel.Name = "_keepDaysLabel";
            _keepDaysLabel.Size = new Size(133, 19);
            _keepDaysLabel.TabIndex = 0;
            _keepDaysLabel.Text = "最終更新日から遡って";
            _toolTip.SetToolTip(_keepDaysLabel, "そのファイルの最終更新日時から遡ってこの日数以内に作られた過去バージョンを残します。\n現在時刻ではなくファイル自身の更新日時が基準です。0で無効。");
            // 
            // _keepDaysInput
            // 
            _keepDaysInput.Location = new Point(136, 3);
            _keepDaysInput.Margin = new Padding(0, 3, 0, 3);
            _keepDaysInput.Maximum = new decimal(new int[] { 36500, 0, 0, 0 });
            _keepDaysInput.Name = "_keepDaysInput";
            _keepDaysInput.Size = new Size(46, 25);
            _keepDaysInput.TabIndex = 1;
            _keepDaysInput.TextAlign = HorizontalAlignment.Right;
            _keepDaysInput.ThousandsSeparator = true;
            _toolTip.SetToolTip(_keepDaysInput, "そのファイルの最終更新日時から遡ってこの日数以内に作られた過去バージョンを残します。\n現在時刻ではなくファイル自身の更新日時が基準です。0で無効。");
            // 
            // _keepDaysSuffix
            // 
            _keepDaysSuffix.Anchor = AnchorStyles.Left;
            _keepDaysSuffix.AutoSize = true;
            _keepDaysSuffix.Location = new Point(182, 8);
            _keepDaysSuffix.Margin = new Padding(0, 7, 3, 3);
            _keepDaysSuffix.Name = "_keepDaysSuffix";
            _keepDaysSuffix.Size = new Size(51, 19);
            _keepDaysSuffix.TabIndex = 2;
            _keepDaysSuffix.Text = "日以内";
            _toolTip.SetToolTip(_keepDaysSuffix, "そのファイルの最終更新日時から遡ってこの日数以内に作られた過去バージョンを残します。\n現在時刻ではなくファイル自身の更新日時が基準です。0で無効。");
            // 
            // _protectDaysPanel
            // 
            _protectDaysPanel.AutoSize = true;
            _protectDaysPanel.Controls.Add(_protectDaysLabel);
            _protectDaysPanel.Controls.Add(_protectDaysInput);
            _protectDaysPanel.Controls.Add(_protectDaysSuffix);
            _protectDaysPanel.Location = new Point(476, 0);
            _protectDaysPanel.Margin = new Padding(0, 0, 6, 0);
            _protectDaysPanel.Name = "_protectDaysPanel";
            _protectDaysPanel.Size = new Size(215, 31);
            _protectDaysPanel.TabIndex = 2;
            _protectDaysPanel.WrapContents = false;
            // 
            // _protectDaysLabel
            // 
            _protectDaysLabel.Anchor = AnchorStyles.Left;
            _protectDaysLabel.AutoSize = true;
            _protectDaysLabel.Location = new Point(3, 8);
            _protectDaysLabel.Margin = new Padding(3, 7, 0, 3);
            _protectDaysLabel.Name = "_protectDaysLabel";
            _protectDaysLabel.Size = new Size(112, 19);
            _protectDaysLabel.TabIndex = 0;
            _protectDaysLabel.Text = "更新日が現在から";
            _toolTip.SetToolTip(_protectDaysLabel, "現在時刻からこの日数以内に更新されたファイルは、丸ごと処理対象から外します。0で無効。");
            // 
            // _protectDaysInput
            // 
            _protectDaysInput.Location = new Point(115, 3);
            _protectDaysInput.Margin = new Padding(0, 3, 0, 3);
            _protectDaysInput.Maximum = new decimal(new int[] { 36500, 0, 0, 0 });
            _protectDaysInput.Name = "_protectDaysInput";
            _protectDaysInput.Size = new Size(46, 25);
            _protectDaysInput.TabIndex = 1;
            _protectDaysInput.TextAlign = HorizontalAlignment.Right;
            _protectDaysInput.ThousandsSeparator = true;
            _toolTip.SetToolTip(_protectDaysInput, "現在時刻からこの日数以内に更新されたファイルは、丸ごと処理対象から外します。0で無効。");
            // 
            // _protectDaysSuffix
            // 
            _protectDaysSuffix.Anchor = AnchorStyles.Left;
            _protectDaysSuffix.AutoSize = true;
            _protectDaysSuffix.Location = new Point(161, 8);
            _protectDaysSuffix.Margin = new Padding(0, 7, 3, 3);
            _protectDaysSuffix.Name = "_protectDaysSuffix";
            _protectDaysSuffix.Size = new Size(51, 19);
            _protectDaysSuffix.TabIndex = 2;
            _protectDaysSuffix.Text = "日以内";
            _toolTip.SetToolTip(_protectDaysSuffix, "現在時刻からこの日数以内に更新されたファイルは、丸ごと処理対象から外します。0で無効。");
            // 
            // _policyFlow
            // 
            _policyFlow.AutoSize = true;
            _policyFlow.Controls.Add(label2);
            _policyFlow.Controls.Add(_minimumFileSizePanel);
            _policyFlow.Controls.Add(_minimumHistoryPanel);
            _policyFlow.Controls.Add(_cacheAgePanel);
            _policyFlow.Dock = DockStyle.Top;
            _policyFlow.Location = new Point(8, 26);
            _policyFlow.Name = "_policyFlow";
            _policyFlow.Size = new Size(1302, 31);
            _policyFlow.TabIndex = 0;
            _policyFlow.WrapContents = false;
            // 
            // label2
            // 
            label2.Anchor = AnchorStyles.Left;
            label2.AutoSize = true;
            label2.Font = new Font("Yu Gothic UI", 10F, FontStyle.Bold);
            label2.Location = new Point(3, 8);
            label2.Margin = new Padding(3, 8, 8, 3);
            label2.Name = "label2";
            label2.Size = new Size(65, 19);
            label2.TabIndex = 4;
            label2.Text = "対象条件";
            // 
            // _minimumFileSizePanel
            // 
            _minimumFileSizePanel.AutoSize = true;
            _minimumFileSizePanel.Controls.Add(_minimumFileSizeLabel);
            _minimumFileSizePanel.Controls.Add(_minimumFileSizeInput);
            _minimumFileSizePanel.Controls.Add(_minimumFileSizeSuffix);
            _minimumFileSizePanel.Location = new Point(76, 0);
            _minimumFileSizePanel.Margin = new Padding(0, 0, 6, 0);
            _minimumFileSizePanel.Name = "_minimumFileSizePanel";
            _minimumFileSizePanel.Size = new Size(211, 31);
            _minimumFileSizePanel.TabIndex = 3;
            _minimumFileSizePanel.WrapContents = false;
            // 
            // _minimumFileSizeLabel
            // 
            _minimumFileSizeLabel.Anchor = AnchorStyles.Left;
            _minimumFileSizeLabel.AutoSize = true;
            _minimumFileSizeLabel.Location = new Point(3, 8);
            _minimumFileSizeLabel.Margin = new Padding(3, 7, 0, 3);
            _minimumFileSizeLabel.Name = "_minimumFileSizeLabel";
            _minimumFileSizeLabel.Size = new Size(93, 19);
            _minimumFileSizeLabel.TabIndex = 0;
            _minimumFileSizeLabel.Text = "ファイルサイズが";
            _toolTip.SetToolTip(_minimumFileSizeLabel, "現行サイズがこの値以上のファイルだけを処理します。\n小さいファイルは何世代あっても削減量が知れているため、サーバーに問い合わせません。\nサイズはローカルの同期フォルダーから読むので、この絞り込みに通信は発生しません。0で無効。");
            // 
            // _minimumFileSizeInput
            // 
            _minimumFileSizeInput.Location = new Point(96, 3);
            _minimumFileSizeInput.Margin = new Padding(0, 3, 0, 3);
            _minimumFileSizeInput.Maximum = new decimal(new int[] { 10000000, 0, 0, 0 });
            _minimumFileSizeInput.Name = "_minimumFileSizeInput";
            _minimumFileSizeInput.Size = new Size(54, 25);
            _minimumFileSizeInput.TabIndex = 1;
            _minimumFileSizeInput.TextAlign = HorizontalAlignment.Right;
            _minimumFileSizeInput.ThousandsSeparator = true;
            _toolTip.SetToolTip(_minimumFileSizeInput, "現行サイズがこの値以上のファイルだけを処理します。\n小さいファイルは何世代あっても削減量が知れているため、サーバーに問い合わせません。\nサイズはローカルの同期フォルダーから読むので、この絞り込みに通信は発生しません。0で無効。");
            // 
            // _minimumFileSizeSuffix
            // 
            _minimumFileSizeSuffix.Anchor = AnchorStyles.Left;
            _minimumFileSizeSuffix.AutoSize = true;
            _minimumFileSizeSuffix.Location = new Point(150, 8);
            _minimumFileSizeSuffix.Margin = new Padding(0, 7, 3, 3);
            _minimumFileSizeSuffix.Name = "_minimumFileSizeSuffix";
            _minimumFileSizeSuffix.Size = new Size(58, 19);
            _minimumFileSizeSuffix.TabIndex = 2;
            _minimumFileSizeSuffix.Text = "MB以上";
            _toolTip.SetToolTip(_minimumFileSizeSuffix, "現行サイズがこの値以上のファイルだけを処理します。\n小さいファイルは何世代あっても削減量が知れているため、サーバーに問い合わせません。\nサイズはローカルの同期フォルダーから読むので、この絞り込みに通信は発生しません。0で無効。");
            // 
            // _minimumHistoryPanel
            // 
            _minimumHistoryPanel.AutoSize = true;
            _minimumHistoryPanel.Controls.Add(_minimumHistoryLabel);
            _minimumHistoryPanel.Controls.Add(_minimumHistoryInput);
            _minimumHistoryPanel.Controls.Add(_minimumHistorySuffix);
            _minimumHistoryPanel.Location = new Point(293, 0);
            _minimumHistoryPanel.Margin = new Padding(0, 0, 6, 0);
            _minimumHistoryPanel.Name = "_minimumHistoryPanel";
            _minimumHistoryPanel.Size = new Size(215, 31);
            _minimumHistoryPanel.TabIndex = 4;
            _minimumHistoryPanel.WrapContents = false;
            // 
            // _minimumHistoryLabel
            // 
            _minimumHistoryLabel.Anchor = AnchorStyles.Left;
            _minimumHistoryLabel.AutoSize = true;
            _minimumHistoryLabel.Location = new Point(3, 8);
            _minimumHistoryLabel.Margin = new Padding(3, 7, 0, 3);
            _minimumHistoryLabel.Name = "_minimumHistoryLabel";
            _minimumHistoryLabel.Size = new Size(94, 19);
            _minimumHistoryLabel.TabIndex = 0;
            _minimumHistoryLabel.Text = "総履歴サイズが";
            _toolTip.SetToolTip(_minimumHistoryLabel, "過去バージョンの合計がこの値以上のファイルだけを処理します。\nこれ未満のファイルは丸ごと対象外です。0で無効。");
            // 
            // _minimumHistoryInput
            // 
            _minimumHistoryInput.Location = new Point(97, 3);
            _minimumHistoryInput.Margin = new Padding(0, 3, 0, 3);
            _minimumHistoryInput.Maximum = new decimal(new int[] { 10000000, 0, 0, 0 });
            _minimumHistoryInput.Name = "_minimumHistoryInput";
            _minimumHistoryInput.Size = new Size(57, 25);
            _minimumHistoryInput.TabIndex = 1;
            _minimumHistoryInput.TextAlign = HorizontalAlignment.Right;
            _minimumHistoryInput.ThousandsSeparator = true;
            _toolTip.SetToolTip(_minimumHistoryInput, "過去バージョンの合計がこの値以上のファイルだけを処理します。\nこれ未満のファイルは丸ごと対象外です。0で無効。");
            // 
            // _minimumHistorySuffix
            // 
            _minimumHistorySuffix.Anchor = AnchorStyles.Left;
            _minimumHistorySuffix.AutoSize = true;
            _minimumHistorySuffix.Location = new Point(154, 8);
            _minimumHistorySuffix.Margin = new Padding(0, 7, 3, 3);
            _minimumHistorySuffix.Name = "_minimumHistorySuffix";
            _minimumHistorySuffix.Size = new Size(58, 19);
            _minimumHistorySuffix.TabIndex = 2;
            _minimumHistorySuffix.Text = "MB以上";
            _toolTip.SetToolTip(_minimumHistorySuffix, "過去バージョンの合計がこの値以上のファイルだけを処理します。\nこれ未満のファイルは丸ごと対象外です。0で無効。");
            // 
            // _cacheAgePanel
            // 
            _cacheAgePanel.AutoSize = true;
            _cacheAgePanel.Controls.Add(_cacheAgeLabel);
            _cacheAgePanel.Controls.Add(_cacheAgeInput);
            _cacheAgePanel.Controls.Add(_cacheAgeSuffix);
            _cacheAgePanel.Location = new Point(514, 0);
            _cacheAgePanel.Margin = new Padding(0, 0, 18, 0);
            _cacheAgePanel.Name = "_cacheAgePanel";
            _cacheAgePanel.Size = new Size(195, 31);
            _cacheAgePanel.TabIndex = 5;
            _cacheAgePanel.WrapContents = false;
            // 
            // _cacheAgeLabel
            // 
            _cacheAgeLabel.Anchor = AnchorStyles.Left;
            _cacheAgeLabel.AutoSize = true;
            _cacheAgeLabel.Location = new Point(3, 8);
            _cacheAgeLabel.Margin = new Padding(3, 7, 0, 3);
            _cacheAgeLabel.Name = "_cacheAgeLabel";
            _cacheAgeLabel.Size = new Size(89, 19);
            _cacheAgeLabel.TabIndex = 0;
            _cacheAgeLabel.Text = "問合わせ間隔";
            _toolTip.SetToolTip(_cacheAgeLabel, "一度取得したバージョン履歴は保存され、次回以降は再利用します。\n同じファイルを問い合わせ直すのは、前回の取得からこの日数が経ち、かつ\nファイルが更新されている場合だけです。\n履歴は編集したときにしか増えないため、更新のないファイルは問い合わせません。0で毎回。");
            // 
            // _cacheAgeInput
            // 
            _cacheAgeInput.Location = new Point(92, 3);
            _cacheAgeInput.Margin = new Padding(0, 3, 0, 3);
            _cacheAgeInput.Maximum = new decimal(new int[] { 36500, 0, 0, 0 });
            _cacheAgeInput.Name = "_cacheAgeInput";
            _cacheAgeInput.Size = new Size(49, 25);
            _cacheAgeInput.TabIndex = 1;
            _cacheAgeInput.TextAlign = HorizontalAlignment.Right;
            _cacheAgeInput.ThousandsSeparator = true;
            _toolTip.SetToolTip(_cacheAgeInput, "一度取得したバージョン履歴は保存され、次回以降は再利用します。\n同じファイルを問い合わせ直すのは、前回の取得からこの日数が経ち、かつ\nファイルが更新されている場合だけです。\n履歴は編集したときにしか増えないため、更新のないファイルは問い合わせません。0で毎回。");
            // 
            // _cacheAgeSuffix
            // 
            _cacheAgeSuffix.Anchor = AnchorStyles.Left;
            _cacheAgeSuffix.AutoSize = true;
            _cacheAgeSuffix.Location = new Point(141, 8);
            _cacheAgeSuffix.Margin = new Padding(0, 7, 3, 3);
            _cacheAgeSuffix.Name = "_cacheAgeSuffix";
            _cacheAgeSuffix.Size = new Size(51, 19);
            _cacheAgeSuffix.TabIndex = 2;
            _cacheAgeSuffix.Text = "日以上";
            _toolTip.SetToolTip(_cacheAgeSuffix, "一度取得したバージョン履歴は保存され、次回以降は再利用します。\n同じファイルを問い合わせ直すのは、前回の取得からこの日数が経ち、かつ\nファイルが更新されている場合だけです。\n履歴は編集したときにしか増えないため、更新のないファイルは問い合わせません。0で毎回。");
            // 
            // _summaryPanel
            // 
            _summaryPanel.AutoSize = true;
            _summaryPanel.ColumnCount = 1;
            _summaryPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
            _summaryPanel.Controls.Add(_summaryLabel, 0, 0);
            _summaryPanel.Dock = DockStyle.Top;
            _summaryPanel.Location = new Point(13, 330);
            _summaryPanel.Name = "_summaryPanel";
            _summaryPanel.Padding = new Padding(4, 8, 4, 8);
            _summaryPanel.RowCount = 1;
            _summaryPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            _summaryPanel.Size = new Size(1318, 36);
            _summaryPanel.TabIndex = 2;
            // 
            // _summaryLabel
            // 
            _summaryLabel.AutoSize = true;
            _summaryLabel.Location = new Point(7, 8);
            _summaryLabel.Name = "_summaryLabel";
            _summaryLabel.Size = new Size(137, 19);
            _summaryLabel.TabIndex = 0;
            _summaryLabel.Text = "データはまだありません。";
            // 
            // _splitContainer
            // 
            _splitContainer.Dock = DockStyle.Fill;
            _splitContainer.Location = new Point(13, 372);
            _splitContainer.Name = "_splitContainer";
            _splitContainer.Orientation = Orientation.Horizontal;
            // 
            // _splitContainer.Panel1
            // 
            _splitContainer.Panel1.Controls.Add(_versionsLayout);
            // 
            // _splitContainer.Panel2
            // 
            _splitContainer.Panel2.Controls.Add(_filesLayout);
            _splitContainer.Size = new Size(1318, 656);
            _splitContainer.SplitterDistance = 328;
            _splitContainer.TabIndex = 3;
            // 
            // _versionsLayout
            // 
            _versionsLayout.ColumnCount = 1;
            _versionsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
            _versionsLayout.Controls.Add(_versionsGrid, 0, 1);
            _versionsLayout.Controls.Add(panel1, 0, 0);
            _versionsLayout.Dock = DockStyle.Fill;
            _versionsLayout.Location = new Point(0, 0);
            _versionsLayout.Name = "_versionsLayout";
            _versionsLayout.RowCount = 2;
            _versionsLayout.RowStyles.Add(new RowStyle());
            _versionsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            _versionsLayout.Size = new Size(1318, 328);
            _versionsLayout.TabIndex = 0;
            // 
            // _versionsGrid
            // 
            _versionsGrid.AllowUserToAddRows = false;
            _versionsGrid.AllowUserToDeleteRows = false;
            _versionsGrid.AllowUserToResizeRows = false;
            _versionsGrid.BackgroundColor = SystemColors.Window;
            _versionsGrid.BorderStyle = BorderStyle.Fixed3D;
            _versionsGrid.ColumnHeadersHeight = 30;
            _versionsGrid.Columns.AddRange(new DataGridViewColumn[] { _versionsFileNameColumn, _versionsCloudPathColumn, _versionsVersionLabelColumn, _versionsCreatedColumn, _versionsModifiedByColumn, _versionsSizeColumn, _versionsFileHistorySizeColumn, _versionsDecisionColumn, _versionsReasonsColumn });
            _versionsGrid.Dock = DockStyle.Fill;
            _versionsGrid.Location = new Point(3, 37);
            _versionsGrid.Name = "_versionsGrid";
            _versionsGrid.ReadOnly = true;
            _versionsGrid.RowHeadersVisible = false;
            _versionsGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _versionsGrid.Size = new Size(1312, 288);
            _versionsGrid.TabIndex = 1;
            _versionsGrid.CellFormatting += VersionsGridOnCellFormatting;
            _versionsGrid.SelectionChanged += VersionsGridOnSelectionChanged;
            // 
            // _versionsFileNameColumn
            // 
            _versionsFileNameColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            _versionsFileNameColumn.HeaderText = "ファイル名";
            _versionsFileNameColumn.Name = "_versionsFileNameColumn";
            _versionsFileNameColumn.ReadOnly = true;
            _versionsFileNameColumn.Resizable = DataGridViewTriState.True;
            _versionsFileNameColumn.Width = 180;
            // 
            // _versionsCloudPathColumn
            // 
            _versionsCloudPathColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            _versionsCloudPathColumn.HeaderText = "パス";
            _versionsCloudPathColumn.Name = "_versionsCloudPathColumn";
            _versionsCloudPathColumn.ReadOnly = true;
            _versionsCloudPathColumn.Resizable = DataGridViewTriState.True;
            _versionsCloudPathColumn.Width = 320;
            // 
            // _versionsVersionLabelColumn
            // 
            _versionsVersionLabelColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            _versionsVersionLabelColumn.HeaderText = "バージョン";
            _versionsVersionLabelColumn.Name = "_versionsVersionLabelColumn";
            _versionsVersionLabelColumn.ReadOnly = true;
            _versionsVersionLabelColumn.Resizable = DataGridViewTriState.True;
            _versionsVersionLabelColumn.Width = 90;
            // 
            // _versionsCreatedColumn
            // 
            _versionsCreatedColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            _versionsCreatedColumn.HeaderText = "作成日時";
            _versionsCreatedColumn.Name = "_versionsCreatedColumn";
            _versionsCreatedColumn.ReadOnly = true;
            _versionsCreatedColumn.Resizable = DataGridViewTriState.True;
            _versionsCreatedColumn.Width = 150;
            // 
            // _versionsModifiedByColumn
            // 
            _versionsModifiedByColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            _versionsModifiedByColumn.HeaderText = "更新者";
            _versionsModifiedByColumn.Name = "_versionsModifiedByColumn";
            _versionsModifiedByColumn.ReadOnly = true;
            _versionsModifiedByColumn.Resizable = DataGridViewTriState.True;
            _versionsModifiedByColumn.Width = 130;
            // 
            // _versionsSizeColumn
            // 
            _versionsSizeColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            _versionsSizeColumn.HeaderText = "サイズ";
            _versionsSizeColumn.Name = "_versionsSizeColumn";
            _versionsSizeColumn.ReadOnly = true;
            _versionsSizeColumn.Resizable = DataGridViewTriState.True;
            _versionsSizeColumn.Width = 105;
            // 
            // _versionsFileHistorySizeColumn
            // 
            _versionsFileHistorySizeColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            _versionsFileHistorySizeColumn.HeaderText = "ファイル履歴計";
            _versionsFileHistorySizeColumn.Name = "_versionsFileHistorySizeColumn";
            _versionsFileHistorySizeColumn.ReadOnly = true;
            _versionsFileHistorySizeColumn.Resizable = DataGridViewTriState.True;
            _versionsFileHistorySizeColumn.Width = 115;
            // 
            // _versionsDecisionColumn
            // 
            _versionsDecisionColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            _versionsDecisionColumn.HeaderText = "判定";
            _versionsDecisionColumn.Name = "_versionsDecisionColumn";
            _versionsDecisionColumn.ReadOnly = true;
            _versionsDecisionColumn.Resizable = DataGridViewTriState.True;
            _versionsDecisionColumn.Width = 110;
            // 
            // _versionsReasonsColumn
            // 
            _versionsReasonsColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            _versionsReasonsColumn.HeaderText = "理由";
            _versionsReasonsColumn.Name = "_versionsReasonsColumn";
            _versionsReasonsColumn.ReadOnly = true;
            _versionsReasonsColumn.Resizable = DataGridViewTriState.True;
            _versionsReasonsColumn.Width = 260;
            // 
            // panel1
            // 
            panel1.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            panel1.Controls.Add(_versionsHeaderLabel);
            panel1.Controls.Add(_candidatesOnlyCheck);
            panel1.Dock = DockStyle.Fill;
            panel1.Location = new Point(3, 3);
            panel1.Name = "panel1";
            panel1.Size = new Size(1312, 28);
            panel1.TabIndex = 8;
            // 
            // _versionsHeaderLabel
            // 
            _versionsHeaderLabel.AutoSize = true;
            _versionsHeaderLabel.Dock = DockStyle.Left;
            _versionsHeaderLabel.Location = new Point(0, 0);
            _versionsHeaderLabel.Name = "_versionsHeaderLabel";
            _versionsHeaderLabel.Padding = new Padding(2, 2, 2, 4);
            _versionsHeaderLabel.Size = new Size(91, 25);
            _versionsHeaderLabel.TabIndex = 0;
            _versionsHeaderLabel.Text = "過去バージョン";
            // 
            // _candidatesOnlyCheck
            // 
            _candidatesOnlyCheck.AutoSize = true;
            _candidatesOnlyCheck.Checked = true;
            _candidatesOnlyCheck.CheckState = CheckState.Checked;
            _candidatesOnlyCheck.Dock = DockStyle.Right;
            _candidatesOnlyCheck.Location = new Point(1177, 0);
            _candidatesOnlyCheck.Margin = new Padding(16, 7, 3, 3);
            _candidatesOnlyCheck.Name = "_candidatesOnlyCheck";
            _candidatesOnlyCheck.Size = new Size(135, 28);
            _candidatesOnlyCheck.TabIndex = 7;
            _candidatesOnlyCheck.Text = "削除候補のみ表示";
            _candidatesOnlyCheck.CheckedChanged += CandidatesOnlyCheckOnCheckedChanged;
            // 
            // _filesLayout
            // 
            _filesLayout.ColumnCount = 1;
            _filesLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
            _filesLayout.Controls.Add(_filesHeaderLabel, 0, 0);
            _filesLayout.Controls.Add(_filesGrid, 0, 1);
            _filesLayout.Dock = DockStyle.Fill;
            _filesLayout.Location = new Point(0, 0);
            _filesLayout.Name = "_filesLayout";
            _filesLayout.RowCount = 2;
            _filesLayout.RowStyles.Add(new RowStyle());
            _filesLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            _filesLayout.Size = new Size(1318, 324);
            _filesLayout.TabIndex = 0;
            // 
            // _filesHeaderLabel
            // 
            _filesHeaderLabel.AutoSize = true;
            _filesHeaderLabel.Dock = DockStyle.Top;
            _filesHeaderLabel.Location = new Point(3, 0);
            _filesHeaderLabel.Name = "_filesHeaderLabel";
            _filesHeaderLabel.Padding = new Padding(2, 2, 2, 4);
            _filesHeaderLabel.Size = new Size(1312, 25);
            _filesHeaderLabel.TabIndex = 0;
            _filesHeaderLabel.Text = "ファイル別の履歴容量";
            // 
            // _filesGrid
            // 
            _filesGrid.AllowUserToAddRows = false;
            _filesGrid.AllowUserToDeleteRows = false;
            _filesGrid.AllowUserToResizeRows = false;
            _filesGrid.BackgroundColor = SystemColors.Window;
            _filesGrid.BorderStyle = BorderStyle.Fixed3D;
            _filesGrid.ColumnHeadersHeight = 30;
            _filesGrid.Columns.AddRange(new DataGridViewColumn[] { _filesFileNameColumn, _filesCloudPathColumn, _filesCurrentSizeColumn, _filesHistoryCountColumn, _filesHistorySizeColumn, _filesCandidateCountColumn, _filesReclaimableColumn, _filesLastModifiedColumn, _filesStatusColumn });
            _filesGrid.ContextMenuStrip = _fileContextMenu;
            _filesGrid.Dock = DockStyle.Fill;
            _filesGrid.Location = new Point(3, 28);
            _filesGrid.MultiSelect = false;
            _filesGrid.Name = "_filesGrid";
            _filesGrid.ReadOnly = true;
            _filesGrid.RowHeadersVisible = false;
            _filesGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _filesGrid.Size = new Size(1312, 293);
            _filesGrid.TabIndex = 1;
            _filesGrid.CellFormatting += FilesGridOnCellFormatting;
            _filesGrid.CellMouseDown += FilesGridOnCellMouseDown;
            // 
            // _filesFileNameColumn
            // 
            _filesFileNameColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            _filesFileNameColumn.HeaderText = "ファイル名";
            _filesFileNameColumn.Name = "_filesFileNameColumn";
            _filesFileNameColumn.ReadOnly = true;
            _filesFileNameColumn.Resizable = DataGridViewTriState.True;
            _filesFileNameColumn.Width = 180;
            // 
            // _filesCloudPathColumn
            // 
            _filesCloudPathColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            _filesCloudPathColumn.HeaderText = "パス";
            _filesCloudPathColumn.Name = "_filesCloudPathColumn";
            _filesCloudPathColumn.ReadOnly = true;
            _filesCloudPathColumn.Resizable = DataGridViewTriState.True;
            _filesCloudPathColumn.Width = 320;
            // 
            // _filesCurrentSizeColumn
            // 
            _filesCurrentSizeColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            _filesCurrentSizeColumn.HeaderText = "現行サイズ";
            _filesCurrentSizeColumn.Name = "_filesCurrentSizeColumn";
            _filesCurrentSizeColumn.ReadOnly = true;
            _filesCurrentSizeColumn.Resizable = DataGridViewTriState.True;
            // 
            // _filesHistoryCountColumn
            // 
            _filesHistoryCountColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            _filesHistoryCountColumn.HeaderText = "履歴数";
            _filesHistoryCountColumn.Name = "_filesHistoryCountColumn";
            _filesHistoryCountColumn.ReadOnly = true;
            _filesHistoryCountColumn.Resizable = DataGridViewTriState.True;
            _filesHistoryCountColumn.Width = 75;
            // 
            // _filesHistorySizeColumn
            // 
            _filesHistorySizeColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            _filesHistorySizeColumn.HeaderText = "履歴容量";
            _filesHistorySizeColumn.Name = "_filesHistorySizeColumn";
            _filesHistorySizeColumn.ReadOnly = true;
            _filesHistorySizeColumn.Resizable = DataGridViewTriState.True;
            _filesHistorySizeColumn.Width = 115;
            // 
            // _filesCandidateCountColumn
            // 
            _filesCandidateCountColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            _filesCandidateCountColumn.HeaderText = "候補数";
            _filesCandidateCountColumn.Name = "_filesCandidateCountColumn";
            _filesCandidateCountColumn.ReadOnly = true;
            _filesCandidateCountColumn.Resizable = DataGridViewTriState.True;
            _filesCandidateCountColumn.Width = 75;
            // 
            // _filesReclaimableColumn
            // 
            _filesReclaimableColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            _filesReclaimableColumn.HeaderText = "削減見込み";
            _filesReclaimableColumn.Name = "_filesReclaimableColumn";
            _filesReclaimableColumn.ReadOnly = true;
            _filesReclaimableColumn.Resizable = DataGridViewTriState.True;
            _filesReclaimableColumn.Width = 115;
            // 
            // _filesLastModifiedColumn
            // 
            _filesLastModifiedColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            _filesLastModifiedColumn.HeaderText = "最終更新";
            _filesLastModifiedColumn.Name = "_filesLastModifiedColumn";
            _filesLastModifiedColumn.ReadOnly = true;
            _filesLastModifiedColumn.Resizable = DataGridViewTriState.True;
            _filesLastModifiedColumn.Width = 140;
            // 
            // _filesStatusColumn
            // 
            _filesStatusColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            _filesStatusColumn.HeaderText = "状態";
            _filesStatusColumn.Name = "_filesStatusColumn";
            _filesStatusColumn.ReadOnly = true;
            _filesStatusColumn.Resizable = DataGridViewTriState.True;
            _filesStatusColumn.Width = 90;
            // 
            // _fileContextMenu
            // 
            _fileContextMenu.Items.AddRange(new ToolStripItem[] { _openFolderMenuItem, _openOnWebMenuItem });
            _fileContextMenu.Name = "_fileContextMenu";
            _fileContextMenu.Size = new Size(146, 48);
            _fileContextMenu.Opening += FileContextMenuOnOpening;
            // 
            // _openFolderMenuItem
            // 
            _openFolderMenuItem.Name = "_openFolderMenuItem";
            _openFolderMenuItem.Size = new Size(145, 22);
            _openFolderMenuItem.Text = "フォルダーを開く";
            _openFolderMenuItem.Click += OpenFolderMenuItemOnClick;
            // 
            // _openOnWebMenuItem
            // 
            _openOnWebMenuItem.Name = "_openOnWebMenuItem";
            _openOnWebMenuItem.Size = new Size(145, 22);
            _openOnWebMenuItem.Text = "Webで開く";
            _openOnWebMenuItem.Click += OpenOnWebMenuItemOnClick;
            // 
            // _statusPanel
            // 
            _statusPanel.AutoSize = true;
            _statusPanel.ColumnCount = 2;
            _statusPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            _statusPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220F));
            _statusPanel.Controls.Add(_statusLabel, 0, 0);
            _statusPanel.Controls.Add(_progressBar, 1, 0);
            _statusPanel.Dock = DockStyle.Fill;
            _statusPanel.Location = new Point(13, 1034);
            _statusPanel.Name = "_statusPanel";
            _statusPanel.RowCount = 1;
            _statusPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            _statusPanel.Size = new Size(1318, 20);
            _statusPanel.TabIndex = 4;
            // 
            // _statusLabel
            // 
            _statusLabel.Anchor = AnchorStyles.Left;
            _statusLabel.AutoSize = true;
            _statusLabel.Location = new Point(3, 0);
            _statusLabel.Name = "_statusLabel";
            _statusLabel.Size = new Size(65, 19);
            _statusLabel.TabIndex = 0;
            _statusLabel.Text = "準備完了";
            // 
            // _progressBar
            // 
            _progressBar.Dock = DockStyle.Fill;
            _progressBar.Location = new Point(1101, 3);
            _progressBar.Name = "_progressBar";
            _progressBar.Size = new Size(214, 14);
            _progressBar.Style = ProgressBarStyle.Marquee;
            _progressBar.TabIndex = 1;
            _progressBar.Visible = false;
            // 
            // _toolTip
            // 
            _toolTip.AutoPopDelay = 20000;
            _toolTip.InitialDelay = 500;
            _toolTip.ReshowDelay = 100;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(1344, 1067);
            Controls.Add(_rootLayout);
            Font = new Font("Yu Gothic UI", 10F);
            MinimumSize = new Size(1100, 720);
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "OneDrive Version History Cleaner";
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
            flowLayoutPanel1.ResumeLayout(false);
            flowLayoutPanel1.PerformLayout();
            _keepVersionsPanel.ResumeLayout(false);
            _keepVersionsPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)_keepVersionsInput).EndInit();
            _keepDaysPanel.ResumeLayout(false);
            _keepDaysPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)_keepDaysInput).EndInit();
            _protectDaysPanel.ResumeLayout(false);
            _protectDaysPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)_protectDaysInput).EndInit();
            _policyFlow.ResumeLayout(false);
            _policyFlow.PerformLayout();
            _minimumFileSizePanel.ResumeLayout(false);
            _minimumFileSizePanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)_minimumFileSizeInput).EndInit();
            _minimumHistoryPanel.ResumeLayout(false);
            _minimumHistoryPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)_minimumHistoryInput).EndInit();
            _cacheAgePanel.ResumeLayout(false);
            _cacheAgePanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)_cacheAgeInput).EndInit();
            _summaryPanel.ResumeLayout(false);
            _summaryPanel.PerformLayout();
            _splitContainer.Panel1.ResumeLayout(false);
            _splitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)_splitContainer).EndInit();
            _splitContainer.ResumeLayout(false);
            _versionsLayout.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)_versionsGrid).EndInit();
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            _filesLayout.ResumeLayout(false);
            _filesLayout.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)_filesGrid).EndInit();
            _fileContextMenu.ResumeLayout(false);
            _statusPanel.ResumeLayout(false);
            _statusPanel.PerformLayout();
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
        private FlowLayoutPanel flowLayoutPanel1;
        private Label label1;
        private Label label2;
        private Panel panel1;
    }
}
