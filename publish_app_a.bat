@echo off
setlocal EnableExtensions EnableDelayedExpansion
cd /d "%~dp0"

set RID=win-x64
set CFG=Debug
set PROJ=APP\APP_A\APP_A.csproj
set OUT=APP\publish\APP_A

echo === Publish APP_A (%RID%, %CFG%) ===
dotnet publish "%PROJ%" -c %CFG% -r %RID% -o "%OUT%" ^
  -p:SelfContained=true -p:PublishSingleFile=true || goto :err

for %%F in ("%OUT%\APP_A.exe") do set SIZE=%%~zF
echo --- Output: "%cd%\%OUT%\APP_A.exe" (%SIZE% bytes)
exit /b 0

:err
echo *** ERROR in publish_app_a.bat ***
exit /b 1
