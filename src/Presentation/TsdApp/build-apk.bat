@echo off
chcp 65001 >nul
setlocal

if /i "%TSD_APK_SIGNING%"=="debug" (
    echo Подпись: debug-ключ этой машины.
) else (
    echo Подпись: release-ключ из android\key.properties.
)

:askBase
set "BASE="
ver >nul
set /p BASE=Базовый номер сборки больше 3: 
if errorlevel 1 exit /b 1
if "%BASE%"=="" goto askBase
echo %BASE%| findstr /r "^[0-9][0-9]*$" >nul
if errorlevel 1 (
    echo Базовый номер сборки: только цифры.
    goto askBase
)
if %BASE% LEQ 3 (
    echo База должна быть больше 3: на ТСД уже стоят versionCode 2003 и 1003.
    goto askBase
)

:askVersion
set "VERSION="
ver >nul
set /p VERSION=versionName: 
if errorlevel 1 exit /b 1
if "%VERSION%"=="" goto askVersion
echo %VERSION%| findstr /r "^[0-9][0-9.]*$" >nul
if errorlevel 1 (
    echo versionName: только цифры и точки.
    goto askVersion
)

set /a ARM64=%BASE%+2000
set /a V7A=%BASE%+1000

set "ROOT=%~dp0"
set "PATH=D:\sdk\flutter\bin;%PATH%"
if not defined JAVA_HOME set "JAVA_HOME=C:\Program Files\Android\Android Studio\jbr"
set "PATH=%JAVA_HOME%\bin;%PATH%"

cd /d "%ROOT%"
call flutter build apk --release --split-per-abi --target-platform android-arm,android-arm64 --build-name "%VERSION%" --build-number "%BASE%"
if errorlevel 1 exit /b 1

set "OUT=%ROOT%build\app\outputs\flutter-apk"
echo.
echo 32-bit: %OUT%\app-armeabi-v7a-release.apk (versionCode %V7A%)
echo 64-bit: %OUT%\app-arm64-v8a-release.apk (versionCode %ARM64%)
echo versionName: %VERSION%
if /i "%TSD_APK_SIGNING%"=="debug" (
    echo Подпись: debug-ключ этой машины. Обновление встанет поверх сборок с этим сертификатом.
) else (
    echo Подпись: android\key.properties. Эти два файла заливает в GitHub владелец keystore. Копию ключа хранить отдельно от репозитория.
)
