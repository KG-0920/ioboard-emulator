@echo off
setlocal enabledelayedexpansion

REM ==== Settings ====
set "RID=win-x64"
set "CONFIG=%~1"
if "%CONFIG%"=="" set "CONFIG=Release"

REM ==== Paths ====
set "ROOT=%~dp0"
set "CSProj=%ROOT%IoboardEmulator\IoboardEmulator.csproj"
set "OUTDIR=%ROOT%IoboardEmulator\publish\%RID%\%CONFIG%"
set "DLLNAME=IoboardEmulator.dll"
set "APP_PUBLISH=%ROOT%APP\publish"

REM ==== Sanity checks ====
where dotnet >nul 2>nul || (echo [ERROR] dotnet not found & exit /b 1)
if not exist "%CSProj%" (echo [ERROR] Not found: %CSProj% & exit /b 1)

echo.
echo === Publish IoboardEmulator (AOT, %RID%, %CONFIG%) ===
dotnet publish "%CSProj%" -c "%CONFIG%" -r "%RID%" -o "%OUTDIR%" ^
 /p:PublishAot=true /p:NativeLib=Shared /p:SelfContained=true /p:StripSymbols=true /p:InvariantGlobalization=true
if errorlevel 1 (echo [ERROR] dotnet publish failed & exit /b 1)

if not exist "%OUTDIR%\%DLLNAME%" (echo [ERROR] Missing %DLLNAME% at %OUTDIR% & exit /b 1)

echo.
echo === Copy emulator DLL to APP/publish (parent folder strategy for Debug) ===
if not exist "%APP_PUBLISH%" mkdir "%APP_PUBLISH%"
copy /Y "%OUTDIR%\%DLLNAME%" "%APP_PUBLISH%\%DLLNAME%" >nul
if errorlevel 1 (echo [ERROR] Copy failed & exit /b 1)

echo [OK] %DLLNAME% -> %APP_PUBLISH%
exit /b 0
