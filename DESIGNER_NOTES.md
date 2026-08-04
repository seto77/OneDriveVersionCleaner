# WinForms デザイナー保守メモ

3つのフォーム（`MainForm` / `DeleteConfirmationForm` / `SharePointSignInForm`）はすべて `.Designer.cs` を持ち、Visual Studio 2026 のビジュアルデザイナーで開いて編集できます。開いて変更し、保存して開き直しても壊れないことを確認済みです。

本書は、その状態を壊さずに保守するための取り決めです。デザイナーでフォームを触る前に読んでください。

## 環境の前提

- **Visual Studio 2026 が必要です。** VS2022 は .NET 10 に対応せず、今後も対応しません
- .NET 10 SDK（`global.json` でバージョンを固定）
- 対象は Windows のみ（`net10.0-windows`）

```console
dotnet build   # 警告0・エラー0を維持すること
dotnet test    # すべて成功を維持すること（件数は出力を正とする）
dotnet run --project src/OneDriveVersionCleaner.App
```

`Directory.Build.props` で `TreatWarningsAsErrors` が有効です。警告は1件も許容されません。

## 大原則

判断に迷ったら、次の1行に戻ってください。

> **`.Designer.cs` は静的なコンポーネント配置図だけを持つ。振る舞いと、設計時に存在しない状態は、通常の `.cs` が持つ。**

`.Designer.cs` は**生成物**として扱ってください。手で維持する2つ目の実装ファイルではありません。デザイナーが読み書きできない書き方を混ぜた時点で、次にデザイナーで触った瞬間に壊れます。

### `.Designer.cs` に入れるもの

- コントロールとコンポーネントの生成
- 定数のプロパティ（Text、Size、Margin、Padding、Anchor、Dock、色、列挙値）
- TableLayoutPanel / FlowLayoutPanel の行・列・スパン・所属
- DataGridView の列定義（`Name` と `ValueType` を除く。後述）
- メニュー項目の構造
- ToolTip の個別文字列（`SetToolTip` を1行ずつ）
- 名前付きイベントハンドラーの購読
- `components` の Dispose

### 通常の `.cs` に残すもの

- 設定読み込み、アカウント検出、ファイルシステム、ネットワーク
- データに依存する文字列、Enabled / Visible
- グリッドの行と `Tag`
- **列の `Name` と `ValueType` の代入**（後述）
- 分岐、ループ、LINQ、ラムダ、クロージャ、非同期初期化
- メニューを開くかどうかの判断
- SplitContainer の分割位置の確定
- WebView2 の初期化とナビゲーション
- `VersionDeletionPlan` から作るダイアログ内容

### デザイン面の文言は英語

日本語はデザイン面に置きません。`.resx` はデザイナーの所有物で、手で書いた訳は次にフォームを保存した時点で消えます。**デザイン面には英語を置き、実行時に `MainForm.Localization.cs` が訳を上書きします。**

デザイナーでラベルを増やしたら、`ApplyLocalizedText` に対応する行を足してください。足し忘れると、日本語表示のときだけ英語が残ります。

### コンストラクターの順序

```csharp
public MainForm(AppSettings settings, string? settingsReadFailure)
{
    _settings = settings;                 // 1. 依存の保存
    _settingsReadFailure = settingsReadFailure;
    InitializeComponent();                // 2. デザイナー生成の配置
    InitializeGridSchema();               // 3. 列の Name と ValueType
    ApplyUiFont();                        // 4. 表示言語に応じたフォント
    ApplyLocalizedText();                 // 5. 訳の上書き
    ApplySettings();                      // 6. 設定・データの適用
    WireRuntimeEvents();                  // 7. デザイナーが持たないイベント
    RefreshDerivedText();                 // 8. 派生状態の更新
}
```

**イベント購読の所有者は必ず片方に決めてください。** デザイナーとコードの両方で購読すると二重に発火します。

`WireRuntimeEvents` が分かれているのは順序のためです。条件入力の `ValueChanged` をデザイナー側で購読すると、`ApplySettings` が値を入れた時点で再計算が走ります。まだ計算するものが何もない時点でです。

### デザイナー用の引数なしコンストラクター

デザイン環境は引数付きコンストラクターを呼べないため、各フォームは private の引数なしコンストラクターを持っています。**そこからアカウント検出・設定読み込み・ネットワークを呼ばないでください。**

## 個別の注意事項

### DataGridView の列

`HeaderText` `Width` `AutoSizeMode` `Resizable` `SortMode` はデザイナーに置いて構いません。**`Name` と `ValueType` は別扱いです。**

- `Name` — 列名はフォームのコンテナ内のコンポーネント名でもあるため、2つのグリッドが同時に `FileName` と `CloudPath` を持てません。デザイナーはフィールド名を付け、コードが依存する列名は `InitializeGridSchema` で代入します
- `ValueType` — `Browsable(false)` のため、プロパティグリッドから編集できず、デザイナーの再生成で保持される保証がありません

`ValueType` を失うと、**表示は正常なのに並べ替えだけが壊れます。** 発見が遅れる典型例です。

以下が現在の定義です。**順序・名前・型を1つも変えないでください。** `CreateCells` が位置で値を渡し、`CellFormatting` が `Name` で分岐しています。見出しは実行時に翻訳されるため、下表は英語（デザイン面の値）です。

上段（過去バージョン、`_versionsGrid`）

| # | Name | HeaderText | Width | ValueType |
|---:|---|---|---:|---|
| 1 | FileName | File | 180 | string |
| 2 | CloudPath | Path | 320 | string |
| 3 | VersionLabel | Version | 90 | string |
| 4 | Created | Created | 150 | **DateTime** |
| 5 | ModifiedBy | Modified by | 130 | string |
| 6 | VersionSize | Size | 105 | **long** |
| 7 | FileHistorySize | File history total | 115 | **long** |
| 8 | Decision | Verdict | 110 | string |
| 9 | Reasons | Reasons | 260 | string |

下段（ファイル別、`_filesGrid`）

| # | Name | HeaderText | Width | ValueType |
|---:|---|---|---:|---|
| 1 | FileName | File | 180 | string |
| 2 | CloudPath | Path | 320 | string |
| 3 | CurrentSize | Current size | 既定 | **long** |
| 4 | HistoryCount | Versions | 75 | **int** |
| 5 | HistorySize | History size | 115 | **long** |
| 6 | CandidateCount | Candidates | 95 | **int** |
| 7 | Reclaimable | Reclaimable | 115 | **long** |
| 8 | LastModified | Last modified | 140 | **DateTime** |
| 9 | Status | Status | 90 | string |

全列で `AutoSizeMode = None`、`Resizable = True`、`SortMode = Automatic` です。**`Fill` を復活させないでください。** Fill 列は幅を変更してもすぐ戻るため、「一部の列だけ幅を変えられない」という不具合として報告された経緯があります。

### SplitContainer

`SplitterDistance` `Panel1MinSize` `Panel2MinSize` をデザイナーの初期化時に確定させないでください。生成直後のサイズと矛盾すると `ArgumentOutOfRangeException` を投げます。`SuspendLayout()` は検証を止めないので回避策になりません。

現在は `OnShown` の `ApplySplitterLayout` が、実レイアウト後の `ClientSize` を基準に計算しています。デザイナーには階層・`Dock`・`Orientation`・無害な仮の分割位置だけを置いてください。

フォームの `MinimumSize` は根拠になりません。上部の AutoSize 領域が DPI や文字の折り返しで伸びるためです。

### ToolTip

`_toolTip` は `components` コンテナに登録されたデザイナーコンポーネントです。生成される `Dispose(bool)` が破棄を引き受けます。

条件は7項目あり、それぞれラベル・入力・単位ラベルの3コントロールに同じ説明が付きます（`MainForm.Localization.cs` の `ApplyLocalizedToolTips`）。**説明文は現在の文言をそのまま引き継いでください。** 利用者からの指摘を受けて調整した経緯があり、意味が変わると混乱します。訳すのは構いませんが、主張している内容を書き換えないでください。

### ContextMenuStrip

メニューの構造（`ContextMenuStrip` と2つの `ToolStripMenuItem`、`Text`、グリッドへの割り当て）はデザイナーに置けます。

**ラムダは使わず、名前付きメソッドを購読してください。** `Opening` の中身（選択ファイルの判定、`File.Exists`、URL の有無、有効状態、キャンセル）は通常の `.cs` に残します。

### WebView2

デザイン面に置くこと自体は Microsoft が公式に対応しています。危険なのは初期化です。

- デザイナーで `Source` を設定しない
- `InitializeComponent()` やコンストラクターから `EnsureCoreWebView2Async` を呼ばない
- `CoreWebView2Environment` をそこで作らない
- コンストラクターで `CoreWebView2` に触らない、ナビゲートしない、スクリプトを注入しない

初期化は `Shown` など、実行時であることが明確な地点から始めてください。

**コンストラクター内の `DesignMode` は信用できません。** コントロールがまだ site されていない可能性があります。デザイナーを検出しようとするより、コンストラクターに実行時の処理を書かないほうが安全です。

**サインイン用の Form と WebView2 は、セッションが生きている間ずっと生存させる必要があります。** `ShowDialog` が OK を返した後も、アプリはその WebView の Cookie を使って同一オリジンの要求を出し続けます。ダイアログを閉じた時点で Dispose すると、認証済みセッションを失います。`SharePointSignInForm` と `SharePointWebSession` が分かれているのはこのためです。

## デザイナーで触った後に確認すること

**いずれも「動いているように見えて実は壊れている」類です。**

- [ ] DataGridView の列の順序・名前・`ValueType` が一致している
- [ ] 列見出しをクリックして並べ替えても例外が出ない（特に `作成日時` と `サイズ`）
- [ ] 列の幅を全列でドラッグ変更できる
- [ ] デザイナーと `WireRuntimeEvents` でイベントを二重購読していない
- [ ] `_versionsGrid.SelectionChanged` が選択件数の表示を更新する
- [ ] グリッド再構築中の `SelectionChanged` が抑止されている（`_rebuildingVersionsGrid`）
- [ ] `FormClosing` で `_operationCancellation?.Cancel()` と `_sharePointSession?.Dispose()` が動く
- [ ] `ApplySettings` 中に `ValueChanged` が発火して再計算が走っていない
- [ ] TableLayoutPanel の列スパン、`RowStyles`、`Dock`、コントロールの順序が保たれている
- [ ] `DeleteConfirmationForm` の `AcceptButton` が**キャンセルボタン**を指している（Enter キーで削除を始めないための措置。デザイナーで設定し直すと消えます）
- [ ] 起動直後の Enabled 状態（停止・出力・削除ボタンは無効、プログレスバーは非表示）
- [ ] 起動直後の表が空である
- [ ] 日本語と英語を切り替えて、ラベル・ボタン・列見出し・ツールチップがすべて追従する
- [ ] サインイン後に WebView2 が破棄・再生成されていない

## 失ってはいけない安全設計

- ファイル本体を削除するコードパスを持たない
- 完全削除のコードパスを持たない
- 現行バージョンを表に出さない、選択させない、計画に入れない

## 参考

- [WinForms のコントロール作成のトラブルシューティング（引数なしコンストラクターの要件）](https://learn.microsoft.com/en-us/dotnet/desktop/winforms/controls/troubleshooting-control-and-component-authoring)
- [DataGridView の列をデザイナーで追加・削除する](https://learn.microsoft.com/en-us/dotnet/desktop/winforms/controls/add-and-remove-columns-in-the-datagrid-using-the-designer)
- [SplitContainer.Panel1MinSize（例外の条件）](https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.splitcontainer.panel1minsize)
- [WinForms アプリでの WebView2 の使用](https://learn.microsoft.com/en-us/microsoft-edge/webview2/get-started/winforms)
- [ToolTip コンストラクター（コンテナーによる破棄）](https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.tooltip.-ctor)
