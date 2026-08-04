# OneDrive Version History Cleaner

**管理者権限なしで、自分の OneDrive for Business に溜まったバージョン履歴を整理する Windows デスクトップアプリです。**

> A Windows desktop app that lets an ordinary Microsoft 365 user reclaim OneDrive for Business
> storage by moving old file versions to the recycle bin — **no admin rights, no app
> registration, no Client ID**. It signs in with the user's normal browser session, ranks past
> versions by size, and moves the ones you pick to the recycle bin, where they stay
> restorable. English and Japanese UI.

![OneDrive Version History Cleaner の画面](https://raw.githubusercontent.com/seto77/OneDriveVersionCleaner/main/docs/screenshot.png)

<sub>起動直後の画面です。上段に過去バージョン、下段にファイル別の履歴容量が並びます。アカウント名とパスは差し替えてあります。</sub>

---

## 解決したい問題

職場や学校の Microsoft 365 では、OneDrive の容量が組織から割り当てられており、自分では増やせません。そして SharePoint のバージョン履歴は、ファイル形式によっては版ごとに**丸ごと**保存されます。1 GB のファイルを50回保存すれば、履歴だけで数十 GB が消えることがあります。

ところが、その掃除は簡単ではありません。

- エクスプローラーの右クリックからは**1ファイルずつ**しか操作できない
- どのファイルが容量を食っているかを**横断的に探せない**
- 管理センターの一括トリム機能は**管理者権限が必要**で、しかも**ごみ箱を経由せず完全削除**される
- 既存の PowerShell スクリプトは**全履歴を一括削除**するものが中心で、選んで消す用途に向かない

つまり「管理者ではない一般利用者が、容量の大きいものだけを選んで、安全に減らす」手段が抜け落ちています。このツールはそこを埋めます。

## できること

- 自分の OneDrive 配下を走査し、**過去バージョンをサイズ順に一覧表示**
- Shift / Ctrl で複数選択し、**まとめてごみ箱へ移動**
- 保持条件（世代数、期間、ファイルサイズ、履歴容量）で対象を絞り込み
- 分析結果を CSV / JSON に出力
- 削除の記録を JSON Lines で保存

## しないこと

安全のため、次の機能は**実装していません**。

- ファイル本体の削除
- 現行バージョンの削除
- 完全削除（ごみ箱を経由しない削除）
- 管理者権限を使った他ユーザーへの操作

## 特徴

### 管理者権限も、アプリ登録も要らない

Azure へのアプリ登録も Client ID も不要です。アプリ内の WebView2 で通常どおり Microsoft 365 にサインインし、**そのブラウザーセッションから同一オリジンで SharePoint REST を呼びます**。パスワードや MFA コードをアプリへ入力する画面はありません。

本ツールは OAuth のアプリ同意を一切使いません。そのため、同意画面そのものが出ません。

認証 Cookie をアプリのコードが読み出すことはありませんが、**WebView2 のプロファイルはディスクに残ります。** サインアウトしたい場合は `%LOCALAPPDATA%\OneDriveVersionCleaner\WebView2\` を削除してください（アプリの終了後に行ってください）。

### ごみ箱経由なので戻せる

移動先は OneDrive のごみ箱です。間違えても Web 画面から復元できます。管理センターのトリム機能が完全削除であるのと対照的です。

なお、ごみ箱の項目も容量を消費します。使用容量を実際に戻すには、後でごみ箱を空にしてください。

### 削除の前後でローカルファイルを検証

過去版の削除が現行ファイルに影響していないことを、実行のたびに自動で確かめます。ファイル単位で、移動の直前と直後にローカルの**存在・更新日時・サイズ**を記録して突き合わせ、いずれかが変化していたらその時点で残りの処理を中止します。ローカルを読み取れなかった場合も「安全」とは判定しません。

### 大規模なライブラリでも要求数を抑える

数万ファイル規模では、1ファイルずつ問い合わせると SharePoint Online の要求制限にかかります。**そこで、どのファイルを問い合わせるかをローカルだけで決めます。** サーバーにライブラリの一覧を出させることはしません。

| 段階 | 内容 | 通信 |
|---|---|---|
| 1 | ローカルの同期フォルダーを歩き、現行サイズが下限以上のファイルだけを候補にする | なし |
| 2 | 前回取得から日数が経ち、かつ更新されたものだけを残す | なし |
| 3 | サイズの大きい順に、1回の上限件数まで絞る（既定 1,000件） | なし |
| 4 | 残った件数を利用者に提示し、同意を得る（200件超のとき） | なし |
| 5 | 候補のファイル情報とバージョン一覧を取得 | 1件2要求 |

同時に投げる要求は2本までです。要求数は最初の1本を送る前に確定するので、走査を始める前に規模が分かります。

**1回の実行で問い合わせるファイル数には上限があります。既定は1,000件です。** 2,000件程度までは通りますが、10,000件規模を一度に問い合わせるとサーバーがエラーを返します。上限を超えた分は次回以降の実行に回ります。見送ったファイルには何も保存されないため、次の実行が改めて選び直します。**残りを取得するには、走査をもう一度実行してください。** 上限は画面の `問合わせ上限` で変更でき、0 で無制限になります。

絞り込みはサイズの大きい順です。削減量が大きいファイルから先に問い合わせるので、途中で要求制限にかかっても、重要なものは取得済みになります。

一度取得した履歴は保存され、次回以降は再利用します。要求制限にかかった場合は `Retry-After` に従って待機し、待機総量が上限を超えたら中断します。

## 前提

- Windows
- Microsoft Edge WebView2 ランタイム（Windows 11 には標準で入っています）
- OneDrive for Business（職場または学校アカウント）が同期済み
- ソースからビルドする場合のみ、.NET 10 SDK

同期ルートは Windows のレジストリから自動検出します。

## 入手して動かす

### 配布物を使う（インストール不要）

1. [Releases](https://github.com/seto77/OneDriveVersionCleaner/releases) から `OneDriveVersionCleaner-win-x64.zip` をダウンロードします
2. 好きな場所に展開します
3. `OneDriveVersionCleaner.exe` を実行します

**インストーラーはありません。** .NET ランタイムを同梱しているので、**利用者の環境に .NET が入っていなくても動きます。** レジストリも Program Files も触りません。

やめるときは展開したフォルダーごと削除してください。設定・キャッシュ・ログは `%LOCALAPPDATA%\OneDriveVersionCleaner\` に残るので、それも消す場合は併せて削除してください（[保存先](#保存先)）。

**exe に Authenticode 署名はありません。** 初回起動時に SmartScreen の警告が出ます。ダウンロードした zip の SHA-256 は、同じ Releases に置いた `.sha256` ファイルと照合できます。

```console
Get-FileHash .\OneDriveVersionCleaner-win-x64.zip -Algorithm SHA256
```

### ソースからビルドする

```console
git clone https://github.com/seto77/OneDriveVersionCleaner.git
cd OneDriveVersionCleaner
dotnet build
dotnet run --project src/OneDriveVersionCleaner.App
```

VS Code の場合はフォルダーを開いて `F5` でも起動します。

### 使う

1. `接続` で接続を確認します（要求1回だけ）
2. `対象フォルダー` を指定します（既定は同期ルート）
3. `履歴を取得` を押し、組織アカウントでサインインします
4. 上段の表で移動したいバージョンを選び、`選択をごみ箱へ移動` を押します

**最初は小さいテストフォルダーで試してください。** 段階を踏んだ手順は [RESTART_TEST_INSTRUCTIONS.md](https://github.com/seto77/OneDriveVersionCleaner/blob/main/RESTART_TEST_INSTRUCTIONS.md) にあります。

## 表示言語

日本語と英語に対応しています。初回はWindowsの表示言語に従い、日本語環境なら日本語、それ以外は英語で起動します。

メニューの `言語 / Language` からいつでも切り替えられます。**再起動は不要で、選択は次回以降も保持されます。** 保存先は `%LOCALAPPDATA%\OneDriveVersionCleaner\ui-language.json` です。

`appsettings.json` の `Language` に `"en"` または `"ja"` を書くと、まだ一度も選択していない利用者の初期言語を固定できます。一度メニューで選ぶと、そちらが優先されます。

日時と数値の書式は、表示言語ではなくWindowsの地域設定に従います。CSV / JSON 出力と削除ログは、表示言語にかかわらず英語の識別子で書き出します。

## 配布物を作る

```console
pwsh scripts/publish.ps1
```

テストを実行したうえで、ランタイムを同梱した単一 exe を作り、zip に固めて SHA-256 を書き出します。**SDK 以外に必要なものはありません**（圧縮は .NET の `ZipFile` で行います）。

実測で、展開後 49.7 MB / zip 44.3 MB です。単一 exe の中身を圧縮しているため、展開後のサイズが 112 MB から 50 MB に下がります。zip のサイズはどちらでも 44 MB 前後で変わらないので、利用者が手元に残すフォルダーが小さくなる分だけ得です。

Windows には .NET ランタイムがプリインストールされていないため、self-contained 以外の形式だと利用者に別途インストールを求めることになります。

zip は1つのフォルダーを含む形にしてあります。展開先にファイルが散らばりません。

```text
OneDriveVersionCleaner-win-x64/
├─ OneDriveVersionCleaner.exe
├─ appsettings.json
├─ README.md
├─ LICENSE.md
└─ THIRD-PARTY-NOTICES.md
```

[THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) を同梱するのは、配布物に .NET ランタイムが入っており、WebView2 の条項が再配布時の告知同梱を求めているためです。**再配布する場合も必ず同梱してください。**

## 条件

各項目は画面上で変更でき、マウスを乗せると説明が出ます。0 はその条件を無効にします。

**対象条件** — どのファイルをそもそも調べるか。

| 画面表示 | 既定値 | 意味 |
|---|---:|---|
| ファイルサイズが | 50 MB以上 | 現行サイズがこれ未満のファイルを丸ごと除外 |
| 総履歴サイズが | 200 MB以上 | 履歴の合計がこれ未満のファイルを丸ごと除外 |
| 問合わせ間隔 | 10日以上 | 前回取得からこの日数が経ち、かつ更新された場合だけ問い合わせ直す |
| 問合わせ上限 | 1,000件まで | 1回の実行で問い合わせるファイル数の上限。サイズの大きい順に選び、残りは次回以降 |

**除外条件** — 調べたファイルのうち、どの版を残すか。

| 画面表示 | 既定値 | 意味 |
|---|---:|---|
| 最近の履歴 | 0件 | 新しいものから数えて残す過去版の数 |
| 最終更新日から遡って | 0日以内 | ファイルの最終更新日時から遡って残す期間 |
| 更新日が現在から | 7日以内 | 現在時刻から見て最近更新されたファイルを丸ごと除外 |

保持数と保持期間の既定値が 0 なのは意図的です。**この2つは既定では働きません。** 残したい版がある場合は、実行前に自分で設定してください。現行版、日時不明、サイズ不明、API が削除不能とした版は、設定にかかわらず常に保持します。

保持数と保持期間は OR 条件です。どちらか一方でも満たす版は削除候補になりません。

保持期間は現在時刻ではなく**ファイル自身の最終更新日時**が基準です。更新が止まって久しいファイルでも、その最終更新の前後の履歴は残ります。

なお、表で選んだ版は保持条件より優先されます。保持条件が残すはずの版を選んだ場合は、確認ダイアログにその件数が出ます。

## 保存先

```text
%LOCALAPPDATA%\OneDriveVersionCleaner\
├─ Logs\delete-YYYYMMDD.jsonl    削除の記録
├─ Cache\versions-<account>.json 取得済みバージョン情報
├─ WebView2\                     サインイン状態
└─ ui-language.json              選択した表示言語
```

削除ログにはファイルパス、バージョンID、サイズ、API 応答、ローカルファイルの前後の状態に加えて、**実行したアプリのバージョン**が残ります。**トークン、Cookie、パスワードは記録しません。**

バージョンはタイトルバーにも出ます。不具合を報告するときは、その番号を書いてください。

ログは実行の**前に**開き、1件ごとにその場で書き出して flush します。途中でアプリが落ちても、そこまでに実行した分は残ります。最後の `runCompleted` 行が無いファイルは「最後まで到達しなかった実行」を意味します。

**ログを開けない場合、移動は開始しません。** 記録の残らない削除は行わない、という方針です。

## 設定ファイル

`appsettings.json` で既定値を変更できます。項目は [appsettings.example.json](appsettings.example.json) を参照してください。

## 構成

```text
src/
├─ OneDriveVersionCleaner.Core/            保持判定、集計、削除計画、キャッシュ、問合わせ上限、出力（UI非依存）
├─ OneDriveVersionCleaner.Infrastructure/  OneDriveアカウントの自動検出（レジストリ）
└─ OneDriveVersionCleaner.App/             WinForms UI、SharePoint RESTセッション、表示言語
tests/                                     単体テスト
```

保持判定と削除計画は Core に閉じており、UI や通信から独立して検証できます。

表示言語は `App/Localization/` にあります。`SupportedCultures`（対応言語の一覧と言語ごとのフォント）、`UiLanguage`（選択の保存と復元）、`Localization.Loc(en:, ja:)`（呼び出し側に訳を置くヘルパー）の3つです。訳す対象が画面の文言だけなので、UI と同じプロジェクトに置いています。

フォームは Visual Studio のデザイナーで編集するため、`.resx` はデザイナーの所有物です。そこへ手で書いた訳は次にデザイナーでフォームを保存した時点で消えるので、**デザイン面には英語を置き、実行時に訳を上書きします。** 詳しくは [DESIGNER_NOTES.md](https://github.com/seto77/OneDriveVersionCleaner/blob/main/DESIGNER_NOTES.md) にあります。

外部依存は `Microsoft.Web.WebView2` の1つだけです。

```console
dotnet test
```

開発に参加する場合は [CONTRIBUTING.md](https://github.com/seto77/OneDriveVersionCleaner/blob/main/CONTRIBUTING.md) を、脆弱性の報告は [SECURITY.md](https://github.com/seto77/OneDriveVersionCleaner/blob/main/SECURITY.md) を参照してください。

## 安全側の設計

- ファイル本体を削除するコードパスを持ちません
- 完全削除のコードパスを持ちません
- 現行バージョンは表に出さず、選択もできず、計画作成時にも例外で弾きます
- 削除の直前にファイルを読み直し、`UniqueId` が走査時と一致しなければ実行しません
- 走査で位置を取得したファイルだけが対象です
- 停止を押すと新しい要求の送信を止め、送信済みの要求は完了を待ちます
- 移動後にバージョン一覧を取り直し、消えたことを確認します
- 移動の前後でローカルの現行ファイルを照合し、変化していれば残りを中止します
- 判定できない情報（日時不明、サイズ不明、取得失敗）は常に保持側に倒します
- 削除はパスではなく `UniqueId` でファイルを指します。確認の後にリネームや置き換えが起きても、別のファイルへ要求が向かいません
- 記録を開けない場合は移動を開始しません。記録は1件ごとに即座に書き出します
- 実行中はウィンドウを閉じられません。停止を要求し、送信済みの要求が終わってから閉じます

## 既知の制限

- 自分の OneDrive のみが対象です。SharePoint チームサイト、Teams ライブラリ、共有ショートカットは対象外です
- 表示は上位5,000件までです。全件は CSV / JSON 出力に含まれます
- 中断からの再開はまだありません
- 対応言語は日本語と英語です

## 免責

このツールはファイルのバージョン履歴を削除します。作者は本ソフトウェアの使用によって生じたいかなる損害についても責任を負いません。**必ずテスト用フォルダーで動作を確認してから、実データに使用してください。**

## ライセンス

[MIT License](LICENSE.md) — Copyright (c) 2026 Y. Seto

依存する NuGet パッケージのライセンスは本ソフトウェアとは別です。とくに `Microsoft.Web.WebView2` は Microsoft 独自のライセンス条項が適用されます。ソースから各自でビルドする場合は、NuGet 取得時に利用者が直接その条項に同意します。**ビルド済みバイナリを再配布する場合は [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) を同梱してください。**
