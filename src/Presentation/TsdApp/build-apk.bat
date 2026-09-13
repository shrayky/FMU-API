@echo off
chcp 65001 >nul
setlocal
set "ROOT=%~dp0"
set "PATH=D:\sdk\flutter\bin;%PATH%"
if not defined JAVA_HOME set "JAVA_HOME=C:\Program Files\Android\Android Studio\jbr"
set "PATH=%JAVA_HOME%\bin;%PATH%"

cd /d "%ROOT%"
call flutter build apk --release --split-per-abi --target-platform android-arm,android-arm64
if errorlevel 1 exit /b 1

set "OUT=%ROOT%build\app\outputs\flutter-apk"
powershell -NoProfile -ExecutionPolicy Bypass -File "%ROOT%android\resign-v1.ps1" "%OUT%\app-armeabi-v7a-release.apk"
if errorlevel 1 exit /b 1
powershell -NoProfile -ExecutionPolicy Bypass -File "%ROOT%android\resign-v1.ps1" "%OUT%\app-arm64-v8a-release.apk"
if errorlevel 1 exit /b 1

echo.
echo 32-bit: %OUT%\app-armeabi-v7a-release.apk
echo 64-bit: %OUT%\app-arm64-v8a-release.apk
