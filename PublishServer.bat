@echo off
setlocal

set SOLUTION=ioboard-emulator.sln
set PROJECT=IoboardServer\IoboardServer.csproj
set CONF=Release
set OUTDIR=publish

if exist "%OUTDIR%" rmdir /s /q "%OUTDIR%"

echo === Restore ===
dotnet restore "%SOLUTION%" || exit /b 1

echo === Publish x64 ===
dotnet publish "%PROJECT%" -c %CONF% -r win-x64 --self-contained false -o "%OUTDIR%\win-x64" || exit /b 1

echo === Publish x86 ===
dotnet publish "%PROJECT%" -c %CONF% -r win-x86 --self-contained false -o "%OUTDIR%\win-x86" || exit /b 1

REM ルートの IoLogConfig.xml を同梱（無ければ雛形を生成）
set LOGCFG=IoLogConfig.xml
if exist "%LOGCFG%" (
  copy /y "%LOGCFG%" "%OUTDIR%\win-x64\%LOGCFG%" >nul
  copy /y "%LOGCFG%" "%OUTDIR%\win-x86\%LOGCFG%" >nul
) else (
  echo ^<LogConfig^>^<Level^>Info^</Level^>^</LogConfig^> > "%OUTDIR%\win-x64\IoLogConfig.xml"
  echo ^<LogConfig^>^<Level^>Info^</Level^>^</LogConfig^> > "%OUTDIR%\win-x86\IoLogConfig.xml"
)

echo === Done ===
echo Output: %OUTDIR%\win-x64, %OUTDIR%\win-x86

endlocal
