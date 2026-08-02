// The designer regenerates this file; nullable annotations it cannot emit would be lost.
#nullable disable

namespace OneDriveVersionCleaner.App
{
    partial class SharePointSignInForm
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
            _layout = new TableLayoutPanel();
            _webView = new Microsoft.Web.WebView2.WinForms.WebView2();
            _footer = new TableLayoutPanel();
            _statusLabel = new Label();
            _verifyButton = new Button();
            _reloadButton = new Button();
            _cancelButton = new Button();
            _navigationTimer = new System.Windows.Forms.Timer(components);
            _layout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_webView).BeginInit();
            _footer.SuspendLayout();
            SuspendLayout();
            //
            // _layout
            //
            _layout.ColumnCount = 1;
            _layout.Dock = DockStyle.Fill;
            _layout.Name = "_layout";
            _layout.RowCount = 2;
            _layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _layout.Controls.Add(_webView, 0, 0);
            _layout.Controls.Add(_footer, 0, 1);
            //
            // _webView
            //
            // No Source here: navigation and CoreWebView2 creation are runtime work, started
            // from Shown. See SharePointSignInForm.InitializeBrowserAsync.
            _webView.Dock = DockStyle.Fill;
            _webView.Name = "_webView";
            //
            // _footer
            //
            _footer.AutoSize = true;
            _footer.ColumnCount = 4;
            _footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            _footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            _footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            _footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            _footer.Dock = DockStyle.Fill;
            _footer.Name = "_footer";
            _footer.Padding = new Padding(8);
            _footer.RowCount = 1;
            _footer.Controls.Add(_statusLabel, 0, 0);
            _footer.Controls.Add(_verifyButton, 1, 0);
            _footer.Controls.Add(_reloadButton, 2, 0);
            _footer.Controls.Add(_cancelButton, 3, 0);
            //
            // _statusLabel
            //
            _statusLabel.Anchor = AnchorStyles.Left;
            _statusLabel.AutoSize = true;
            _statusLabel.Name = "_statusLabel";
            _statusLabel.Text = "職場または学校の Microsoft 365 アカウントでサインインしてください。";
            //
            // _verifyButton
            //
            _verifyButton.AutoSize = true;
            _verifyButton.Enabled = false;
            _verifyButton.Name = "_verifyButton";
            _verifyButton.Text = "ログイン完了を確認";
            _verifyButton.Click += VerifyButtonOnClick;
            //
            // _reloadButton
            //
            _reloadButton.AutoSize = true;
            _reloadButton.Enabled = false;
            _reloadButton.Name = "_reloadButton";
            _reloadButton.Text = "ページを再読み込み";
            _reloadButton.Click += ReloadButtonOnClick;
            //
            // _cancelButton
            //
            _cancelButton.AutoSize = true;
            _cancelButton.DialogResult = DialogResult.Cancel;
            _cancelButton.Name = "_cancelButton";
            _cancelButton.Text = "キャンセル";
            //
            // _navigationTimer
            //
            _navigationTimer.Interval = 30000;
            _navigationTimer.Tick += NavigationTimerOnTick;
            //
            // SharePointSignInForm
            //
            AutoScaleMode = AutoScaleMode.Dpi;
            CancelButton = _cancelButton;
            ControlBox = false;
            MinimumSize = new Size(860, 620);
            Name = "SharePointSignInForm";
            Size = new Size(1050, 760);
            StartPosition = FormStartPosition.CenterParent;
            Text = "Microsoft 365 サインイン（本人権限）";
            Controls.Add(_layout);
            _layout.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)_webView).EndInit();
            _footer.ResumeLayout(false);
            _footer.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private TableLayoutPanel _layout;
        private Microsoft.Web.WebView2.WinForms.WebView2 _webView;
        private TableLayoutPanel _footer;
        private Label _statusLabel;
        private Button _verifyButton;
        private Button _reloadButton;
        private Button _cancelButton;
        private System.Windows.Forms.Timer _navigationTimer;
    }
}
