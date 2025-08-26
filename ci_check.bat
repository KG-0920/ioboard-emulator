@echo off
setlocal EnableExtensions
set "ROOT=%~dp0"
set "EXITCODE=0"

REM --- /nopause でキー待ち無効 ---
set "NOPAUSE="
if /I "%~1"=="/nopause" set "NOPAUSE=1"

title IoBoard CI Check
where dotnet >nul 2>nul || (
  echo [ERROR] dotnet SDK not found in PATH.
  set "EXITCODE=1"
  goto :FINALLY
)

echo === Build Debug and Release ===
for %%C in (Debug Release) do (
  echo --- Building IoboardEmulator [%%C]
  dotnet build "%ROOT%IoboardEmulator\IoboardEmulator.csproj" -c %%C || (set "EXITCODE=1" & goto :FINALLY)
  echo --- Building APP_A [%%C]
  dotnet build "%ROOT%APP\APP_A\APP_A.csproj" -c %%C || (set "EXITCODE=1" & goto :FINALLY)
  echo --- Building APP_B [%%C]
  dotnet build "%ROOT%APP\APP_B\APP_B.csproj" -c %%C || (set "EXITCODE=1" & goto :FINALLY)
  echo --- Building IoboardServer [%%C]
  dotnet build "%ROOT%IoboardServer\IoboardServer.csproj" -c %%C || (set "EXITCODE=1" & goto :FINALLY)
)

echo --- Stopping running apps (if any) ---
for %%P in (APP_A.exe APP_B.exe IoboardServer.exe) do (
  taskkill /IM %%P /F >nul 2>nul
)

echo === Publish Emu/Server=Release, Apps=Debug ===
call "%ROOT%publish_all.bat" Release Debug || (set "EXITCODE=1" & goto :FINALLY)

REM --- Artifacts check ---
set "EMU_DLL=%ROOT%APP\publish\IoboardEmulator.dll"
set "APP_A_EXE=%ROOT%APP\publish\APP_A\APP_A.exe"
set "APP_B_EXE=%ROOT%APP\publish\APP_B\APP_B.exe"
set "SVR_EXE=%ROOT%IoboardServer\publish\win-x64\Release\IoboardServer.exe"

for %%F in ("%EMU_DLL%" "%APP_A_EXE%" "%APP_B_EXE%" "%SVR_EXE%") do (
  if not exist %%F (
    echo [ERROR] Missing artifact: %%~fF
    set "EXITCODE=1"
    goto :FINALLY
  )
)

echo.
echo [OK] All artifacts present:
echo   %EMU_DLL%
echo   %APP_A_EXE%
echo   %APP_B_EXE%
echo   %SVR_EXE%

:FINALLY
echo.
if "%EXITCODE%"=="0" (
  echo === DONE: success ===
) else (
  echo === DONE: failed (EXITCODE=%EXITCODE%) ===
)

if not defined NOPAUSE (
  echo.
  echo Press any key to close...
  pause >nul
)
exit /b %EXITCODE%
