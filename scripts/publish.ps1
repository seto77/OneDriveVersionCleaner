# Builds the portable release: one self-contained exe, packed into a zip with the documents that
# have to travel with it.
#
# Self-contained because Windows ships no .NET runtime: a framework-dependent build would make
# every user install one first. Zip rather than an installer because this tool is run a handful of
# times and then put away — unpack it where you like, delete the folder when you are done. Nothing
# is written outside %LOCALAPPDATA%, so there is nothing for an uninstaller to undo.
[CmdletBinding()]
param(
    [string]$Runtime = 'win-x64',
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repo 'src\OneDriveVersionCleaner.App\OneDriveVersionCleaner.App.csproj'
$stage = Join-Path $repo "publish\OneDriveVersionCleaner-$Runtime"
$archive = "$stage.zip"

if (Test-Path -LiteralPath $stage) {
    Remove-Item -LiteralPath $stage -Recurse -Force
}

# The bundle is compressed. Measured on win-x64: it takes the exe from 112 MB to 50 MB while the
# zip stays at about 44 MB either way — so it costs nothing to download and saves the user 60 MB
# in the folder they keep. AllowedReferenceRelatedFileExtensions drops the WebView2 IntelliSense
# XML, which is 0.6 MB of documentation nobody reads from inside a shipped application.
dotnet publish $project `
    --configuration $Configuration `
    --runtime $Runtime `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:DebugType=none `
    -p:AllowedReferenceRelatedFileExtensions=none `
    --output $stage `
    --nologo
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE" }

# THIRD-PARTY-NOTICES is not optional: the bundle carries the .NET runtime, and the WebView2
# package's terms require its notice to travel with a redistributed binary.
foreach ($document in 'README.md', 'README.ja.md', 'LICENSE.md', 'THIRD-PARTY-NOTICES.md') {
    Copy-Item -LiteralPath (Join-Path $repo $document) -Destination $stage
}

# A release that ships a failing build is worse than no release. This runs after publish so a
# packaging attempt cannot quietly skip it.
dotnet test (Join-Path $repo 'OneDriveVersionCleaner.slnx') --configuration $Configuration --nologo
if ($LASTEXITCODE -ne 0) { throw "dotnet test failed with exit code $LASTEXITCODE" }

if (Test-Path -LiteralPath $archive) {
    Remove-Item -LiteralPath $archive -Force
}

# Zipped through .NET rather than an external archiver, so that building a release needs nothing
# installed beyond the SDK. The base directory is included, so unpacking gives one folder instead
# of spilling files into wherever the user extracted it.
[System.IO.Compression.ZipFile]::CreateFromDirectory(
    $stage,
    $archive,
    [System.IO.Compression.CompressionLevel]::SmallestSize,
    $true)

$published = (Get-ChildItem -LiteralPath $stage -Recurse -File | Measure-Object -Property Length -Sum).Sum
$packed = (Get-Item -LiteralPath $archive).Length
$hash = (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant()
Set-Content -LiteralPath "$archive.sha256" -Value "$hash  $(Split-Path -Leaf $archive)" -Encoding ascii

'{0}' -f $archive
'  展開後: {0:N1} MB  →  zip: {1:N1} MB ({2:N0}% 削減)' -f `
    ($published / 1MB), ($packed / 1MB), (100 - 100 * $packed / $published)
'  SHA-256: {0}' -f $hash
'  この exe には Authenticode 署名がありません。SmartScreen の警告が出ます。'
