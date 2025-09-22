@echo off
setlocal
powershell -NoProfile -ExecutionPolicy Bypass -File "HashCheck.ps1"
set ERR=%ERRORLEVEL%
if not "%ERR%"=="0" (
  echo [ERR] Hash check failed (code=%ERR%)
  exit /b %ERR%
)
echo [OK] Hash check passed.
exit /b 0
