@echo off
setlocal EnableExtensions EnableDelayedExpansion
cd /d "%~dp0"

set RID=win-x64
set CFG=Release
set PROJ=IoboardServer\IoboardServer.csproj
set OUT=IoboardServer\publish\win-x64\Release

echo --- Stopping running server (if any) ---
taskkill /f /im IoboardServer.exe >nul 2>nul

echo === Publish IoboardServer [RID=%RID% CONFIG=%CFG%] (SingleFile, SelfContained) ===
dotnet publish "%PROJ%" -c %CFG% -r %RID% -o "%OUT%" ^
  -p:SelfContained=true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true || goto :err

for %%F in ("%OUT%\IoboardServer.exe") do set SIZE=%%~zF
echo --- Output: "%cd%\%OUT%\IoboardServer.exe" (%SIZE% bytes)
echo [OK] Published single-file self-contained server.
exit /b 0

:err
echo *** ERROR in publish_server.bat ***
exit /b 1
