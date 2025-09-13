@echo off
setlocal EnableExtensions
rem File : ci_check.bat
rem Ver  : v1.0 (2025-09-12 JST)
rem Why  : Clean → Build → Publish の一連を既存順序で実行。等価リファクタは行わない。

cd /d "%~dp0"

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

echo === Build Debug and Release ===

echo --- Building APP_A [Debug]
dotnet build "APP\APP_A\APP_A.csproj" -c Debug -r win-x64 || goto :err

echo --- Building APP_B [Debug]
dotnet build "APP\APP_B\APP_B.csproj" -c Debug -r win-x64 || goto :err

echo --- Building IoboardEmulator [Release]
dotnet build "IoboardEmulator\IoboardEmulator.csproj" -c Release -r win-x64 || goto :err

echo --- Building IoboardServer [Release]
dotnet build "IoboardServer\IoboardServer.csproj" -c Release -r win-x64 || goto :err

echo --- Stopping running apps (if any)
taskkill /f /im APP_A.exe >nul 2>nul
taskkill /f /im APP_B.exe >nul 2>nul
taskkill /f /im IoboardServer.exe >nul 2>nul

echo === Publish Emu/Server=Release, Apps=Debug ===
call "%~dp0publish_all.bat" || goto :err

echo === DONE ===
exit /b 0

:err
echo *** ERROR in ci_check.bat ***
exit /b 1
