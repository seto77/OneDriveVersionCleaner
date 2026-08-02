// The designer regenerates this file; nullable annotations it cannot emit would be lost.
#nullable disable

namespace OneDriveVersionCleaner.App
{
    partial class DeleteConfirmationForm
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
            _layout = new TableLayoutPanel();
            _summaryLabel = new Label();
            _notesLabel = new Label();
            _targetGroup = new GroupBox();
            _targetBox = new TextBox();
            _buttonPanel = new FlowLayoutPanel();
            _cancelButton = new Button();
            _executeButton = new Button();
            _layout.SuspendLayout();
            _targetGroup.SuspendLayout();
            _buttonPanel.SuspendLayout();
            SuspendLayout();
            //
            // _layout
            //
            _layout.ColumnCount = 1;
            _layout.Dock = DockStyle.Fill;
            _layout.Name = "_layout";
            _layout.RowCount = 4;
            _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _layout.Controls.Add(_summaryLabel, 0, 0);
            _layout.Controls.Add(_notesLabel, 0, 1);
            _layout.Controls.Add(_targetGroup, 0, 2);
            _layout.Controls.Add(_buttonPanel, 0, 3);
            //
            // _summaryLabel
            //
            _summaryLabel.AutoSize = true;
            _summaryLabel.Dock = DockStyle.Top;
            _summaryLabel.Margin = new Padding(0, 0, 0, 12);
            _summaryLabel.Name = "_summaryLabel";
            //
            // _notesLabel
            //
            _notesLabel.AutoSize = true;
            _notesLabel.Dock = DockStyle.Top;
            _notesLabel.Margin = new Padding(0, 0, 0, 12);
            _notesLabel.MaximumSize = new Size(660, 0);
            _notesLabel.Name = "_notesLabel";
            _notesLabel.Text =
                "・ファイル本体と現行バージョンは対象になりません。\r\n" +
                "・移動したバージョンはOneDriveのごみ箱から復元できます。\r\n" +
                "・ごみ箱の項目も容量を消費します。容量を戻すにはごみ箱を空にしてください。\r\n" +
                "・保持ポリシーや訴訟ホールドの対象は、移動できない場合があります。\r\n" +
                "・移動の直後に、ローカルの現行ファイルが無事かを自動で確認します。";
            //
            // _targetGroup
            //
            _targetGroup.Dock = DockStyle.Fill;
            _targetGroup.Margin = new Padding(0, 0, 0, 12);
            _targetGroup.Name = "_targetGroup";
            _targetGroup.Padding = new Padding(6);
            _targetGroup.Text = "移動するバージョン";
            _targetGroup.Controls.Add(_targetBox);
            //
            // _targetBox
            //
            _targetBox.BackColor = SystemColors.Window;
            _targetBox.Dock = DockStyle.Fill;
            _targetBox.Multiline = true;
            _targetBox.Name = "_targetBox";
            _targetBox.ReadOnly = true;
            _targetBox.ScrollBars = ScrollBars.Both;
            _targetBox.WordWrap = false;
            //
            // _buttonPanel
            //
            _buttonPanel.AutoSize = true;
            _buttonPanel.Dock = DockStyle.Top;
            _buttonPanel.FlowDirection = FlowDirection.RightToLeft;
            _buttonPanel.Name = "_buttonPanel";
            _buttonPanel.WrapContents = false;
            _buttonPanel.Controls.Add(_cancelButton);
            _buttonPanel.Controls.Add(_executeButton);
            //
            // _cancelButton
            //
            _cancelButton.AutoSize = true;
            _cancelButton.DialogResult = DialogResult.Cancel;
            _cancelButton.Name = "_cancelButton";
            _cancelButton.Text = "キャンセル";
            //
            // _executeButton
            //
            _executeButton.AutoSize = true;
            _executeButton.DialogResult = DialogResult.OK;
            _executeButton.ForeColor = Color.Firebrick;
            _executeButton.Name = "_executeButton";
            _executeButton.Text = "ごみ箱へ移動";
            //
            // DeleteConfirmationForm
            //
            AutoScaleMode = AutoScaleMode.Dpi;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = false;
            MinimizeBox = false;
            MinimumSize = new Size(680, 520);
            Name = "DeleteConfirmationForm";
            Padding = new Padding(14);
            ShowInTaskbar = false;
            Size = new Size(740, 600);
            StartPosition = FormStartPosition.CenterParent;
            Text = "ごみ箱へ移動する確認";
            Controls.Add(_layout);
            _layout.ResumeLayout(false);
            _layout.PerformLayout();
            _targetGroup.ResumeLayout(false);
            _targetGroup.PerformLayout();
            _buttonPanel.ResumeLayout(false);
            _buttonPanel.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private TableLayoutPanel _layout;
        private Label _summaryLabel;
        private Label _notesLabel;
        private GroupBox _targetGroup;
        private TextBox _targetBox;
        private FlowLayoutPanel _buttonPanel;
        private Button _cancelButton;
        private Button _executeButton;
    }
}
