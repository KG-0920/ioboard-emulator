@echo off
setlocal

REM ==== Settings ====
set "RID=win-x64"
set "CONFIG=%~1"
if "%CONFIG%"=="" set "CONFIG=Debug"

REM ==== Paths ====
set "ROOT=%~dp0"
set "CSProj=%ROOT%APP\APP_A\APP_A.csproj"
set "OUTDIR=%ROOT%APP\publish\APP_A"
set "EXE=%OUTDIR%\APP_A.exe"

REM （Emulator AOT DLL を親フォルダに置く運用の場合）
set "EMU_OUT=%ROOT%IoboardEmulator\publish\%RID%\%CONFIG%"
set "EMU_DLL=IoboardEmulator.dll"
set "APP_PUBLISH=%ROOT%APP\publish"

where dotnet >nul 2>nul || (echo [ERROR] dotnet not found & exit /b 1)
if not exist "%CSProj%" (echo [ERROR] Not found: %CSProj% & exit /b 1)

echo.
echo === Publish APP_A (%RID%, %CONFIG%) ===
dotnet publish "%CSProj%" -c "%CONFIG%" -r "%RID%" -o "%OUTDIR%" ^
 /p:PublishSingleFile=true /p:SelfContained=true /p:UseAppHost=true ^
 /p:PublishTrimmed=false /p:EnableCompressionInSingleFile=true ^
 /p:IncludeNativeLibrariesForSelfExtract=true
if errorlevel 1 (echo [ERROR] dotnet publish failed & exit /b 1)

if not exist "%EXE%" (
  echo [ERROR] Missing EXE: %EXE%
  exit /b 1
)

for %%A in ("%EXE%") do set "SIZE=%%~zA"
echo --- Output: "%EXE%" (%SIZE% bytes)

REM ★ 1MB 未満は FDD(apphost) の可能性が高い → エラー扱い
if %SIZE% LSS 1000000 (
  echo [ERROR] EXE too small. Looks framework-dependent. SingleFile/SelfContained not applied.
  exit /b 1
)

echo [OK] APP_A -> %OUTDIR%

echo.
echo === (Optional) Copy Emulator DLL to APP\publish (parent folder strategy for Debug) ===
if not exist "%EMU_OUT%\%EMU_DLL%" (
  echo [WARN] Emulator DLL not found at "%EMU_OUT%\%EMU_DLL%". Skipped copy.
) else (
  if not exist "%APP_PUBLISH%" mkdir "%APP_PUBLISH%"
  copy /Y "%EMU_OUT%\%EMU_DLL%" "%APP_PUBLISH%\%EMU_DLL%" >nul
  if errorlevel 1 (echo [ERROR] Copy failed & exit /b 1)
  echo [OK] %EMU_DLL% -> %APP_PUBLISH%
)

exit /b 0
