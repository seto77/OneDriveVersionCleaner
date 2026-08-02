using System.Diagnostics;
using System.Runtime.InteropServices;
using OneDriveVersionCleaner.Infrastructure.Authentication;

namespace OneDriveVersionCleaner.App;

internal sealed class DeviceCodeForm : Form
{
    private readonly DeviceCodePrompt _prompt;

    public DeviceCodeForm(DeviceCodePrompt prompt)
    {
        _prompt = prompt;
        Text = "Microsoft 365 サインイン";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Padding = new Padding(16);

        var layout = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 4,
            Dock = DockStyle.Fill,
        };

        var message = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(520, 0),
            Text = "ブラウザーで Microsoft 365 にサインインし、次のコードを入力してください。",
            Margin = new Padding(0, 0, 0, 10),
        };
        var code = new TextBox
        {
            ReadOnly = true,
            Text = prompt.UserCode,
            Font = new Font(Font.FontFamily, 16, FontStyle.Bold),
            TextAlign = HorizontalAlignment.Center,
            Width = 300,
            Margin = new Padding(0, 0, 0, 10),
        };
        var expiry = new Label
        {
            AutoSize = true,
            Text = $"有効期限: {prompt.ExpiresOn.ToLocalTime():yyyy-MM-dd HH:mm:ss}",
            Margin = new Padding(0, 0, 0, 12),
        };
        var buttons = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = Padding.Empty,
        };
        var copyButton = new Button { Text = "コードをコピー", AutoSize = true };
        copyButton.Click += (_, _) => CopyCode();
        var browserButton = new Button { Text = "ブラウザーを開く", AutoSize = true };
        browserButton.Click += (_, _) => OpenBrowser();
        var closeButton = new Button { Text = "閉じる", AutoSize = true };
        closeButton.Click += (_, _) => Close();
        buttons.Controls.AddRange([copyButton, browserButton, closeButton]);

        layout.Controls.Add(message, 0, 0);
        layout.Controls.Add(code, 0, 1);
        layout.Controls.Add(expiry, 0, 2);
        layout.Controls.Add(buttons, 0, 3);
        Controls.Add(layout);

        Shown += (_, _) => OpenBrowser();
    }

    private void CopyCode()
    {
        try
        {
            Clipboard.SetText(_prompt.UserCode);
        }
        catch (Exception exception) when (exception is ExternalException or ThreadStateException)
        {
            MessageBox.Show(this, exception.Message, "コピーできませんでした", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void OpenBrowser()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = _prompt.VerificationUrl,
                UseShellExecute = true,
            });
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            MessageBox.Show(
                this,
                $"ブラウザーを開けませんでした。次のURLを手動で開いてください。\n{_prompt.VerificationUrl}\n\n{exception.Message}",
                "ブラウザーを開けませんでした",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }
}
