@echo off
setlocal

REM ==== Settings ====
set "RID=win-x64"
set "CONFIG=%~1"
if "%CONFIG%"=="" set "CONFIG=Debug"

REM ==== Paths ====
set "ROOT=%~dp0"
set "CSProj=%ROOT%APP\APP_B\APP_B.csproj"
set "OUTDIR=%ROOT%APP\publish\APP_B"

where dotnet >nul 2>nul || (echo [ERROR] dotnet not found & exit /b 1)
if not exist "%CSProj%" (echo [ERROR] Not found: %CSProj% & exit /b 1)

echo.
echo === Publish APP_B (%RID%, %CONFIG%) ===
dotnet publish "%CSProj%" -c "%CONFIG%" -r "%RID%" -o "%OUTDIR%" ^
 /p:PublishSingleFile=true /p:SelfContained=true /p:StripSymbols=true /p:InvariantGlobalization=true
if errorlevel 1 (echo [ERROR] dotnet publish failed & exit /b 1)

echo [OK] APP_B -> %OUTDIR%
exit /b 0
