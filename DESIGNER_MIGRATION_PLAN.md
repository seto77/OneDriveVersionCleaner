# WinForms デザイナー対応 移行指示書

作成日: 2026-08-02

## この文書について

現在このアプリの UI は、3つのフォームすべてを C# コードで組み立てています。`.Designer.cs` も `.resx` も存在しません。そのため Visual Studio の WinForms ビジュアルデザイナーが使えません。

本書は、デザイナーで編集できる構造へ移行するための指示書です。**別セッションでの実装を想定**しており、前提知識なしで着手できるように書いています。

移行方針は外部レビュー（Codex CLI, `gpt-5.6-sol`）を経ています。その指摘は本書に反映済みです。

## 環境の前提

- **Visual Studio 2026 が必要です。** VS2022 は .NET 10 に対応せず、今後も対応しません
- .NET 10 SDK（`global.json` で 10.0.302 を指定）
- 対象は Windows のみ（`net10.0-windows`）

```console
dotnet build   # 警告0・エラー0を維持すること
dotnet test    # 現在151件、すべて成功
dotnet run --project src/OneDriveVersionCleaner.App
```

`Directory.Build.props` で `TreatWarningsAsErrors` が有効です。警告は1件も許容されません。

## ゴール

1. `MainForm` と `DeleteConfirmationForm` をデザイナーで開いて編集できる
2. 既存の動作を1つも変えない
3. ビルド警告0、テスト151件成功を維持する

## 大原則

移行の判断基準は次の1行に集約されます。迷ったらこれに戻ってください。

> **`.Designer.cs` は静的なコンポーネント配置図だけを持つ。振る舞いと、設計時に存在しない状態は、通常の `.cs` が持つ。**

`.Designer.cs` は**生成物**として扱ってください。手で維持する2つ目の実装ファイルではありません。デザイナーが読み書きできない書き方を混ぜた時点で、次にデザイナーで触った瞬間に壊れます。

### `.Designer.cs` に入れるもの

- コントロールとコンポーネントの生成
- 定数のプロパティ（Text、Size、Margin、Padding、Anchor、Dock、色、列挙値）
- TableLayoutPanel / FlowLayoutPanel の行・列・スパン・所属
- DataGridView の列定義（`ValueType` を除く）
- メニュー項目の構造
- ToolTip の個別文字列（`SetToolTip` を1行ずつ）
- 名前付きイベントハンドラーの購読
- `components` の Dispose

### 通常の `.cs` に残すもの

- 設定読み込み、アカウント検出、ファイルシステム、ネットワーク
- データに依存する文字列、Enabled / Visible
- グリッドの行と `Tag`
- **`ValueType` の代入**（後述）
- 分岐、ループ、LINQ、ラムダ、クロージャ、非同期初期化
- メニューを開くかどうかの判断
- SplitContainer の分割位置の確定
- WebView2 の初期化とナビゲーション
- `VersionDeletionPlan` から作るダイアログ内容

### コンストラクターの順序

```csharp
public MainForm(AppSettings settings, string? settingsWarning)
{
    _settings = settings;                 // 1. 依存の保存
    _settingsWarning = settingsWarning;
    InitializeComponent();                // 2. デザイナー生成の配置
    InitializeGridSchema();               // 3. ValueType など設計時に持てない情報
    ApplySettings();                      // 4. 設定・データの適用
    WireRuntimeEvents();                  // 5. デザイナーが持たないイベント
    RefreshDeleteState();                 // 6. 派生状態の更新
}
```

**イベント購読の所有者は必ず片方に決めてください。** デザイナーとコードの両方で購読すると二重に発火します。

### デザイナー用の引数なしコンストラクター

デザイン環境は引数付きコンストラクターを呼べません。次を追加してください。

```csharp
/// <summary>デザイナー専用。実行時の処理は一切行わないこと。</summary>
private MainForm() => InitializeComponent();
```

このコンストラクターから、アカウント検出や設定読み込みを**絶対に呼ばないでください**。

### `.resx` を拒否しない

Visual Studio がフォームの `.resx` を作ったら、そのまま受け入れてください。アイコンや画像を扱う際に必要になります。

## 作業順序

リスクの小さい順です。**この順を守ってください。**

### 段階1: DeleteConfirmationForm（小・独立）

162行、他から独立しており、失敗しても影響が閉じています。ここで手順を確立してください。

1. 固定の4行シェル（サマリー / 注意書き / 対象一覧 / ボタン）をデザイナーで作る
2. `BuildSummary` `BuildNotes` `BuildTargetList` `BuildButtons` を廃止する
3. `VersionDeletionPlan` から作る文字列は、`InitializeComponent()` の後に代入する

**絶対に失ってはいけない挙動**があります。

```csharp
CancelButton = cancelButton;
AcceptButton = cancelButton;   // 意図的に両方キャンセル
```

Enter キーで削除が始まらないようにするための措置です。デザイナーで `AcceptButton` を設定し直すと消えます。

### 段階2: MainForm（大・本命）

1,816行。静的な配置図をデザイナーで**作り直します**。既存の `BuildXxx` メソッドを `.Designer.cs` へ移すのではありません。

対象は `BuildInterface` から呼ばれる次の一式です。

- `BuildConnectionGroup` `BuildPolicyGroup` `BuildSummaryPanel` `BuildGridArea` `BuildStatusPanel`
- ヘルパー `AddPolicyControl` `AddLabeledRow` `CreateTextColumn` `WrapGrid` `ConfigureGrid` `ConfigureNumber` `ConfigureTextBox`

すべて名前付きフィールドとして展開してください。現在は無名で生成しているラベル類（見出しなど）にも名前を付けます。

**ビルダーは、画面の見た目と動作を比較して一致を確認してから削除してください。**

### 段階3: イベントとライフサイクルの安定化

段階2の直後に、後述のチェックリストを1つずつ確認します。

### 4段階: SharePointWebSession の分割（別コミット）

1,274行のうち UI は先頭160行程度で、残りは REST・再試行・スロットリング・ダイジェストの処理です。**Form と セッションが同居しています。**

推奨する分割は次のとおりです。

- `SharePointSignInForm` — WebView2、状態ラベル、3ボタン、フッター、タイマーを持つデザイナー対応 Form
- `SharePointWebSession` — Form を継承しない、REST とセッション管理のクラス

**この段階は他と同じコミットに混ぜないでください。** ライフサイクルの変更を伴い、リスクが最も高い部分です。

とくに次に注意してください。**サインイン用の Form と WebView2 は、セッションが生きている間ずっと生存させる必要があります。** `ShowDialog` が OK を返した後も、アプリはその WebView の Cookie を使って同一オリジンの要求を出し続けます。ダイアログを閉じた時点で Dispose すると、認証済みセッションを失います。

## 個別の注意事項

### DataGridView の列

列そのものはデザイナー対応です。`Name` `HeaderText` `Width` `AutoSizeMode` `Resizable` `SortMode` はデザイナーに置いて構いません。

**`ValueType` だけは別扱いです。** `DataGridViewColumn.ValueType` は `Browsable(false)` のため、プロパティグリッドから編集できず、手で書いた代入がデザイナーの再生成で保持される保証がありません。`InitializeComponent()` の直後に、専用メソッドで代入してください。

```csharp
private void InitializeGridSchema()
{
    _versionsGrid.Columns["Created"]!.ValueType = typeof(DateTime);
    _versionsGrid.Columns["VersionSize"]!.ValueType = typeof(long);
    // ...
}
```

`ValueType` を失うと、**表示は正常なのに並べ替えだけが壊れます。** 発見が遅れる典型例です。

以下が現在の定義です。**順序・名前・型を1つも変えないでください。**

上段（過去バージョン、`_versionsGrid`）

| # | Name | HeaderText | Width | ValueType |
|---:|---|---|---:|---|
| 1 | FileName | ファイル名 | 180 | string |
| 2 | CloudPath | パス | 320 | string |
| 3 | VersionLabel | バージョン | 90 | string |
| 4 | Created | 作成日時 | 150 | **DateTime** |
| 5 | ModifiedBy | 更新者 | 130 | string |
| 6 | VersionSize | サイズ | 105 | **long** |
| 7 | FileHistorySize | ファイル履歴計 | 115 | **long** |
| 8 | Decision | 判定 | 110 | string |
| 9 | Reasons | 理由 | 260 | string |

下段（ファイル別、`_filesGrid`）

| # | Name | HeaderText | Width | ValueType |
|---:|---|---|---:|---|
| 1 | FileName | ファイル名 | 180 | string |
| 2 | CloudPath | パス | 320 | string |
| 3 | CurrentSize | 現行サイズ | 100 | **long** |
| 4 | HistoryCount | 履歴数 | 75 | **int** |
| 5 | HistorySize | 履歴容量 | 115 | **long** |
| 6 | CandidateCount | 候補数 | 75 | **int** |
| 7 | Reclaimable | 削減見込み | 115 | **long** |
| 8 | LastModified | 最終更新 | 140 | **DateTime** |
| 9 | Status | 状態 | 90 | string |

順序が重要な理由は `CreateCells` が位置で値を渡しているため、名前が重要な理由は `CellFormatting` が `Name` で分岐しているためです。

全列で `AutoSizeMode = None`、`Resizable = True`、`SortMode = Automatic` です。**`Fill` を復活させないでください。** Fill 列は幅を変更してもすぐ戻るため、「一部の列だけ幅を変えられない」という不具合として報告された経緯があります。

### SplitContainer

`SplitterDistance` `Panel1MinSize` `Panel2MinSize` をデザイナーの初期化時に確定させないでください。生成直後のサイズと矛盾すると `ArgumentOutOfRangeException` を投げます。`SuspendLayout()` は検証を止めないので回避策になりません。

安全な手順は次のとおりです。

1. デザイナーには階層、`Dock`、`Orientation`、無害な仮の分割位置だけを置く
2. 最小サイズは初期化時に触らない
3. 最初の実レイアウト後（`OnShown` など）に、`SplitContainer.ClientSize` を基準として計算する
4. `Panel1MinSize + Panel2MinSize` が収まることを確認してから適用する
5. 分割位置を `Panel1MinSize ～ 利用可能量 - Panel2MinSize` に丸める

フォームの `MinimumSize` は根拠になりません。上部の AutoSize 領域が DPI や文字の折り返しで伸びるためです。

### ToolTip

現在はフィールド初期化子で持っていますが、`components` コンテナに登録されるデザイナーコンポーネントに変更してください。生成される `Dispose(bool)` が破棄を引き受けます。

`AddPolicyControl` 内の `foreach` によるツールチップ設定は、**ラベル・入力・単位ラベルそれぞれへの個別の `SetToolTip` 呼び出し**に展開してください。

保持条件は7項目あります（`MainForm.cs:227-256`）。説明文は現在の文言をそのまま引き継いでください。利用者からの指摘を受けて調整した経緯があり、意味が変わると混乱します。

### ContextMenuStrip

メニューの構造（`ContextMenuStrip` と2つの `ToolStripMenuItem`、`Text`、グリッドへの割り当て）はデザイナーに置けます。

**ラムダは名前付きメソッドに置き換えてください。** `Opening` の中身（選択ファイルの判定、`File.Exists`、URL の有無、有効状態、キャンセル）は通常の `.cs` に残します。

### WebView2

デザイン面に置くこと自体は Microsoft が公式に対応しています。危険なのは初期化です。段階4で扱いますが、原則を先に書いておきます。

- デザイナーで `Source` を設定しない
- `InitializeComponent()` やコンストラクターから `EnsureCoreWebView2Async` を呼ばない
- `CoreWebView2Environment` をそこで作らない
- コンストラクターで `CoreWebView2` に触らない、ナビゲートしない、スクリプトを注入しない

初期化は `Shown` など、実行時であることが明確な地点から始めてください。

**コンストラクター内の `DesignMode` は信用できません。** コントロールがまだ site されていない可能性があります。デザイナーを検出しようとするより、コンストラクターに実行時の処理を書かないほうが安全です。

デザイン面が不安定な場合は、プレースホルダーのパネルをデザイナーに置き、WebView2 は実行時に生成して追加する方法があります。次善策であり、最初から選ぶものではありません。

## 壊れやすい箇所チェックリスト

移行後に1つずつ確認してください。**いずれも「動いているように見えて実は壊れている」類です。**

- [ ] DataGridView の列の順序・名前・`ValueType` が一致している
- [ ] 列見出しをクリックして並べ替えても例外が出ない（特に `作成日時` と `サイズ`）
- [ ] 列の幅を全列でドラッグ変更できる
- [ ] デザイナーと `WireEvents` でイベントを二重購読していない
- [ ] `_versionsGrid.SelectionChanged` が選択件数の表示を更新する
- [ ] グリッド再構築中の `SelectionChanged` が抑止されている（`_rebuildingVersionsGrid`）
- [ ] `FormClosing` で `_operationCancellation?.Cancel()` と `_sharePointSession?.Dispose()` が動く
- [ ] `ApplySettings` 中に `ValueChanged` が発火して再計算が走っていない
- [ ] TableLayoutPanel の列スパン、`RowStyles`、`Dock`、コントロールの順序が保たれている
- [ ] `DeleteConfirmationForm` の `AcceptButton` がキャンセルボタンを指している
- [ ] 起動直後の Enabled 状態（停止・出力・削除ボタンは無効、プログレスバーは非表示）
- [ ] 起動直後の表が空である（デモ機能は削除済み。復活させないこと）
- [ ] サインイン後に WebView2 が破棄・再生成されていない

## 検証手順

1. `dotnet build` — **警告0・エラー0**
2. `dotnet test` — **151件成功**
3. `dotnet run --project src/OneDriveVersionCleaner.App` — 起動して例外が出ない
4. Visual Studio 2026 で `MainForm.cs` と `DeleteConfirmationForm.cs` をデザイナーで開ける
5. デザイナーで何か小さな変更（ボタン位置など）をして保存し、**再度開いて壊れていない**ことを確認する
6. 上のチェックリストを通す

手順5が本番です。デザイナーが再生成しても壊れないことが、この移行の成否そのものです。

## やってはいけないこと

**既存のビルダーメソッドを機械的に `.Designer.cs` へ翻訳すること。** これが最大の失敗パターンです。静的なコントロールのグラフをデザイナー側で作り直し、そのうえで実行時の振る舞いを意図的に接続し直してください。

そのほか、次は現在の安全設計です。移行の過程で失わないでください。

- ファイル本体を削除するコードパスを持たない
- 完全削除のコードパスを持たない
- 現行バージョンを表に出さない、選択させない、計画に入れない

## 参考

- [WinForms のコントロール作成のトラブルシューティング（引数なしコンストラクターの要件）](https://learn.microsoft.com/en-us/dotnet/desktop/winforms/controls/troubleshooting-control-and-component-authoring)
- [DataGridView の列をデザイナーで追加・削除する](https://learn.microsoft.com/en-us/dotnet/desktop/winforms/controls/add-and-remove-columns-in-the-datagrid-using-the-designer)
- [SplitContainer.Panel1MinSize（例外の条件）](https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.splitcontainer.panel1minsize)
- [WinForms アプリでの WebView2 の使用](https://learn.microsoft.com/en-us/microsoft-edge/webview2/get-started/winforms)
- [ToolTip コンストラクター（コンテナーによる破棄）](https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.tooltip.-ctor)
