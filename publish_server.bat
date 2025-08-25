@echo off
setlocal

REM ==== Settings ====
set "RID=win-x64"
set "CONFIG=%~1"
if "%CONFIG%"=="" set "CONFIG=Release"

REM ==== Paths ====
set "ROOT=%~dp0"
set "CSProj=%ROOT%IoboardServer\IoboardServer.csproj"
set "OUTDIR=%ROOT%IoboardServer\publish\%RID%\%CONFIG%"
set "IOLOG=%ROOT%IoLogConfig.xml"

where dotnet >nul 2>nul || (echo [ERROR] dotnet not found & exit /b 1)
if not exist "%CSProj%" (echo [ERROR] Not found: %CSProj% & exit /b 1)

echo.
echo === Publish IoboardServer (SingleFile, %RID%, %CONFIG%) ===
dotnet publish "%CSProj%" -c "%CONFIG%" -r "%RID%" -o "%OUTDIR%" ^
 /p:PublishSingleFile=true /p:SelfContained=true /p:StripSymbols=true /p:InvariantGlobalization=true
if errorlevel 1 (echo [ERROR] dotnet publish failed & exit /b 1)

if exist "%IOLOG%" (
  copy /Y "%IOLOG%" "%OUTDIR%\IoLogConfig.xml" >nul
  echo [OK] Copied IoLogConfig.xml
) else (
  echo [WARN] IoLogConfig.xml not found at repo root
)

echo [OK] IoboardServer -> %OUTDIR%
exit /b 0
