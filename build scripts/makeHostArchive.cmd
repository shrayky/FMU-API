@echo off
chcp 65001 >nul
setlocal EnableDelayedExpansion

set /p VERSION=Print software version (for example 12-1): 
if "%VERSION%"=="" (
    echo Sorry, wrong version number.
    exit /b 1
)

rem Host ищет каталог версии через Version.TryParse: 12-1 -> 12.1
set "VERSION_FOLDER=%VERSION:-=.%"

set "SCRIPT_DIR=%~dp0"
set "BUILDS=%SCRIPT_DIR%..\builds"
set "PRODUCT_NAME=fmu-api-check"
set "HOST_EXE=fmu-api.exe"
set "PRODUCT_EXE=%PRODUCT_NAME%.exe"

set "OUTPUT_X64=%BUILDS%\%VERSION%-x64-win.zip"
set "OUTPUT_X86=%BUILDS%\%VERSION%-x86-win.zip"

set "HOST_X64=%BUILDS%\ha-win-x64"
set "HOST_X86=%BUILDS%\ha-win-x86"
set "PRODUCT_X64=%BUILDS%\x64 full"
set "PRODUCT_X86=%BUILDS%\x86 full"

if not exist "%HOST_X64%\%HOST_EXE%" (
    echo Не найден host: %HOST_X64%\%HOST_EXE%
    exit /b 1
)
if not exist "%PRODUCT_X64%\%PRODUCT_EXE%" (
    echo Не найден продукт: %PRODUCT_X64%\%PRODUCT_EXE%
    exit /b 1
)
if not exist "%PRODUCT_X64%\wwwroot" (
    echo Не найден wwwroot: %PRODUCT_X64%\wwwroot
    exit /b 1
)

if exist "%OUTPUT_X64%" del "%OUTPUT_X64%"
if exist "%OUTPUT_X86%" del "%OUTPUT_X86%"

call :PackPlatform "%HOST_X64%" "%PRODUCT_X64%" "%OUTPUT_X64%"
if errorlevel 1 exit /b 1

set "DO_X86=0"
if exist "%HOST_X86%\%HOST_EXE%" if exist "%PRODUCT_X86%\%PRODUCT_EXE%" if exist "%PRODUCT_X86%\wwwroot" set "DO_X86=1"

if "%DO_X86%"=="1" (
    call :PackPlatform "%HOST_X86%" "%PRODUCT_X86%" "%OUTPUT_X86%"
    if errorlevel 1 exit /b 1
) else (
    echo Пропуск x86: нет %HOST_X86%\%HOST_EXE% или %PRODUCT_X86%\%PRODUCT_EXE%
)

echo Archives successfully created:
echo - %OUTPUT_X64%
if exist "%OUTPUT_X86%" echo - %OUTPUT_X86%

echo.
echo All publishing completed!
pause
exit /b 0

rem Собирает временную раскладку host + продукт\версия и пакует zip
:PackPlatform
set "HOST_DIR=%~1"
set "PRODUCT_DIR=%~2"
set "OUTPUT=%~3"
set "STAGING=%TEMP%\fmu-host-archive-%RANDOM%"

mkdir "%STAGING%\%PRODUCT_NAME%\%VERSION_FOLDER%"
if errorlevel 1 (
    echo Не удалось создать %STAGING%
    exit /b 1
)

copy /Y "%HOST_DIR%\%HOST_EXE%" "%STAGING%\%HOST_EXE%" >nul
if errorlevel 1 (
    echo Не удалось скопировать %HOST_EXE%
    rd /s /q "%STAGING%"
    exit /b 1
)

copy /Y "%PRODUCT_DIR%\%PRODUCT_EXE%" "%STAGING%\%PRODUCT_NAME%\%VERSION_FOLDER%\%PRODUCT_EXE%" >nul
if errorlevel 1 (
    echo Не удалось скопировать %PRODUCT_EXE%
    rd /s /q "%STAGING%"
    exit /b 1
)

xcopy /E /I /Q /Y "%PRODUCT_DIR%\wwwroot" "%STAGING%\%PRODUCT_NAME%\%VERSION_FOLDER%\wwwroot" >nul
if errorlevel 1 (
    echo Не удалось скопировать wwwroot
    rd /s /q "%STAGING%"
    exit /b 1
)

pushd "%STAGING%"
powershell -NoProfile -Command "Compress-Archive -LiteralPath 'fmu-api.exe','fmu-api-check' -DestinationPath '%OUTPUT%' -Force"
set "PACK_ERR=!errorlevel!"
popd

rd /s /q "%STAGING%"

if not "!PACK_ERR!"=="0" (
    echo Не удалось создать архив %OUTPUT%
    exit /b 1
)

echo Создан %OUTPUT%
exit /b 0
