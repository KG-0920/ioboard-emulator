@echo off
setlocal EnableExtensions
REM === Settings ===
set "RID=win-x64"
set "CONFIG=%~1"
if "%CONFIG%"=="" set "CONFIG=Release"

REM === Paths ===
set "ROOT=%~dp0"
set "CSProj=%ROOT%IoboardServer\IoboardServer.csproj"
set "OUTDIR=%ROOT%IoboardServer\publish\%RID%\%CONFIG%"
set "EXE=%OUTDIR%\IoboardServer.exe"

where dotnet >nul 2>nul || (echo [ERROR] dotnet not found & exit /b 1)
if not exist "%CSProj%" (echo [ERROR] Not found: %CSProj% & exit /b 1)

echo --- Stopping running server (if any) ---
taskkill /IM IoboardServer.exe /F >nul 2>nul

echo --- Clean output dir ---
if exist "%OUTDIR%" rmdir /S /Q "%OUTDIR%"

echo === Publish IoboardServer [RID=%RID% CONFIG=%CONFIG%] (SingleFile, SelfContained) ===
REM ★ 改行バックスラッシュ(^)は使わず、確実に渡す
dotnet publish "%CSProj%" -c "%CONFIG%" -r "%RID%" -o "%OUTDIR%" ^
 /p:PublishSingleFile=true /p:SelfContained=true /p:UseAppHost=true ^
 /p:StripSymbols=true /p:InvariantGlobalization=true /p:EnableCompressionInSingleFile=true ^
 /p:PublishTrimmed=false
if errorlevel 1 (echo [ERROR] dotnet publish failed & exit /b 1)

if not exist "%EXE%" (
  echo [ERROR] Missing exe: "%EXE%"
  exit /b 1
)

for %%A in ("%EXE%") do set "SIZE=%%~zA"
echo --- Output: "%EXE%" (%SIZE% bytes)

REM ★ 最低 1MB 未満は FDD(apphost) になっている可能性大 → エラー扱い
if %SIZE% LSS 1000000 (
  echo [ERROR] EXE too small. Looks framework-dependent. Enforcing SelfContained/SingleFile failed.
  echo        Check: .csproj properties and that no higher-level Directory.Build.props overrides them.
  exit /b 1
)

echo [OK] Published single-file self-contained server.
exit /b 0
