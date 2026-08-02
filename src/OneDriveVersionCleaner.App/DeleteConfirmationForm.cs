using System.Text;
using OneDriveVersionCleaner.Core.Deletion;
using OneDriveVersionCleaner.Core.Formatting;

namespace OneDriveVersionCleaner.App;

/// <summary>
/// FR-DELETE-002: shows exactly what a run will move to the recycle bin before it starts.
/// </summary>
internal sealed class DeleteConfirmationForm : Form
{
    /// <param name="policyProtectedCount">
    /// How many of the planned versions the current retention policy would have kept. Manual
    /// selection deliberately overrides the policy, so this is surfaced rather than blocked.
    /// </param>
    public DeleteConfirmationForm(VersionDeletionPlan plan, int policyProtectedCount)
    {
        ArgumentNullException.ThrowIfNull(plan);

        Text = "ごみ箱へ移動する確認";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        MinimumSize = new Size(680, 520);
        Size = new Size(740, 600);
        AutoScaleMode = AutoScaleMode.Dpi;
        Padding = new Padding(14);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        layout.Controls.Add(BuildSummary(plan, policyProtectedCount), 0, 0);
        layout.Controls.Add(BuildNotes(), 0, 1);
        layout.Controls.Add(BuildTargetList(plan), 0, 2);
        layout.Controls.Add(BuildButtons(), 0, 3);
        Controls.Add(layout);
    }

    private static Control BuildSummary(VersionDeletionPlan plan, int policyProtectedCount)
    {
        var unknownSuffix = plan.UnknownSizeCount == 0
            ? ""
            : $"（サイズ不明 {plan.UnknownSizeCount:N0}件を除く）";
        var deferredLine = plan.DeferredVersionCount == 0
            ? ""
            : $"\r\n1回の上限を超えるため見送る過去版: {plan.DeferredVersionCount:N0}件";
        var protectedLine = policyProtectedCount == 0
            ? ""
            : $"\r\n現在の保持条件では残る対象を {policyProtectedCount:N0}件 含みます。";

        return new Label
        {
            AutoSize = true,
            Dock = DockStyle.Top,
            Font = new Font(DefaultFont, FontStyle.Bold),
            ForeColor = policyProtectedCount == 0 ? SystemColors.ControlText : Color.Firebrick,
            Text =
                $"過去バージョン {plan.VersionCount:N0}件（{plan.FileCount:N0}ファイル）をごみ箱へ移動します。\r\n" +
                $"削減見込み容量: {ByteSizeFormatter.Format(plan.KnownReclaimableBytes)}{unknownSuffix}" +
                deferredLine +
                protectedLine,
            Margin = new Padding(0, 0, 0, 12),
        };
    }

    private static Control BuildNotes() => new Label
    {
        AutoSize = true,
        Dock = DockStyle.Top,
        MaximumSize = new Size(660, 0),
        Text =
            "・ファイル本体と現行バージョンは対象になりません。\r\n" +
            "・移動したバージョンはOneDriveのごみ箱から復元できます。\r\n" +
            "・ごみ箱の項目も容量を消費します。容量を戻すにはごみ箱を空にしてください。\r\n" +
            "・保持ポリシーや訴訟ホールドの対象は、移動できない場合があります。\r\n" +
            "・移動の直後に、ローカルの現行ファイルが無事かを自動で確認します。",
        Margin = new Padding(0, 0, 0, 12),
    };

    private static Control BuildTargetList(VersionDeletionPlan plan)
    {
        var builder = new StringBuilder();
        foreach (var file in plan.Files)
        {
            builder.AppendLine(file.File.CloudPath);
            foreach (var version in file.Versions)
            {
                var created = version.Created.HasValue
                    ? version.Created.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss")
                    : "日時不明";
                builder.AppendLine(
                    $"　{version.Label ?? version.VersionId,-8} {created}  " +
                    $"{ByteSizeFormatter.Format(version.Size)}");
            }
        }

        var box = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Both,
            WordWrap = false,
            Font = new Font(FontFamily.GenericMonospace, DefaultFont.Size),
            BackColor = SystemColors.Window,
            Text = builder.ToString(),
        };

        var group = new GroupBox
        {
            Text = "移動するバージョン",
            Dock = DockStyle.Fill,
            Padding = new Padding(6),
            Margin = new Padding(0, 0, 0, 12),
        };
        group.Controls.Add(box);
        return group;
    }

    private Control BuildButtons()
    {
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
        };

        var cancelButton = new Button
        {
            Text = "キャンセル",
            AutoSize = true,
            DialogResult = DialogResult.Cancel,
        };
        var executeButton = new Button
        {
            Text = "ごみ箱へ移動",
            AutoSize = true,
            DialogResult = DialogResult.OK,
            ForeColor = Color.Firebrick,
        };

        panel.Controls.Add(cancelButton);
        panel.Controls.Add(executeButton);

        // Enter must not start the run; the user has to click the action deliberately.
        CancelButton = cancelButton;
        AcceptButton = cancelButton;
        return panel;
    }
}
