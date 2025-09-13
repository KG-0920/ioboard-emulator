@echo off
setlocal EnableExtensions
rem File : assert_emu_exports.bat
rem Ver  : v1.1 (2025-09-12 JST)
rem Why  : IoboardEmulator.dll のエクスポート検査。dumpbin 自動検出を追加

cd /d "%~dp0"
if not exist "logs" md "logs" >nul 2>nul

set "DLL=APP\publish\IoboardEmulator.dll"
if not exist "%DLL%" (
  echo [ERR] not found: %DLL%
  exit /b 1
)

set "DUMPBIN="
where dumpbin >nul 2>nul && for /f "delims=" %%P in ('where dumpbin') do set "DUMPBIN=%%P"
if not defined DUMPBIN (
  rem vswhere → vcvars/VsDevCmd を試す（Developer Promptなしでも通す）
  set "VSW=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"
  if exist "%VSW%" (
    for /f "usebackq tokens=*" %%I in (`"%VSW%" -latest -prerelease -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath`) do set "VSROOT=%%I"
    if defined VSROOT (
      if exist "%VSROOT%\VC\Auxiliary\Build\vcvars64.bat" (
        call "%VSROOT%\VC\Auxiliary\Build\vcvars64.bat" >nul 2>&1
      ) else if exist "%VSROOT%\Common7\Tools\VsDevCmd.bat" (
        call "%VSROOT%\Common7\Tools\VsDevCmd.bat" >nul 2>&1
      )
      where dumpbin >nul 2>nul && for /f "delims=" %%P in ('where dumpbin') do set "DUMPBIN=%%P"
    )
  )
)

if not defined DUMPBIN (
  echo [ERR] dumpbin not found. Please run from 'Developer Command Prompt for VS'.
  exit /b 2
)

"%DUMPBIN%" /exports "%DLL%" > "logs\exports_latest.txt" 2>&1

findstr /C:"DioOpen" /C:"DioClose" /C:"DioInputByte" /C:"DioOutputByte" /C:"DioCommonGetPciDeviceInfo" "logs\exports_latest.txt" >nul
if not errorlevel 1 (
  echo [OK] Exports found (DioInputByte/DioOutputByte naming)
  exit /b 0
)

findstr /C:"DioOpen" /C:"DioClose" /C:"InputByte" /C:"OutputByte" /C:"DioCommonGetPciDeviceInfo" "logs\exports_latest.txt" >nul
if not errorlevel 1 (
  echo [OK] Exports found (InputByte/OutputByte naming)
  exit /b 0
)

echo [ERR] Missing expected exports. See logs\exports_latest.txt
exit /b 3
