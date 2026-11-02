@echo off
chcp 65001 >nul
setlocal
if "%~1"=="" (
    echo Укажите базовый номер сборки больше 3 и versionName. Пример: build-apk.bat 4 1.0.3
    exit /b 1
)
if "%~2"=="" (
    echo Укажите versionName. Пример: build-apk.bat 4 1.0.3
    exit /b 1
)

echo %~1| findstr /r "^[0-9][0-9]*$" >nul
if errorlevel 1 (
    echo Базовый номер сборки: только цифры. Пример: build-apk.bat 4 1.0.3
    exit /b 1
)
if %~1 LEQ 3 (
    echo База должна быть больше 3: на ТСД уже стоят versionCode 2003 и 1003. Пример: build-apk.bat 4 1.0.3
    exit /b 1
)
echo %~2| findstr /r "^[0-9][0-9.]*$" >nul
if errorlevel 1 (
    echo versionName: только цифры и точки. Пример: build-apk.bat 4 1.0.3
    exit /b 1
)

set /a ARM64=%~1+2000
set /a V7A=%~1+1000

set "ROOT=%~dp0"
set "PATH=D:\sdk\flutter\bin;%PATH%"
if not defined JAVA_HOME set "JAVA_HOME=C:\Program Files\Android\Android Studio\jbr"
set "PATH=%JAVA_HOME%\bin;%PATH%"

cd /d "%ROOT%"
call flutter build apk --release --split-per-abi --target-platform android-arm,android-arm64 --build-name "%~2" --build-number "%~1"
if errorlevel 1 exit /b 1

set "OUT=%ROOT%build\app\outputs\flutter-apk"
echo.
echo 32-bit: %OUT%\app-armeabi-v7a-release.apk (versionCode %V7A%)
echo 64-bit: %OUT%\app-arm64-v8a-release.apk (versionCode %ARM64%)
echo versionName: %~2
echo Эти два файла заливает в GitHub владелец keystore. Копию ключа хранить отдельно от репозитория.
