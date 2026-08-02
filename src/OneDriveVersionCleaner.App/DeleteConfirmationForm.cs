using System.Text;
using OneDriveVersionCleaner.App.Localization;
using OneDriveVersionCleaner.Core.Deletion;
using static OneDriveVersionCleaner.App.Localization.Localization;

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
        ApplyLocalizedText();
        ApplyDerivedFonts();
        ApplyPlan(plan, policyProtectedCount);

        // Enter must not start the run; the user has to click the action deliberately. Both of
        // these stay here rather than in the designer: setting AcceptButton on the design
        // surface would silently point it at the execute button again.
        CancelButton = _cancelButton;
        AcceptButton = _cancelButton;
    }

    /// <summary>
    /// Puts the current interface language over the English the design surface carries. The
    /// dialog is built fresh for every run, so it never has to follow a language change.
    /// </summary>
    private void ApplyLocalizedText()
    {
        Text = Loc(en: "Confirm move to recycle bin", ja: "ごみ箱へ移動する確認");
        _targetGroup.Text = Loc(en: "Versions to move", ja: "移動するバージョン");
        _cancelButton.Text = Loc(en: "Cancel", ja: "キャンセル");
        _executeButton.Text = Loc(en: "Move to recycle bin", ja: "ごみ箱へ移動");
        _notesLabel.Text = Loc(
            en: "• The file itself and its current version are never touched.\r\n" +
                "• Moved versions can be restored from the OneDrive recycle bin.\r\n" +
                "• Recycle bin items still use quota. Empty it to get the space back.\r\n" +
                "• Versions under a retention policy or a legal hold may refuse to move.\r\n" +
                "• The local copy of the current file is checked right after the move.",
            ja: "・ファイル本体と現行バージョンは対象になりません。\r\n" +
                "・移動したバージョンはOneDriveのごみ箱から復元できます。\r\n" +
                "・ごみ箱の項目も容量を消費します。容量を戻すにはごみ箱を空にしてください。\r\n" +
                "・保持ポリシーや訴訟ホールドの対象は、移動できない場合があります。\r\n" +
                "・移動の直後に、ローカルの現行ファイルが無事かを自動で確認します。");
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
            : Loc(en: $" (excluding {plan.UnknownSizeCount:N0} of unknown size)",
                  ja: $"（サイズ不明 {plan.UnknownSizeCount:N0}件を除く）");
        var protectedLine = policyProtectedCount == 0
            ? ""
            : Loc(en: $"\r\n{policyProtectedCount:N0} of these would be kept by the current rules.",
                  ja: $"\r\n現在の保持条件では残る対象を {policyProtectedCount:N0}件 含みます。");

        return Loc(
                   en: $"Moving {plan.VersionCount:N0} past versions " +
                       $"({plan.FileCount:N0} files) to the recycle bin.\r\n" +
                       $"Space expected back: {SizeText.Of(plan.KnownReclaimableBytes)}",
                   ja: $"過去バージョン {plan.VersionCount:N0}件（{plan.FileCount:N0}ファイル）を" +
                       "ごみ箱へ移動します。\r\n" +
                       $"削減見込み容量: {SizeText.Of(plan.KnownReclaimableBytes)}") +
               unknownSuffix +
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
                    : Loc(en: "date unknown  ", ja: "日時不明　　　");
                builder.AppendLine(
                    $"　{version.Label ?? version.VersionId,-8} {created}  " +
                    $"{SizeText.Of(version.Size)}");
            }
        }

        return builder.ToString();
    }
}
