@echo off
setlocal

REM 引数1：Emulator/Server の CONFIG（既定 Release）
REM 引数2：Apps の CONFIG（既定 Debug）
set "CONFIG_EMU_SVR=%~1"
if "%CONFIG_EMU_SVR%"=="" set "CONFIG_EMU_SVR=Release"
set "CONFIG_APPS=%~2"
if "%CONFIG_APPS%"=="" set "CONFIG_APPS=Debug"

set "ROOT=%~dp0"

echo.
echo ===== PUBLISH ALL START =====

call "%ROOT%publish_emulator_aot.bat" "%CONFIG_EMU_SVR%" || goto :error
call "%ROOT%publish_app_a.bat"       "%CONFIG_APPS%"     || goto :error
call "%ROOT%publish_app_b.bat"       "%CONFIG_APPS%"     || goto :error
call "%ROOT%publish_server.bat"      "%CONFIG_EMU_SVR%"  || goto :error

echo ===== ALL DONE =====
exit /b 0

:error
echo [ERROR] One of publish steps failed.
exit /b 1
