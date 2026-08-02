# OneDrive バージョン履歴整理ツール 仕様書

- 文書名: OneDrive Version History Cleaner
- 仮称: **OneDriveVersionCleaner**
- 文書版: 0.1
- 状態: MVP 要件定義案
- 作成日: 2026-08-01
- 対象OS: Windows
- 想定実装言語: C#
- 想定実行環境: 現行の .NET LTS
- 想定ユーザー: 自分の職場・学校用 OneDrive を利用する一般ユーザー

---

## 1. 目的

OneDrive 上のファイルについて、過去のバージョンが消費している容量を自動的に調査し、容量消費の大きいファイルを特定する。

ユーザーが指定した保持条件に基づき、不要な過去バージョンを安全に削除し、OneDrive の使用容量を削減する。

本ツールは、ユーザーがブラウザーまたはファイルエクスプローラーから手動で実行できる「バージョン履歴の削除」を、本人の権限内で効率化・自動化することを目的とする。

---

## 2. 背景

Windows のファイルエクスプローラーでは、OneDrive 配下のファイルを右クリックして、次の操作を実行できる。

```text
右クリック
  → バージョン履歴
  → 過去バージョンの削除
```

ただし、この操作は次の理由から大量のファイルには適さない。

- ファイルごとに操作する必要がある
- 容量消費の大きいバージョンを横断的に探せない
- 削除候補を一括して比較できない
- 数百から数千ファイルを手動確認するのは現実的でない
- 誤操作の危険がある

本ツールは、ローカルの同期フォルダーを直接操作するのではなく、Microsoft 365 に本人としてサインインし、OneDrive／SharePoint 側のバージョン情報を取得・削除する。

---

## 3. 基本方針

1. ユーザー本人の委任権限だけを使用する
2. 管理者権限を前提としない
3. 初期状態では削除を実行せず、分析のみ行う
4. 削除前に候補を一覧表示し、削減見込み容量を示す
5. 最新バージョンは削除しない
6. 一括削除前に Dry Run を実行できる
7. すべての処理結果をログに残す
8. 通信中断やスロットリングが発生しても再開できる
9. エクスプローラーの UI 自動操作は使用しない
10. API または SharePoint のクライアントライブラリを使用する

---

## 4. 対象範囲

### 4.1 MVPで対象とするもの

- Windows PC
- 職場または学校用 OneDrive
- ユーザー本人が所有または編集権限を持つファイル
- 自分の OneDrive 同期ルート配下
- 通常のファイルのメジャーバージョン
- ローカルパスまたはフォルダー選択による対象指定
- 過去バージョンの一覧取得
- バージョン容量の集計
- 削除候補の抽出
- 過去バージョンの個別削除および一括削除
- CSV／JSON／ログファイルへの出力

### 4.2 MVPでは対象外とするもの

- SharePoint チームサイト全体の管理
- 他ユーザーの OneDrive
- 管理者権限を用いたサイト全体の一括トリミング
- 保持ポリシー、訴訟ホールド、レコード管理の解除
- OneDrive のごみ箱の自動消去
- ファイル本体の削除
- 最新バージョンの削除
- バージョンの復元
- エクスプローラーの右クリックメニューへの統合
- Teams や SharePoint の共有ライブラリ
- OneDrive に追加された共有フォルダーのショートカット
- 複数テナントの同時処理

これらは将来拡張候補とする。

---

## 5. 用語

| 用語 | 定義 |
|---|---|
| 現行バージョン | 現在表示・同期されている最新のファイル |
| 過去バージョン | OneDrive／SharePoint に保存された旧版 |
| 履歴容量 | 過去バージョンのサイズを合計した推定値 |
| 削減見込み容量 | 削除候補となったバージョンサイズの合計 |
| Dry Run | 実際には削除せず、削除対象と結果見込みだけを確認する処理 |
| 保持数 | 新しいものから数えて必ず残すバージョン数 |
| 保持期間 | 指定日数以内に作成されたバージョンを残す期間 |
| 対象ルート | ユーザーが分析対象として指定したフォルダー |
| Drive Item | Microsoft 365 側でファイルまたはフォルダーを表す項目 |
| スキャン | 対象ファイルとバージョン情報を取得し、ローカルに集計する処理 |

---

## 6. 想定利用シナリオ

### 6.1 全体を分析する

1. ツールを起動する
2. Microsoft 365 にサインインする
3. OneDrive のローカル同期ルートを選択する
4. 「分析開始」を押す
5. ツールがクラウド側の全ファイルと過去バージョンを調査する
6. 履歴容量の大きい順に一覧表示する
7. ユーザーが削除方針を指定する
8. Dry Run を実行する
9. 削除候補と削減見込み容量を確認する
10. 明示的に削除を実行する

### 6.2 特定フォルダーだけ分析する

1. エクスプローラーから OneDrive 配下のフォルダーをツールへドラッグ＆ドロップする
2. ツールがローカルパスを OneDrive 上の相対パスへ変換する
3. 指定フォルダー以下だけをスキャンする
4. 結果を一覧表示する

### 6.3 容量消費の大きいファイルだけ整理する

1. 分析結果を履歴容量の降順で並べる
2. 履歴容量が指定値以上のファイルだけを抽出する
3. ファイルごとに保持数または保持期間を指定する
4. 削除候補を確認する
5. 選択したファイルだけ削除する

---

## 7. 機能要件

## 7.1 認証

### FR-AUTH-001

Microsoft 365 に対する対話的サインインを提供する。

### FR-AUTH-002

認証にはユーザー本人の委任アクセスを使用する。

### FR-AUTH-003

アプリケーション権限、クライアントシークレット、管理者用アカウントを要求しない。

### FR-AUTH-004

認証トークンは OS の安全な資格情報保存領域を利用してキャッシュする。

### FR-AUTH-005

サインアウトおよび別アカウントへの切り替え機能を提供する。

### FR-AUTH-006

認証または同意がテナントポリシーによって拒否された場合は、理由と対処方法を表示する。

---

## 7.2 対象フォルダーの指定

### FR-TARGET-001

次のいずれかで対象フォルダーを指定できる。

- フォルダー選択ダイアログ
- ドラッグ＆ドロップ
- コマンドライン引数
- 設定ファイル

### FR-TARGET-002

選択されたパスが OneDrive 同期ルート配下であることを確認する。

### FR-TARGET-003

ローカルパスを OneDrive 上の相対パスへ変換する。

例:

```text
ローカル:
C:\Users\UserName\OneDrive - Organization\Research\Data

クラウド相対パス:
Research/Data
```

### FR-TARGET-004

複数の OneDrive 同期アカウントが存在する場合は、対象アカウントを識別して選択できる。

### FR-TARGET-005

MVPでは、共有ライブラリ、Teams ライブラリ、共有ショートカットを検出した場合、対象外であることを表示する。

---

## 7.3 ファイル列挙

### FR-SCAN-001

対象フォルダー以下のファイルを再帰的に列挙する。

### FR-SCAN-002

フォルダーそのものはバージョン履歴の対象としない。

### FR-SCAN-003

各ファイルについて次の情報を取得する。

- Drive ID
- Drive Item ID
- ファイル名
- OneDrive 上のパス
- ローカルパス
- 現行ファイルサイズ
- 最終更新日時
- 更新者
- Web URL
- ファイル種別
- バージョン数
- 過去バージョン合計サイズ

### FR-SCAN-004

オンライン専用ファイルでも、ローカルへのダウンロードを行わずに調査できる。

### FR-SCAN-005

対象外ファイル、アクセス拒否、取得失敗を区別して記録する。

### FR-SCAN-006

大量ファイルを処理するため、ページングされた API 応答に対応する。

### FR-SCAN-007

通信制限が発生した場合は、サーバーが指示する待機時間に従って再試行する。

---

## 7.4 バージョン情報の取得

### FR-VERSION-001

各ファイルのバージョン履歴を取得する。

### FR-VERSION-002

各バージョンについて可能な範囲で次の情報を取得する。

- バージョンID
- バージョンラベル
- 作成日時
- 更新者
- サイズ
- 現行バージョンか否か
- 削除可能か否か
- 取得結果
- エラー内容

### FR-VERSION-003

サイズ情報を取得できないバージョンは「不明」として扱い、ゼロとはみなさない。

### FR-VERSION-004

現行バージョンは削除候補に含めない。

### FR-VERSION-005

バージョン情報取得失敗があっても、他のファイルの分析を継続する。

---

## 7.5 容量集計

### FR-AGG-001

ファイルごとに次の値を計算する。

- 現行ファイルサイズ
- 過去バージョン数
- 過去バージョン合計サイズ
- 全バージョン推定合計サイズ
- 削除候補数
- 削減見込み容量
- 最古バージョン日時
- 最新の過去バージョン日時

### FR-AGG-002

フォルダー単位でも履歴容量を集計する。

### FR-AGG-003

全体の次の統計を表示する。

- 対象ファイル数
- バージョンを持つファイル数
- 過去バージョン総数
- 過去バージョン推定総容量
- 削除候補総数
- 削減見込み総容量
- エラー数
- スキャン所要時間

### FR-AGG-004

表示される容量は推定値であり、OneDrive 管理画面の使用量と完全には一致しない可能性があることを明示する。

---

## 7.6 一覧表示

### FR-UI-001

ファイル一覧を表形式で表示する。

推奨列:

| 列 | 内容 |
|---|---|
| 選択 | 削除対象として選択 |
| ファイル名 | 対象ファイル名 |
| パス | OneDrive 上の相対パス |
| 現行サイズ | 現在のファイルサイズ |
| 履歴数 | 過去バージョン数 |
| 履歴容量 | 過去バージョンの合計 |
| 削除候補数 | 現在の条件で削除される数 |
| 削減見込み | 削除候補の合計サイズ |
| 最終更新 | 現行ファイルの更新日時 |
| 状態 | 正常、警告、エラーなど |

### FR-UI-002

次の列でソートできる。

- 履歴容量
- 削減見込み容量
- 履歴数
- 現行サイズ
- 最終更新日時
- パス
- ファイル名

### FR-UI-003

次の条件でフィルターできる。

- 履歴容量が指定値以上
- バージョン数が指定値以上
- 最終更新から指定日数以上
- ファイル拡張子
- フォルダーパス
- エラーの有無
- 削除候補の有無

### FR-UI-004

ファイルを選択すると、そのファイルのバージョン一覧を表示する。

### FR-UI-005

ファイルの Web 版 OneDrive ページを開く機能を提供する。

### FR-UI-006

ローカルファイルまたはローカルフォルダーをエクスプローラーで開く機能を提供する。

---

## 7.7 削除方針

削除条件は、全体設定とファイル個別設定を持つ。

### FR-POLICY-001

次の保持条件を指定できる。

- 最新 N 世代を保持
- 過去 N 日以内のバージョンを保持
- 指定日時以降のバージョンを保持
- 指定容量未満の履歴は処理しない
- 指定バージョン数未満のファイルは処理しない
- 最終更新から指定日数以内のファイルは処理しない

### FR-POLICY-002

保持数と保持期間が同時指定された場合は、いずれか一方の条件を満たすバージョンを保持する。

すなわち、安全側に判定する。

### FR-POLICY-003

既定値は次のとおりとする。

```text
最新バージョン: 必ず保持
最新の過去バージョン: 10世代を保持
保持期間: 30日
処理する最小ファイルサイズ: 50 MB
履歴容量しきい値: 10 MB
最終更新後の保護期間: 7日
1回の最大削除件数: 100
```

### FR-POLICY-004

ユーザーが「すべての過去バージョンを削除」を選択した場合でも、追加確認を要求する。

### FR-POLICY-005

保持条件の変更時には、削除候補数と削減見込み容量を即時再計算する。

---

## 7.8 Dry Run

### FR-DRYRUN-001

初期状態では Dry Run を有効にする。

### FR-DRYRUN-002

Dry Run では削除 API を呼び出さない。

### FR-DRYRUN-003

Dry Run の結果として次を表示・出力する。

- 削除対象ファイル数
- 削除対象バージョン数
- 削減見込み容量
- 保持されるバージョン数
- 削除不能と予測される項目
- エラーまたは警告

### FR-DRYRUN-004

Dry Run 結果を CSV および JSON で保存できる。

---

## 7.9 削除実行

### FR-DELETE-001

削除対象は、ユーザーが明示的に選択したファイルまたは現在のフィルター結果に限定する。

### FR-DELETE-002

削除開始前に、次の確認情報を表示する。

- 対象ファイル数
- 対象バージョン数
- 削減見込み容量
- 保持条件
- 復元できない可能性
- ごみ箱を経由しない可能性
- 保持ポリシー等により削除できない可能性

### FR-DELETE-003

削除実行には、確認用文字列の入力または二段階確認を要求する。

例:

```text
DELETE
```

### FR-DELETE-004

バージョンを新しい順または古い順のどちらで削除するかは、実装試験後に決定する。

既定案は古い順とする。

### FR-DELETE-005

ファイル本体を削除する API は実装しない。

### FR-DELETE-006

現行バージョンを削除する処理は実装しない。

### FR-DELETE-007

1回の実行で削除できる件数に上限を設ける。

### FR-DELETE-008

削除成功、削除済み、削除不能、アクセス拒否、一時エラー、恒久エラーを区別する。

### FR-DELETE-009

処理中に停止ボタンを押した場合、新しい削除要求の送信を停止し、進行中の要求だけを完了させる。

### FR-DELETE-010

削除後に対象ファイルのバージョン一覧を再取得し、結果を検証する。

---

## 7.10 ログとレポート

### FR-LOG-001

次のログを出力する。

- アプリ起動・終了
- サインイン・サインアウト
- 対象フォルダー
- スキャン開始・終了
- API 呼び出し失敗
- 再試行
- 削除対象
- 削除結果
- 中断・再開
- 設定値

### FR-LOG-002

ログにはアクセストークン、更新トークン、パスワードを記録しない。

### FR-LOG-003

削除履歴として最低限次を保存する。

- 実行日時
- アカウント識別子
- Drive ID
- Drive Item ID
- ファイルパス
- バージョンID
- バージョン日時
- バージョンサイズ
- API 応答結果
- エラー内容

### FR-LOG-004

分析結果を CSV に出力できる。

### FR-LOG-005

削除結果を CSV および JSON に出力できる。

### FR-LOG-006

ログの既定保存先は次とする。

```text
%LOCALAPPDATA%\OneDriveVersionCleaner\
```

---

## 7.11 中断と再開

### FR-RESUME-001

スキャン途中の状態をチェックポイントとして保存できる。

### FR-RESUME-002

削除途中の状態を保存し、再起動後に未処理分だけを再開できる。

### FR-RESUME-003

再開時には、対象バージョンが現在も存在するか再確認する。

### FR-RESUME-004

前回の分析結果が古い場合、削除前に再スキャンを要求する。

既定の有効期限案:

```text
24時間
```

---

## 8. 非機能要件

## 8.1 安全性

### NFR-SAFE-001

初回起動時は分析専用モードとする。

### NFR-SAFE-002

削除機能は設定画面で有効化するまで使用できないようにしてもよい。

### NFR-SAFE-003

ファイル本体を削除するコードパスを持たない。

### NFR-SAFE-004

削除処理の単体テストおよび統合テストを必須とする。

### NFR-SAFE-005

本番アカウントでの試験前に、専用テストフォルダーで検証する。

---

## 8.2 セキュリティ

### NFR-SEC-001

OAuth 2.0／OpenID Connect による対話認証を使用する。

### NFR-SEC-002

必要最小限の委任アクセス許可だけを要求する。

候補:

```text
Files.Read
Files.ReadWrite
offline_access
User.Read
```

実際の必要権限は、採用する API とテナント設定に基づき確定する。

### NFR-SEC-003

アプリケーションシークレットをローカルに保存しない。

### NFR-SEC-004

認証キャッシュは OS のユーザー単位で保護する。

### NFR-SEC-005

管理者権限でのアプリ実行を要求しない。

---

## 8.3 性能

### NFR-PERF-001

UI をブロックせず、非同期にスキャン・削除を実行する。

### NFR-PERF-002

同時 API 要求数は設定可能とし、既定では低い値に制限する。

既定案:

```text
2～4並列
```

### NFR-PERF-003

API スロットリングを考慮し、指数バックオフまたは Retry-After に従う。

### NFR-PERF-004

10,000ファイル以上でもメモリ使用量が過大にならないよう、逐次処理またはローカルデータベースを使用する。

### NFR-PERF-005

一覧表示は仮想化し、大量行でも操作可能にする。

---

## 8.4 信頼性

### NFR-REL-001

一時的なネットワーク障害では自動再試行する。

### NFR-REL-002

認証期限切れ時は再認証を促し、処理状態を保持する。

### NFR-REL-003

一部ファイルの失敗によって処理全体を中止しない。

### NFR-REL-004

削除要求は冪等性を考慮し、既に削除済みの場合は成功相当として扱う。

### NFR-REL-005

アプリ異常終了後も、最後に確定したチェックポイントから再開できる。

---

## 8.5 操作性

### NFR-UX-001

Windows の標準的なデスクトップアプリとして動作する。

### NFR-UX-002

容量は B、KB、MB、GB、TB を適切に切り替えて表示する。

### NFR-UX-003

日時はローカルタイムと UTC の混同がないよう、内部では UTC、表示ではローカル時間を使用する。

### NFR-UX-004

危険な操作は赤色表示、分析処理は通常色で区別する。

### NFR-UX-005

処理中は、現在のファイル、処理済み件数、全体件数、エラー数を表示する。

---

## 9. 画面仕様

## 9.1 メイン画面

構成案:

```text
┌──────────────────────────────────────────────────────────┐
│ アカウント: user@example.jp         [サインアウト]       │
├──────────────────────────────────────────────────────────┤
│ 対象: C:\Users\...\OneDrive - Organization\Research      │
│ [参照] [ドラッグ＆ドロップ] [分析開始] [停止]            │
├──────────────────────────────────────────────────────────┤
│ ファイル数  履歴数  履歴容量  削減見込み  エラー数       │
│ 12,340      85,120  420 GB    310 GB       12            │
├──────────────────────────────────────────────────────────┤
│ フィルター: 履歴容量 > [100] MB  保持 [10] 世代 [30]日   │
├──────────────────────────────────────────────────────────┤
│ □ ファイル名 | パス | 履歴数 | 履歴容量 | 削減見込み     │
│ □ data.zip    | ...  |  120   | 150 GB   | 145 GB        │
│ □ result.xlsx | ...  |   85   |  42 GB   |  38 GB        │
├──────────────────────────────────────────────────────────┤
│ [CSV出力] [Dry Run] [選択項目を削除]                     │
└──────────────────────────────────────────────────────────┘
```

---

## 9.2 バージョン詳細画面

表示項目:

- ファイル名
- クラウド上のパス
- ローカルパス
- 現行サイズ
- 履歴合計サイズ
- 最終更新日時
- Web で開く
- エクスプローラーで開く

バージョン一覧:

| 選択 | バージョン | 作成日時 | 更新者 | サイズ | 判定 | 理由 |
|---|---|---|---|---:|---|---|
|  | 現行 | 2026-08-01 | User | 1.2 GB | 保持 | 最新版 |
|  | 34.0 | 2026-07-30 | User | 1.1 GB | 保持 | 30日以内 |
| ✓ | 12.0 | 2025-10-01 | User | 1.0 GB | 削除 | 保持条件外 |

---

## 9.3 設定画面

設定項目:

- 認証方式
- テナント／アカウント
- 既定の対象フォルダー
- 保持バージョン数
- 保持日数
- 履歴容量しきい値
- 最終更新後の保護期間
- 1回の最大削除数
- 同時接続数
- 再試行回数
- ログ保存先
- 分析結果の保存先
- 削除機能の有効／無効
- 開発者向け詳細ログ

---

## 10. データモデル案

## 10.1 AccountInfo

```csharp
public sealed class AccountInfo
{
    public string TenantId { get; init; } = "";
    public string UserId { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public string UserPrincipalName { get; init; } = "";
}
```

## 10.2 DriveInfo

```csharp
public sealed class DriveInfo
{
    public string DriveId { get; init; } = "";
    public string DriveType { get; init; } = "";
    public string WebUrl { get; init; } = "";
    public string LocalRootPath { get; init; } = "";
}
```

## 10.3 FileEntry

```csharp
public sealed class FileEntry
{
    public string DriveId { get; init; } = "";
    public string ItemId { get; init; } = "";
    public string Name { get; init; } = "";
    public string CloudPath { get; init; } = "";
    public string LocalPath { get; init; } = "";
    public string WebUrl { get; init; } = "";
    public long? CurrentSize { get; init; }
    public DateTimeOffset? LastModified { get; init; }

    public List<VersionEntry> Versions { get; init; } = [];

    public int PreviousVersionCount =>
        Versions.Count(v => !v.IsCurrent);

    public long? PreviousVersionTotalSize =>
        Versions.Where(v => !v.IsCurrent && v.Size.HasValue)
                .Sum(v => v.Size);
}
```

## 10.4 VersionEntry

```csharp
public sealed class VersionEntry
{
    public string VersionId { get; init; } = "";
    public string? Label { get; init; }
    public DateTimeOffset? Created { get; init; }
    public string? ModifiedBy { get; init; }
    public long? Size { get; init; }
    public bool IsCurrent { get; init; }
    public bool IsDeleteCandidate { get; set; }
    public string DecisionReason { get; set; } = "";
}
```

## 10.5 RetentionPolicy

```csharp
public sealed class RetentionPolicy
{
    public int KeepLatestVersions { get; init; } = 10;
    public int KeepDays { get; init; } = 30;
    public int ProtectRecentlyModifiedFileDays { get; init; } = 7;
    public long MinimumHistoryBytes { get; init; } = 10L * 1024 * 1024;
    public int MaximumDeletesPerRun { get; init; } = 100;
}
```

---

## 11. 削除候補判定

基本判定例:

```csharp
IEnumerable<VersionEntry> SelectDeleteCandidates(
    FileEntry file,
    RetentionPolicy policy,
    DateTimeOffset now)
{
    if (file.LastModified is not null &&
        file.LastModified > now.AddDays(-policy.ProtectRecentlyModifiedFileDays))
    {
        return [];
    }

    var previousVersions = file.Versions
        .Where(v => !v.IsCurrent)
        .OrderByDescending(v => v.Created)
        .ToList();

    var keepByCount = previousVersions
        .Take(policy.KeepLatestVersions)
        .Select(v => v.VersionId)
        .ToHashSet();

    var keepAfter = now.AddDays(-policy.KeepDays);

    return previousVersions.Where(v =>
        !keepByCount.Contains(v.VersionId) &&
        v.Created < keepAfter);
}
```

実装時には次も考慮する。

- 作成日時が取得できないバージョンは既定で保持する
- サイズ不明のバージョンは既定で保持するか、警告付きで扱う
- バージョン一覧の並び順を信用せず、日時で明示的に並べる
- API が現行バージョンを履歴一覧に含めるかを実機検証する
- 同一日時または同一ラベルの扱いを検証する
- 削除可能性を事前には完全判定できない場合がある

---

## 12. システム構成案

```text
┌───────────────────────────────┐
│ Windows Desktop UI            │
│ WinForms または WPF           │
└──────────────┬────────────────┘
               │
┌──────────────▼────────────────┐
│ Application Service           │
│ Scan / Aggregate / Policy     │
│ Dry Run / Delete / Export     │
└───────┬───────────┬───────────┘
        │           │
┌───────▼──────┐ ┌──▼───────────┐
│ OneDrive API │ │ Local Storage │
│ Adapter      │ │ SQLite/JSON   │
└───────┬──────┘ └──────────────┘
        │
┌───────▼───────────────────────┐
│ Microsoft Graph /             │
│ SharePoint CSOM / REST         │
└───────────────────────────────┘
```

API 固有コードを `IOneDriveVersionService` の背後に隠し、Graph と SharePoint 系 API を差し替え可能にする。

```csharp
public interface IOneDriveVersionService
{
    Task<AccountInfo> SignInAsync(CancellationToken cancellationToken);

    IAsyncEnumerable<FileEntry> EnumerateFilesAsync(
        string cloudRelativePath,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<VersionEntry>> GetVersionsAsync(
        string driveId,
        string itemId,
        CancellationToken cancellationToken);

    Task<DeleteResult> DeleteVersionAsync(
        string driveId,
        string itemId,
        string versionId,
        CancellationToken cancellationToken);
}
```

---

## 13. API 選定方針

## 13.1 第一候補

Microsoft Graph を第一候補とする。

理由:

- Microsoft 365 の標準 API である
- 委任認証を利用できる
- ファイル列挙とバージョン取得を一つの API 系統で扱える
- 将来の保守性が比較的高い
- C# SDK を利用できる

## 13.2 第二候補

SharePoint CSOM または SharePoint REST を代替候補とする。

採用条件:

- Graph で旧バージョン削除が安定して実行できない
- バージョンサイズ等の必要情報が不足する
- 大学テナントの設定により Graph の必要権限が取得できない
- OneDrive for Business 固有の操作が必要になる

## 13.3 実装前の技術検証項目

1. 本人権限でバージョン一覧を取得できるか
2. バージョンサイズを取得できるか
3. 本人権限で過去バージョンを削除できるか
4. 最新バージョンを API 側で識別できるか
5. 削除後に OneDrive 使用容量が減少するか
6. 削除結果がエクスプローラーの「バージョン履歴」に反映されるか
7. 削除がごみ箱を経由するか
8. 保持ポリシー対象ファイルでどのようなエラーが返るか
9. ユーザーによる OAuth 同意が大学テナントで許可されるか
10. OneDrive Files On-Demand の状態に影響されないか

---

## 14. ローカルパスとクラウドパスの対応

### 14.1 MVPの方式

1. OneDrive 同期ルートを特定する
2. 選択されたローカルパスから同期ルートを除去する
3. 区切り文字を `/` に変換する
4. URL エンコードしてクラウド上の項目を検索する
5. 取得した Drive Item ID を以後の処理に使用する

### 14.2 注意事項

次のパスは単純変換できない場合がある。

- SharePoint ライブラリの同期
- Teams ライブラリ
- 「OneDrive にショートカットを追加」
- 共有フォルダー
- 名前に特殊文字を含む項目
- ローカルで名前が変換されている項目
- 複数の組織アカウント

MVPでは、自分の OneDrive ルート配下に限定する。

---

## 15. エラー処理

| 状況 | 処理 |
|---|---|
| サインイン失敗 | エラー内容を表示し、再試行を許可 |
| 同意拒否 | 必要権限と管理者への依頼内容を表示 |
| ネットワーク切断 | 状態保存後、自動再試行 |
| HTTP 429 | Retry-After に従い待機 |
| HTTP 401 | トークン更新または再サインイン |
| HTTP 403 | アクセス拒否として記録し続行 |
| HTTP 404 | 移動・削除済みとして再確認 |
| バージョン削除失敗 | 対象だけ失敗として記録 |
| 保持ポリシー | 削除不能理由を表示 |
| サイズ不明 | 容量集計から除外し警告 |
| アプリ異常終了 | チェックポイントから再開 |

---

## 16. 容量表示に関する注意

本ツールが集計する「履歴容量」は、API から取得できる各バージョンサイズの合計値である。

OneDrive 管理画面のストレージ使用量とは、次の理由で一致しない可能性がある。

- ストレージ集計の反映に時間差がある
- ごみ箱の項目が容量に含まれる
- 保持ライブラリや保持ポリシーが影響する
- バージョンサイズ情報が取得できない場合がある
- サーバー側での圧縮や差分保存の扱い
- SharePoint 側の計算規則
- 削除済み項目の保持期間

したがって、削減見込み容量は目安として表示する。

---

## 17. 設定ファイル案

```json
{
  "TargetPath": "C:\\Users\\UserName\\OneDrive - Organization",
  "KeepLatestVersions": 10,
  "KeepDays": 30,
  "ProtectRecentlyModifiedFileDays": 7,
  "MinimumHistorySizeMB": 10,
  "MinimumFileSizeMB": 50,
  "MaximumDeletesPerRun": 100,
  "MaxConcurrency": 3,
  "RetryCount": 5,
  "DryRun": true,
  "EnableDelete": false,
  "ExportDirectory": "%LOCALAPPDATA%\\OneDriveVersionCleaner\\Reports"
}
```

---

## 18. コマンドライン案

GUI に加えて、将来的に次のコマンドラインを提供する。

```text
OneDriveVersionCleaner scan
    --path "C:\Users\User\OneDrive - Organization\Research"
    --output report.csv

OneDriveVersionCleaner plan
    --input report.json
    --keep-versions 10
    --keep-days 30
    --min-history-size 100MB

OneDriveVersionCleaner delete
    --plan delete-plan.json
    --max-delete 100
    --confirm DELETE
```

MVPでは GUI のみでもよい。

---

## 19. テスト要件

## 19.1 単体テスト

- 保持数による候補判定
- 保持日数による候補判定
- 保持数と保持日数の複合条件
- 最新バージョン除外
- サイズ不明の扱い
- 日時不明の扱い
- ローカルパスからクラウドパスへの変換
- 容量集計
- 最大削除件数
- 再試行判定

## 19.2 統合テスト

専用テストフォルダーを作成し、次を検証する。

- Word／Excel／PowerPoint
- 大容量 ZIP
- テキストファイル
- ファイル名に日本語を含む
- ファイル名に空白を含む
- ファイル名に `#`、`%`、`+` 等を含む
- 100世代以上の履歴
- オンライン専用ファイル
- 同期保留中ファイル
- 読み取り専用ファイル
- 共有されたファイル
- 保持ポリシー対象ファイル
- 削除途中のネットワーク切断
- HTTP 429
- アプリ強制終了後の再開

## 19.3 受け入れ基準

MVPは次を満たした時点で完成とする。

1. 本人の大学アカウントでサインインできる
2. 自分の OneDrive の指定フォルダー以下を列挙できる
3. 各ファイルのバージョン数とサイズを取得できる
4. 履歴容量順に一覧表示できる
5. CSV に分析結果を出力できる
6. Dry Run で削除候補を確認できる
7. 指定した過去バージョンだけを削除できる
8. 最新バージョンおよびファイル本体を削除しない
9. 削除結果をログで確認できる
10. 通信エラー後に処理を再開できる

---

## 20. 開発フェーズ案

## Phase 0: API 技術検証

コンソールアプリで次だけを確認する。

1. サインイン
2. 自分の OneDrive 情報取得
3. 指定ファイルの Drive Item ID 取得
4. バージョン一覧取得
5. バージョンサイズ取得
6. テスト用過去バージョン1件の削除
7. 削除後の再取得

成果物:

```text
OneDriveVersionProbe.exe
```

## Phase 1: 分析専用 MVP

- フォルダー選択
- 全ファイル列挙
- バージョン一覧取得
- 容量集計
- ソート・フィルター
- CSV 出力
- 削除機能なし

## Phase 2: Dry Run と削除

- 保持方針
- 削除候補作成
- Dry Run
- 個別削除
- 一括削除
- ログ
- 最大削除件数

## Phase 3: 信頼性向上

- チェックポイント
- 中断・再開
- スロットリング対応
- SQLite キャッシュ
- 大規模フォルダー対応
- エラー再処理

## Phase 4: 操作性向上

- ドラッグ＆ドロップ
- エクスプローラーから開く
- Web 版で開く
- フォルダー別集計
- グラフ表示
- 設定プリセット

## Phase 5: 将来拡張

- エクスプローラー右クリック統合
- SharePoint ライブラリ対応
- Teams ライブラリ対応
- 複数アカウント対応
- 定期実行
- 自動通知
- 管理者向けモード

---

## 21. 未確定事項

以下は Phase 0 の技術検証後に確定する。

1. Microsoft Graph 単独で全機能を実現できるか
2. SharePoint CSOM を併用する必要があるか
3. 大学テナントでユーザー同意が可能か
4. 必要な最小 OAuth 権限
5. バージョンサイズの取得精度
6. 削除されたバージョンがごみ箱を経由するか
7. 削除後に容量へ反映されるまでの時間
8. 共有ファイルの扱い
9. マイナーバージョンの扱い
10. Windows UI を WinForms と WPF のどちらで実装するか
11. ローカルキャッシュに SQLite を使うか
12. バージョン削除の最適な並列数
13. エクスプローラー右クリック統合を行うか

---

## 22. 推奨する初期実装

初期実装は次の構成を推奨する。

```text
言語:
  C#

実行環境:
  現行 .NET LTS

UI:
  WinForms または WPF
  初期検証ではコンソールでも可

認証:
  MSAL

クラウドアクセス:
  Microsoft Graph を第一候補
  SharePoint CSOM／REST を代替候補

ローカル保存:
  JSON
  大規模化後は SQLite

ログ:
  構造化ログ
  日次ローテーション

配布:
  x64 の self-contained 単一フォルダー配布
```

最初から完成版 GUI を作るのではなく、Phase 0 のコンソールプローブで API の実現可能性を確認してから、分析専用 GUI に進む。

---

## 23. 安全上の最終原則

本ツールでは、次の原則を変更しない。

```text
1. ファイル本体は削除しない
2. 最新バージョンは削除しない
3. 初期状態では削除しない
4. 削除前に必ず候補を表示する
5. 削除件数に上限を設ける
6. すべての削除結果を記録する
7. 取得不能・判定不能なバージョンは原則として保持する
8. 削除後に結果を再確認する
```

---

## 24. MVPの要約

MVP は、次の一連の処理を実現する Windows デスクトップアプリとする。

```text
OneDrive フォルダーを指定
        ↓
本人として Microsoft 365 にサインイン
        ↓
クラウド側のファイルとバージョンを取得
        ↓
履歴容量の大きい順に一覧表示
        ↓
保持数・保持期間を指定
        ↓
Dry Run で削除候補を確認
        ↓
明示的な確認後に過去バージョンを削除
        ↓
削除結果を検証し、CSV／ログに保存
```
