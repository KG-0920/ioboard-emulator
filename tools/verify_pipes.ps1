<# 
  tools/verify_pipes.ps1
  目的:
    - ソース/バイナリに旧パイプ名の直書きがないか検査し、見つかったら終了コード 1 で失敗。
    - V1 正準: PipeConfig.PipeName("IoboardBus") のみ許可。
  使い方:
    - ci_check.bat から呼び出し（レポジトリルートで実行される想定）
#>

param(
  [string]$RepoRoot = (Get-Location).Path
)

$ErrorActionPreference = "Stop"

$patterns = @(
  'IoboardEmu_RSW_',        # 旧1
  'IoboardEmulator_RSW',    # 旧2
  'ioboard_emulator_rsw'    # 旧3
)

$badHits = @()
$self = $MyInvocation.MyCommand.Path  # このスクリプト自身のフルパス

# ---- 1) ソース検査（.cs/.xaml/.bat/.ps1 などテキスト）----
$srcExt = @('*.cs','*.xaml','*.bat','*.ps1','*.cmd','*.props','*.targets')
$srcFiles = @()
foreach ($ext in $srcExt) {
  $srcFiles += Get-ChildItem -Path $RepoRoot -Recurse -File -Filter $ext -ErrorAction SilentlyContinue
}

foreach ($f in $srcFiles) {
  if ($f.FullName -ieq $self) { continue }
  $text = Get-Content -LiteralPath $f.FullName -Raw -ErrorAction SilentlyContinue
  foreach ($p in $patterns) {
    if ($text -match [Regex]::Escape($p)) {
      $badHits += "SRC:`t$($f.FullName):`t$p"
    }
  }
}

# ---- 2) バイナリ検査（.dll/.exe の埋め込み文字列）----
$binExt = @('*.dll','*.exe')
$binFiles = @()
foreach ($ext in $binExt) {
  $binFiles += Get-ChildItem -Path $RepoRoot -Recurse -File -Filter $ext -ErrorAction SilentlyContinue
}

foreach ($bf in $binFiles) {
  try {
    $bytes = [System.IO.File]::ReadAllBytes($bf.FullName)
    $utf8  = [System.Text.Encoding]::UTF8.GetString($bytes)
    foreach ($p in $patterns) {
      if ($utf8.Contains($p)) {
        $badHits += "BIN:`t$($bf.FullName):`t$p"
      }
    }
  } catch {
    # 読めない実行ファイルはスキップ（署名/圧縮等）
  }
}

if ($badHits.Count -gt 0) {
  Write-Host "[NG] Forbidden pipe names were found:" -ForegroundColor Red
  $badHits | ForEach-Object { Write-Host " - $_" -ForegroundColor Red }
  Write-Host "Use Common.PipeConfig.PipeName (IoboardBus) only." -ForegroundColor Red
  exit 1
} else {
  Write-Host "[OK] No forbidden pipe names detected. (Canonical only: IoboardBus)"
  exit 0
}
