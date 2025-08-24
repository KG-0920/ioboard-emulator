@echo off
setlocal

REM ===== Settings =====
set CONF=Debug
set OUTROOT=APP\publish
set APP_A=APP\APP_A\APP_A.csproj
set APP_B=APP\APP_B\APP_B.csproj

REM ===== Clean output =====
if exist "%OUTROOT%" rmdir /s /q "%OUTROOT%"

REM ===== Publish APP_A (Debug / self-contained / single-file) =====
echo === Publish APP_A (%CONF%) ===
dotnet publish "%APP_A%" -c %CONF% -o "%OUTROOT%\APP_A" || exit /b 1

REM ===== Publish APP_B (Debug / self-contained / single-file) =====
echo === Publish APP_B (%CONF%) ===
dotnet publish "%APP_B%" -c %CONF% -o "%OUTROOT%\APP_B" || exit /b 1

REM ===== Copy shared config files into both apps (optional) =====
echo === Copy Config Files ===
if exist IoboardConfig.xml (
  copy /y IoboardConfig.xml "%OUTROOT%\APP_A\\" >nul
  copy /y IoboardConfig.xml "%OUTROOT%\APP_B\\" >nul
)
if exist IoLogConfig.xml (
  copy /y IoLogConfig.xml "%OUTROOT%\APP_A\\" >nul
  copy /y IoLogConfig.xml "%OUTROOT%\APP_B\\" >nul
)

REM ===== Place IoboardEmulator.dll at the parent of both apps =====
REM 期待する実行レイアウト：
REM publish\
REM  ├─ APP_A\APP_A.exe
REM  ├─ APP_B\APP_B.exe
REM  └─ IoboardEmulator.dll   ← ★ exe の1つ上に置く（IoBoardWrapper の #if DEBUG 前提）
echo === Locate & Copy IoboardEmulator.dll ===
set EMU_DST=%OUTROOT%\IoboardEmulator.dll

REM 代表的な場所を順に探索（必要に応じてパスを調整）
set EMU_CAND1=IoboardEmulator\bin\Debug\net8.0-windows\IoboardEmulator.dll
set EMU_CAND2=IoboardEmulator\bin\Debug\IoboardEmulator.dll

if exist "%EMU_CAND1%" (
  copy /y "%EMU_CAND1%" "%EMU_DST%" >nul
) else if exist "%EMU_CAND2%" (
  copy /y "%EMU_CAND2%" "%EMU_DST%" >nul
) else (
  echo [WARN] IoboardEmulator.dll が見つかりませんでした。手動で "%EMU_DST%" に配置してください。
)

echo === Done ===
echo Output root: %OUTROOT%
echo - Run: %OUTROOT%\APP_A\APP_A.exe / %OUTROOT%\APP_B\APP_B.exe  (Debug自己完結)
echo - Ensure: %OUTROOT%\IoboardEmulator.dll exists (Debugのみ必須)

endlocal
