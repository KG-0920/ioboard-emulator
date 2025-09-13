@echo off
setlocal EnableExtensions
rem File : publish_emulator_aot.bat
rem Ver  : v1.0 (2025-09-12 JST)
rem Why  : IoboardEmulator を Release/AOT(shared DLL) で発行。等価リファクタは行わない。

cd /d "%~dp0"

rem clean（既存構造に合わせて最低限）
rd /s /q "IoboardEmulator\bin"  2>nul
rd /s /q "IoboardEmulator\obj"  2>nul
rd /s /q "APP\publish"          2>nul

rem AOT 発行（NativeLib=Shared）
dotnet publish "IoboardEmulator\IoboardEmulator.csproj" ^
  -c Release -r win-x64 ^
  -p:PublishAot=true -p:NativeLib=Shared -p:SelfContained=true -p:StripSymbols=true ^
  -o "APP\publish" || goto :err

if not exist "APP\publish\IoboardEmulator.dll" goto :err

for %%F in ("APP\publish\IoboardEmulator.dll") do set SIZE=%%~zF
echo --- Output: "%cd%\APP\publish\IoboardEmulator.dll" (%SIZE% bytes)
exit /b 0

:err
echo *** ERROR in publish_emulator_aot.bat ***
exit /b 1
