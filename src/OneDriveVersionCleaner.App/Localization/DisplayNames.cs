using OneDriveVersionCleaner.Core.Models;
using OneDriveVersionCleaner.Core.Retention;
using static OneDriveVersionCleaner.App.Localization.Localization;

namespace OneDriveVersionCleaner.App.Localization;

/// <summary>
/// Turns the domain enumerations into text a reader can act on.
/// </summary>
/// <remarks>
/// The names themselves stay in the exports and in the audit log, where a stable identifier is
/// worth more than a readable one. Only the grids and the dialogs come through here.
/// </remarks>
internal static class DisplayNames
{
    public static string Of(RetentionDisposition disposition) => disposition switch
    {
        RetentionDisposition.Keep => Loc(en: "Keep", ja: "保持"),
        RetentionDisposition.DeleteCandidate => Loc(en: "Candidate", ja: "削除候補"),
        _ => disposition.ToString(),
    };

    public static string Of(RetentionReason reason) => reason switch
    {
        RetentionReason.CurrentVersion =>
            Loc(en: "current version", ja: "現行バージョン"),
        RetentionReason.FileRecentlyModified =>
            Loc(en: "file modified recently", ja: "最近更新されたファイル"),
        RetentionReason.FileLastModifiedTimeUnknown =>
            Loc(en: "file modification time unknown", ja: "ファイルの更新日時が不明"),
        RetentionReason.FileRetrievalFailed =>
            Loc(en: "file could not be read", ja: "ファイルを取得できなかった"),
        RetentionReason.HistoryBelowMinimumSize =>
            Loc(en: "history below the size threshold", ja: "履歴容量が下限未満"),
        RetentionReason.FileBelowMinimumSize =>
            Loc(en: "file below the size threshold", ja: "ファイルサイズが下限未満"),
        RetentionReason.FileSizeUnknown =>
            Loc(en: "file size unknown", ja: "ファイルサイズが不明"),
        RetentionReason.VersionCountBelowMinimum =>
            Loc(en: "too few versions", ja: "バージョン数が下限未満"),
        RetentionReason.VersionCreationTimeUnknown =>
            Loc(en: "version date unknown", ja: "バージョンの作成日時が不明"),
        RetentionReason.VersionSizeUnknown =>
            Loc(en: "version size unknown", ja: "バージョンのサイズが不明"),
        RetentionReason.InvalidVersionSize =>
            Loc(en: "version size invalid", ja: "バージョンのサイズが不正"),
        RetentionReason.VersionRetrievalFailed =>
            Loc(en: "version list could not be read", ja: "バージョン一覧を取得できなかった"),
        RetentionReason.ApiMarkedNotDeletable =>
            Loc(en: "reported as not deletable", ja: "削除不可と通知された"),
        RetentionReason.KeptByLatestCount =>
            Loc(en: "within the kept version count", ja: "保持する履歴数の範囲内"),
        RetentionReason.KeptByAge =>
            Loc(en: "within the kept period", ja: "保持する期間の範囲内"),
        RetentionReason.KeptByExplicitDate =>
            Loc(en: "after the configured cut-off date", ja: "指定日以降"),
        RetentionReason.EligibleForDeletion =>
            Loc(en: "no rule keeps it", ja: "保持条件に該当しない"),
        _ => reason.ToString(),
    };

    public static string Of(RetrievalStatus status) => status switch
    {
        RetrievalStatus.Success => Loc(en: "OK", ja: "正常"),
        RetrievalStatus.Warning => Loc(en: "Warning", ja: "警告"),
        RetrievalStatus.Excluded => Loc(en: "Excluded", ja: "対象外"),
        RetrievalStatus.AccessDenied => Loc(en: "No access", ja: "権限なし"),
        RetrievalStatus.NotFound => Loc(en: "Not found", ja: "見つからない"),
        RetrievalStatus.Error => Loc(en: "Error", ja: "エラー"),
        _ => status.ToString(),
    };

    public static string Of(DeleteResultStatus status) => status switch
    {
        DeleteResultStatus.Deleted => Loc(en: "Moved", ja: "移動済み"),
        DeleteResultStatus.AlreadyDeleted => Loc(en: "Already gone", ja: "既に削除済み"),
        DeleteResultStatus.NotDeletable => Loc(en: "Not deletable", ja: "削除不可"),
        DeleteResultStatus.AccessDenied => Loc(en: "No access", ja: "権限なし"),
        DeleteResultStatus.TransientError => Loc(en: "Temporary failure", ja: "一時的な失敗"),
        DeleteResultStatus.PermanentError => Loc(en: "Failed", ja: "失敗"),
        _ => status.ToString(),
    };
}
