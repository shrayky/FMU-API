@echo off
chcp 65001 >nul
setlocal
set TSD_APK_SIGNING=debug
call "%~dp0build-apk.bat"
exit /b %ERRORLEVEL%
