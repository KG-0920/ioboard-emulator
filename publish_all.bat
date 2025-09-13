@echo off
setlocal EnableExtensions
rem File : publish_all.bat
rem Ver  : v1.0 (2025-09-12 JST)
rem Why  : 既存の呼び出し順を明示。等価リファクタは行わない。

cd /d "%~dp0"
echo ===== PUBLISH ALL START =====

rem エミュレータ（AOT 共有DLL）
call "%~dp0publish_emulator_aot.bat" || goto :err

rem APP_A / APP_B（Debug）
echo === Publish APP_A (win-x64, Debug) ===
call "%~dp0publish_app_a.bat" || goto :err

echo === Publish APP_B (win-x64, Debug) ===
call "%~dp0publish_app_b.bat" || goto :err

rem サーバ（Release）
echo --- Stopping running server (if any) ---
taskkill /f /im IoboardServer.exe >nul 2>nul

echo === Publish IoboardServer [Release] ===
call "%~dp0publish_server.bat" || goto :err

exit /b 0

:err
echo *** ERROR in publish_all.bat ***
exit /b 1
