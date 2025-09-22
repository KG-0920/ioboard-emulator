<# HashCheck.ps1 (固定パス版)
  目的:
    - 実行フォルダ(配布物) と 開発フォルダ( publish 物 ) の SHA256 を比較
    - 不一致/不足があれば exit 1 で終了（CI/手動チェック両方で使える）
    - 固定パスで動かす（ci_check.bat とは独立運用）

  固定パス（必要に応じて編集してください）
#>

# ==== 固定パスをここで設定（編集ポイント）====
$RepoRoot = "D:\PcBackup\Windows11\一時\GitHubClone\ioboard-emulator"
$AppDir   = "D:\PcBackup\Windows11\一時\ChatGPTとのやり取り\Testルートフォルダ\APP"
# ================================================

$ErrorActionPreference = "Stop"

function Show-FileInfo {
  param([string]$Path)
  if (Test-Path -LiteralPath $Path) {
    Get-Item -LiteralPath $Path | Select-Object FullName,Length,LastWriteTime
  } else {
    [pscustomobject]@{ FullName=$Path; Length=$null; LastWriteTime=$null }
  }
}

function Get-HashOrNull {
  param([string]$Path)
  if (Test-Path -LiteralPath $Path) {
    return (Get-FileHash -Algorithm SHA256 -LiteralPath $Path).Hash
  }
  return $null
}

# publish 側（開発フォルダ内）
$emuPub = Join-Path $RepoRoot "APP\publish\IoboardEmulator.dll"
$svrPub = Join-Path $RepoRoot "IoboardServer\publish\win-x64\Release\IoboardServer.exe"

# 実行側（配布先 APP 直下）
$emuApp = Join-Path $AppDir "IoboardEmulator.dll"
$svrApp = Join-Path $AppDir "IoboardServer.exe"

Write-Host ""
Write-Host "=== IoboardServer.exe ==="
Show-FileInfo $svrApp
Show-FileInfo $svrPub
Write-Host ""

Write-Host "=== IoboardEmulator.dll ==="
Show-FileInfo $emuApp
Show-FileInfo $emuPub
Write-Host ""

# ===== 整合チェック =====
$fail = $false

# DLL
$hAppDll = Get-HashOrNull $emuApp
$hPubDll = Get-HashOrNull $emuPub

if (-not $hAppDll) { Write-Host "[ERR] Missing App DLL: $emuApp" -ForegroundColor Red; $fail = $true }
if (-not $hPubDll) { Write-Host "[ERR] Missing publish DLL: $emuPub" -ForegroundColor Red; $fail = $true }
if ($hAppDll -and $hPubDll) {
  if ($hAppDll -ne $hPubDll) {
    Write-Host "[ERR] HASH mismatch: IoboardEmulator.dll" -ForegroundColor Red
    Write-Host "  AppDir : $hAppDll"
    Write-Host "  publish: $hPubDll"
    $fail = $true
  } else {
    Write-Host "[OK] DLL hash matched."
  }
}

# Server EXE
$hAppExe = Get-HashOrNull $svrApp
$hPubExe = Get-HashOrNull $svrPub

if (-not $hAppExe) { Write-Host "[ERR] Missing App Server: $svrApp" -ForegroundColor Red; $fail = $true }
if (-not $hPubExe) { Write-Host "[ERR] Missing publish Server: $svrPub" -ForegroundColor Red; $fail = $true }
if ($hAppExe -and $hPubExe) {
  if ($hAppExe -ne $hPubExe) {
    Write-Host "[ERR] HASH mismatch: IoboardServer.exe" -ForegroundColor Red
    Write-Host "  AppDir : $hAppExe"
    Write-Host "  publish: $hPubExe"
    $fail = $true
  } else {
    Write-Host "[OK] Server hash matched."
  }
}

if ($fail) { exit 1 } else { exit 0 }
