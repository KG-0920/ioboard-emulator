@echo off
setlocal

REM === Settings ===
set SOLUTION=ioboard-emulator.sln
set PROJECT=IoboardServer\IoboardServer.csproj
set CONF=Release
set OUTDIR=publish

REM Clean output
if exist "%OUTDIR%" rmdir /s /q "%OUTDIR%"

echo.
echo === Restore ===
dotnet restore "%SOLUTION%"
if errorlevel 1 exit /b 1

echo.
echo === Publish x64 ===
dotnet publish "%PROJECT%" -c %CONF% -r win-x64 --self-contained false -o "%OUTDIR%\win-x64"
if errorlevel 1 exit /b 1

echo.
echo === Publish x86 ===
dotnet publish "%PROJECT%" -c %CONF% -r win-x86 --self-contained false -o "%OUTDIR%\win-x86"
if errorlevel 1 exit /b 1

REM === IoLogConfig.xml の同梱（ルート配置前提） ===
set LOGCFG=IoLogConfig.xml
if exist "%LOGCFG%" (
  copy /y "%LOGCFG%" "%OUTDIR%\win-x64\%LOGCFG%" >nul
  copy /y "%LOGCFG%" "%OUTDIR%\win-x86\%LOGCFG%" >nul
) else (
  echo ^<LogConfig^>^<Level^>Info^</Level^>^</LogConfig^> > "%OUTDIR%\win-x64\IoLogConfig.xml"
  echo ^<LogConfig^>^<Level^>Info^</Level^>^</LogConfig^> > "%OUTDIR%\win-x86\IoLogConfig.xml"
)

echo.
echo === Done ===
echo Output:
echo   %OUTDIR%\win-x64
echo   %OUTDIR%\win-x86

endlocal
