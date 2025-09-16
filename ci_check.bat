@echo off
setlocal ENABLEDELAYEDEXPANSION
rem File : ci_check.bat
rem Ver  : v1.0 (2025-09-12 JST)
rem Why  : Clean → Build → Publish の一連を既存順序で実行。等価リファクタは行わない。

cd /d "%~dp0"

REM =========================================================
REM Clean
REM =========================================================
echo [STEP] kill running apps
taskkill /f /im APP_A.exe >nul 2>nul
taskkill /f /im APP_B.exe >nul 2>nul
taskkill /f /im IoboardServer.exe >nul 2>nul

echo [STEP] remove publish dirs
rd /s /q "APP\publish"                                    2>nul
rd /s /q "IoboardServer\publish"                           2>nul
rd /s /q "IoboardEmulator\publish"                         2>nul

echo [STEP] remove bin/obj
rd /s /q "APP\APP_A\bin"          2>nul
rd /s /q "APP\APP_A\obj"          2>nul
rd /s /q "APP\APP_B\bin"          2>nul
rd /s /q "APP\APP_B\obj"          2>nul
rd /s /q "IoboardServer\bin"      2>nul
rd /s /q "IoboardServer\obj"      2>nul
rd /s /q "IoboardEmulator\bin"    2>nul
rd /s /q "IoboardEmulator\obj"    2>nul
rd /s /q "Common\bin"             2>nul
rd /s /q "Common\obj"             2>nul

echo [OK] Clean completed. Starting build/publish...

REM =========================================================
REM Build (keep project order & configs)
REM =========================================================
echo === Build Debug and Release ===

echo --- Building APP_A [Debug]
dotnet build "APP\APP_A\APP_A.csproj" -c Debug -r win-x64 || goto :err

echo --- Building APP_B [Debug]
dotnet build "APP\APP_B\APP_B.csproj" -c Debug -r win-x64 || goto :err

echo --- Building IoboardEmulator [Release]
dotnet build "IoboardEmulator\IoboardEmulator.csproj" -c Release -r win-x64 || goto :err

echo --- Building IoboardServer [Release]
dotnet build "IoboardServer\IoboardServer.csproj" -c Release -r win-x64 || goto :err


REM =========================================================
REM Publish （一本化：publish_all.bat を呼び出し）
REM   ※ publish_all.bat 内で publish_app_*.bat / publish_emulator_aot.bat /
REM      publish_server.bat を呼ぶ想定（出力パスは V1 規約に整合）
REM =========================================================
echo === Publish Emu/Server=Release, Apps=Debug ===
echo ===== PUBLISH ALL START =====
call "publish_all.bat" || goto :err

REM =========================================================
REM (A) 旧パイプ名ガード（V1 準拠）
REM =========================================================
echo === Verify forbidden pipe names (canonical only) ===
if exist "tools\verify_pipes.ps1" (
  powershell -NoProfile -ExecutionPolicy Bypass -File "tools\verify_pipes.ps1"
  if errorlevel 1 goto :err
) else (
  echo [ERR] tools\verify_pipes.ps1 not found. Please add it. & goto :err
)

REM =========================================================
REM (B) 配布整合（SHA256）：手動配布後に APP\ が存在する場合のみ検査
REM   - APP\IoboardEmulator.dll  vs APP\publish\IoboardEmulator.dll
REM   - APP\IoboardServer.exe    vs IoboardServer\publish\win-x64\Release\IoboardServer.exe
REM =========================================================
echo === Verify distribution integrity (SHA256) ===
if exist "APP\IoboardEmulator.dll" (
  if not exist "APP\publish\IoboardEmulator.dll" (
    echo [ERR] Missing: APP\publish\IoboardEmulator.dll & goto :err
  )
  for %%F in (IoboardEmulator.dll) do (
    for /f "tokens=1,*" %%a in ('powershell -NoProfile -Command "Get-FileHash -Algorithm SHA256 '.\APP\%%F' | %% {$_.Hash}"') do set H1=%%a
    for /f "tokens=1,*" %%a in ('powershell -NoProfile -Command "Get-FileHash -Algorithm SHA256 '.\APP\publish\%%F' | %% {$_.Hash}"') do set H2=%%a
    if /I not "!H1!"=="!H2!" (
      echo [ERR] HASH mismatch: %%F
      echo   APP\%%F         = !H1!
      echo   APP\publish\%%F = !H2!
      goto :err
    )
  )
) else (
  echo [INFO] APP\IoboardEmulator.dll not found. Skipping deploy integrity check for DLL (manual copy not done yet?).
)

if exist "APP\IoboardServer.exe" (
  if not exist "IoboardServer\publish\win-x64\Release\IoboardServer.exe" (
    echo [ERR] Missing: IoboardServer\publish\win-x64\Release\IoboardServer.exe & goto :err
  )
  for %%F in (IoboardServer.exe) do (
    for /f "tokens=1,*" %%a in ('powershell -NoProfile -Command "Get-FileHash -Algorithm SHA256 '.\APP\%%F' | %% {$_.Hash}"') do set H1=%%a
    for /f "tokens=1,*" %%a in ('powershell -NoProfile -Command "Get-FileHash -Algorithm SHA256 '.\IoboardServer\publish\win-x64\Release\%%F' | %% {$_.Hash}"') do set H2=%%a
    if /I not "!H1!"=="!H2!" (
      echo [ERR] HASH mismatch: %%F
      echo   APP\%%F                                   = !H1!
      echo   IoboardServer\publish\win-x64\Release\%%F = !H2!
      goto :err
    )
  )
) else (
  echo [INFO] APP\IoboardServer.exe not found. Skipping deploy integrity check for Server (manual copy not done yet?).
)

REM =========================================================
REM (C) エクスポート確認（dumpbin が使える場合のみ）
REM =========================================================
where dumpbin >nul 2>nul
if not errorlevel 1 (
  if exist "assert_emu_exports.bat" (
    echo === Verify exports of IoboardEmulator.dll (dumpbin) ===
    call "assert_emu_exports.bat"
    if errorlevel 1 goto :err
  ) else (
    echo [INFO] assert_emu_exports.bat not found. Skipping export verification hook.
  )
) else (
  echo [INFO] dumpbin not found. Skipping export verification. (Use Developer Command Prompt to enable)
)

echo === DONE ===
exit /b 0

:err
echo *** ERROR in ci_check.bat ***
exit /b 1
