using System.Text;
using OneDriveVersionCleaner.Core.Deletion;
using OneDriveVersionCleaner.Core.Formatting;

namespace OneDriveVersionCleaner.App;

/// <summary>
/// FR-DELETE-002: shows exactly what a run will move to the recycle bin before it starts.
/// </summary>
internal sealed partial class DeleteConfirmationForm : Form
{
    /// <summary>Designer only. Must never do anything a running instance would do.</summary>
    private DeleteConfirmationForm() => InitializeComponent();

    /// <param name="policyProtectedCount">
    /// How many of the planned versions the current retention policy would have kept. Manual
    /// selection deliberately overrides the policy, so this is surfaced rather than blocked.
    /// </param>
    public DeleteConfirmationForm(VersionDeletionPlan plan, int policyProtectedCount)
    {
        ArgumentNullException.ThrowIfNull(plan);

        InitializeComponent();
        ApplyDerivedFonts();
        ApplyPlan(plan, policyProtectedCount);

        // Enter must not start the run; the user has to click the action deliberately. Both of
        // these stay here rather than in the designer: setting AcceptButton on the design
        // surface would silently point it at the execute button again.
        CancelButton = _cancelButton;
        AcceptButton = _cancelButton;
    }

    /// <summary>
    /// Fonts derived from the ambient default cannot live on the design surface: the designer
    /// would freeze one machine's font family and size into the layout.
    /// </summary>
    private void ApplyDerivedFonts()
    {
        _summaryLabel.Font = new Font(_summaryLabel.Font, FontStyle.Bold);
        _targetBox.Font = new Font(FontFamily.GenericMonospace, _targetBox.Font.Size);
    }

    private void ApplyPlan(VersionDeletionPlan plan, int policyProtectedCount)
    {
        _summaryLabel.ForeColor = policyProtectedCount == 0
            ? SystemColors.ControlText
            : Color.Firebrick;
        _summaryLabel.Text = BuildSummaryText(plan, policyProtectedCount);
        _targetBox.Text = BuildTargetList(plan);
    }

    private static string BuildSummaryText(VersionDeletionPlan plan, int policyProtectedCount)
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

        return
            $"過去バージョン {plan.VersionCount:N0}件（{plan.FileCount:N0}ファイル）をごみ箱へ移動します。\r\n" +
            $"削減見込み容量: {ByteSizeFormatter.Format(plan.KnownReclaimableBytes)}{unknownSuffix}" +
            deferredLine +
            protectedLine;
    }

    private static string BuildTargetList(VersionDeletionPlan plan)
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

        return builder.ToString();
    }
}
