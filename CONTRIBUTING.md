# 開発に参加する / Contributing

日本語でも英語でも構いません。Issue も Pull Request も歓迎します。

Japanese or English, both are fine. Issues and pull requests are welcome.

## 貼る前に / Before you paste

**ログ・出力・スクリーンショットから、パス・ファイル名・組織名・アカウント名・ID を外してください。**
このツールが扱うものには、それらがそのまま入っています。詳しくは [SECURITY.md](SECURITY.md) を
参照してください。

**Remove paths, file names, organisation and account names, and ids from anything you paste** —
logs, exports and screenshots all carry them. See [SECURITY.md](SECURITY.md).

## 開発環境 / Getting set up

- Windows（`net10.0-windows` のため、他の OS ではビルドできません）
- .NET 10 SDK（`global.json` が版を固定しています）
- フォームをデザイナーで編集する場合は Visual Studio 2026。VS2022 は .NET 10 に対応しません

```console
dotnet build
dotnet test
dotnet run --project src/OneDriveVersionCleaner.App
```

`Directory.Build.props` で `TreatWarningsAsErrors` が有効です。**警告は1件も通りません。** CI も同じ
条件で動きます（[.github/workflows/ci.yml](.github/workflows/ci.yml)）。

## 変えてはいけないもの / What must not change

安全側の設計です。**これらを外す変更は取り込めません。**

These are the safety properties of the tool. **A change that removes one of them will not be merged.**

- ファイル本体を削除するコードパスを持たない / no code path deletes a file itself
- ごみ箱を経由しない完全削除のコードパスを持たない / no code path deletes permanently
- 現行バージョンは表に出さず、選択もさせず、計画にも入れない / the current version is never listed, selected or planned
- 記録を開けないときは移動を開始しない / nothing is recycled unless the log opened first

## 置き場所 / Where code goes

| 置き場所 | 何を置くか |
|---|---|
| `Core/` | 保持判定、集計、削除計画、キャッシュ、問合わせ上限、出力。UI にも通信にも依存しない |
| `Infrastructure/` | Windows レジストリからの OneDrive アカウント検出 |
| `App/` | WinForms UI、SharePoint REST セッション、表示言語 |

**判断のロジックは `Core/` に置いてください。** そこに置けば単体テストで検証できます。UI や通信を
必要とする形で書くと、検証手段が実機だけになります。

## テスト / Tests

`Core/` に手を入れたら、テストも足してください。既存のテストは
[tests/OneDriveVersionCleaner.Core.Tests/](tests/OneDriveVersionCleaner.Core.Tests/) にあります。

削除・通信の経路は自動テストの対象外です。実機での確認手順は
[RESTART_TEST_INSTRUCTIONS.md](RESTART_TEST_INSTRUCTIONS.md) にあります。**実データではなく、小さな
テスト用フォルダーで確認してください。**

## フォームを編集する / Editing the forms

デザイナーで触る前に [DESIGNER_NOTES.md](DESIGNER_NOTES.md) を読んでください。`.Designer.cs` に
何を置いてよく、何を置いてはいけないかが書いてあります。**デザイン面の文言は英語です。** 日本語は
実行時に `MainForm.Localization.cs` が上書きします。

Read [DESIGNER_NOTES.md](DESIGNER_NOTES.md) before opening a form in the designer. **The design
surface carries English**; Japanese is applied at run time.

## 文言 / Wording

画面の文言、とくに条件のツールチップは、利用者からの指摘を受けて調整した経緯があります。**訳すのは
構いませんが、主張している内容を書き換えないでください。**

The interface wording — the rule tooltips in particular — has been adjusted in response to what
users misread. Translating it is fine; changing what it claims is not.
