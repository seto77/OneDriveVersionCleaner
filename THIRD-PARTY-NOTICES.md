# 第三者コンポーネントの告知 / Third-party notices

このファイルは、配布物に含まれる第三者コンポーネントの告知です。ソースからビルドする場合、これらは
NuGet 取得時に利用者が直接それぞれの条項に同意します。**ビルド済みバイナリを再配布する場合は、この
ファイルを同梱してください。**

This file lists the third-party components redistributed with a built release. When you build
from source you accept each component's terms directly at restore time. **If you redistribute a
built binary, ship this file with it.**

---

## Microsoft.Web.WebView2

- パッケージ / Package: `Microsoft.Web.WebView2`
- 用途 / Used for: アプリ内の Microsoft 365 サインインと、同一オリジンの SharePoint REST 呼び出し
- ライセンス / License: Microsoft 独自条項（MIT ではありません）/ Microsoft-specific terms, not MIT

条項の全文は、パッケージに含まれる `LICENSE.txt` にあります。復元後は次の場所にあります。

The full terms are in the `LICENSE.txt` inside the package. After a restore it is at:

```text
%USERPROFILE%\.nuget\packages\microsoft.web.webview2\<version>\LICENSE.txt
```

同パッケージの `NOTICE.txt` も併せて参照してください。

WebView2 ランタイム本体はこのリポジトリでは配布しません。利用者の Windows に既に入っているものを使い
ます。

The WebView2 runtime itself is not redistributed here; the copy already present on the user's
Windows installation is used.

---

## .NET ランタイム / .NET runtime

`scripts/publish.ps1` は self-contained な単一 exe を作ります。**この exe には .NET ランタイムが含まれ
ます。** Windows は .NET を同梱しないため、これ以外の形式だと利用者に別途インストールを求めることに
なります。

`scripts/publish.ps1` produces a self-contained single-file executable, so **the .NET runtime is
embedded in it.** Windows ships no .NET runtime, so any other shape would force the user to
install one first.

- ライセンス / License: MIT (.NET Runtime, .NET Windows Desktop Runtime)
- Copyright (c) .NET Foundation and Contributors
- <https://github.com/dotnet/runtime/blob/main/LICENSE.TXT>

---

## 本ソフトウェア / This software

OneDriveVersionCleaner 自体は [MIT License](LICENSE.md) です。上記の条項は本ソフトウェアのライセンス
とは別のものであり、本ソフトウェアの MIT ライセンスがそれらを上書きすることはありません。

OneDriveVersionCleaner itself is under the [MIT License](LICENSE.md). The terms above are
separate from it, and this software's MIT licence does not override them.
