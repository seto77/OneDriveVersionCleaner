# セキュリティ / Security

## 対象バージョン / Supported versions

最新のリリースだけを対象にしています。古い zip を使っている場合は、まず最新に入れ替えてください。

Only the latest release is supported. If you are running an older zip, please update first.

## 報告の方法 / Reporting

**公開 Issue には書かないでください。** このリポジトリの `Security` タブ → `Report a vulnerability` から非公開で報告してください。

**Please do not open a public issue.** Use `Security` → `Report a vulnerability` in this repository to report privately.

個人が余暇で保守しているソフトウェアです。返信まで数日かかることがあります。

This is maintained by one person in their spare time; a reply can take a few days.

## 報告に含めないもの / Never include

このツールが扱うログ・出力・キャッシュには、次が入ります。**そのまま貼らないでください。**

クラウド上とローカルのファイルパス / ファイル名 / SharePoint サイト名 / テナント名 / アカウント名 / Drive ID / Item ID / Version ID / API の応答本文。

**認証情報、Cookie、トークン、パスワード、MFA コードは、いかなる場合も貼らないでください。**

The logs, exports and cache this tool writes contain cloud and local file paths, file names, the
SharePoint site and tenant name, the account name, drive/item/version ids, and raw API responses.
Quote only the lines you need, with names replaced. **Never paste credentials, cookies, tokens,
passwords or MFA codes.**

## とくに知りたいこと / What matters most

このツールはファイルのバージョン履歴を削除します。次に当たるものは、他より重く扱います。

This tool deletes file version history. Reports in these areas are treated as more serious than
the rest:

- 現行ファイルや、選択していないバージョンが失われる経路 / anything that could destroy a current file or a version the user did not select
- ごみ箱を経由せず完全削除になる経路 / anything that could bypass the recycle bin
- 削除の記録（`%LOCALAPPDATA%\OneDriveVersionCleaner\Logs\`）が残らないまま削除が進む経路 / any path where versions are recycled without the log being written
- 認証情報・Cookie・トークンがディスクやログへ漏れる経路 / any leak of credentials, cookies or tokens to disk or logs

## 設計上の前提 / By design

アプリはパスワードも MFA コードも受け取りません。サインインは WebView2 の中で Microsoft 365 の画面が行い、アプリはそのセッションから同一オリジンで SharePoint REST を呼びます。

The app never receives a password or MFA code. Sign-in happens inside WebView2 on Microsoft's own
pages, and the app calls SharePoint REST same-origin from that session.

**WebView2 のプロファイルはディスクに残ります。** サインアウトしたい場合は、アプリの終了後に
`%LOCALAPPDATA%\OneDriveVersionCleaner\WebView2\` を削除してください。共用 PC ではとくに注意して
ください。

**The WebView2 profile stays on disk.** To sign out, delete
`%LOCALAPPDATA%\OneDriveVersionCleaner\WebView2\` after closing the app. This matters on a shared PC.

配布する exe に Authenticode 署名はありません。**リリース以外の場所から入手した exe は使わないで
ください。** zip の SHA-256 は Releases に併記しています。

The released exe is not Authenticode signed. **Do not run a copy obtained from anywhere other than
this repository's Releases.** The SHA-256 of each zip is published alongside it.
