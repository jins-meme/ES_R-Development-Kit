<#
Windows版リリース資産のパッケージングスクリプト。
公開用ディレクトリ(発行済みexe/dll一式など)を受け取り、デバッグシンボルや
ユーザー固有ファイルを除いた状態でzip化する。

Usage:
  pwsh scripts/package-windows-release.ps1 -SourceDir <公開したいフォルダ> -OutputZip <出力zipパス>
#>
param(
    [Parameter(Mandatory = $true)][string]$SourceDir,
    [Parameter(Mandatory = $true)][string]$OutputZip
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $SourceDir)) {
    throw "SourceDir not found: $SourceDir"
}

$stage = Join-Path $env:TEMP ("release_stage_" + [guid]::NewGuid())
New-Item -ItemType Directory -Path $stage | Out-Null

try {
    # obj/, bin\Debug, .vs, *.pdb, *.user などビルド中間生成物・個人設定を除外してコピー
    robocopy $SourceDir $stage /E `
        /XD obj ".vs" "bin\Debug" `
        /XF "*.pdb" "*.user" "*.vspscc" `
        | Out-Null

    if (Test-Path $OutputZip) {
        Remove-Item $OutputZip -Force
    }
    Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $OutputZip -Force
    Write-Host "Created $OutputZip"
}
finally {
    Remove-Item -Recurse -Force $stage
}
