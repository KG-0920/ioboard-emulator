@echo off
setlocal EnableExtensions EnableDelayedExpansion
cd /d "%~dp0"

set RID=win-x64
set CFG=Debug
set PROJ=APP\APP_B\APP_B.csproj
set OUT=APP\publish\APP_B

echo === Publish APP_B (%RID%, %CFG%) ===
dotnet publish "%PROJ%" -c %CFG% -r %RID% -o "%OUT%" ^
  -p:SelfContained=true -p:PublishSingleFile=true || goto :err

for %%F in ("%OUT%\APP_B.exe") do set SIZE=%%~zF
echo --- Output: "%cd%\%OUT%\APP_B.exe" (%SIZE% bytes)
exit /b 0

:err
echo *** ERROR in publish_app_b.bat ***
exit /b 1
