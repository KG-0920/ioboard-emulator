@echo off
setlocal EnableExtensions
rem File : assert_emu_native.bat
rem Ver  : v1.0 (2025-09-12 JST)
rem Why  : IoboardEmulator.dll が CLR ヘッダを持っていないか（= ネイティブ相当か）を確認

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
  echo [ERR] dumpbin not found. Use Developer Command Prompt for VS.
  exit /b 2
)

"%DUMPBIN%" /headers "%DLL%" > "logs\headers_latest.txt" 2>&1
findstr /i "CLR header" "logs\headers_latest.txt" >nul
if errorlevel 1 (
  echo [OK] No CLR header detected (likely native/AOT image)
  exit /b 0
) else (
  echo [ERR] CLR header present (managed image). See logs\headers_latest.txt
  exit /b 3
)
