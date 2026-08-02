# Builds a self-contained single-file release and packs it with 7-Zip.
#
# Self-contained because Windows ships no .NET runtime: a framework-dependent build would make
# every user install one first. The bundle is left uncompressed so that 7-Zip can do the
# compressing; compressing twice makes the archive larger, not smaller.
[CmdletBinding()]
param(
    [string]$Runtime = 'win-x64',
    [string]$Configuration = 'Release',
    [string]$SevenZip = 'C:\Program Files\7-Zip\7z.exe'
)

$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repo 'src\OneDriveVersionCleaner.App\OneDriveVersionCleaner.App.csproj'
$stage = Join-Path $repo "publish\$Runtime"
$archive = Join-Path $repo "publish\OneDriveVersionCleaner-$Runtime.7z"

if (Test-Path -LiteralPath $stage) {
    Remove-Item -LiteralPath $stage -Recurse -Force
}

dotnet publish $project `
    --configuration $Configuration `
    --runtime $Runtime `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=false `
    -p:DebugType=none `
    -p:PublishDocumentationFile=false `
    -p:PublishDocumentationFiles=false `
    --output $stage `
    --nologo
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE" }

Copy-Item -LiteralPath (Join-Path $repo 'README.md') -Destination $stage
Copy-Item -LiteralPath (Join-Path $repo 'LICENSE.md') -Destination $stage

if (-not (Test-Path -LiteralPath $SevenZip)) {
    Write-Warning "7-Zip not found at $SevenZip. Published files are in $stage."
    return
}

if (Test-Path -LiteralPath $archive) {
    Remove-Item -LiteralPath $archive -Force
}

# LZMA2 at -mx=9. Stronger dictionary and word sizes were measured and gave no further gain.
& $SevenZip a -t7z -mx=9 -m0=lzma2 $archive (Join-Path $stage '*') | Out-Null
if ($LASTEXITCODE -ne 0) { throw "7-Zip failed with exit code $LASTEXITCODE" }

$published = (Get-ChildItem -LiteralPath $stage -Recurse -File | Measure-Object -Property Length -Sum).Sum
$packed = (Get-Item -LiteralPath $archive).Length
'{0}' -f $archive
'  発行: {0:N1} MB  →  書庫: {1:N1} MB ({2:N0}% 削減)' -f `
    ($published / 1MB), ($packed / 1MB), (100 - 100 * $packed / $published)
