@echo off
setlocal
set /p VERSION=Print software version (for example 11-1): 
if "%VERSION%"=="" (
    echo Sorry, wrong version number.
    exit /b 1
)

set "OUTPUT_X64l=../builds/%VERSION%-x64-linux.zip"
set SOURCE_X64l="../builds/x64 linux"
set SOURCE_WWW="../builds/wwwroot"

rem Удаляем старые архивы с теми же именами если существуют
if exist "%OUTPUT_X64l%" del "%OUTPUT_X64l%"

rem Проверяем наличие директорий
if not exist %SOURCE_X64l% (
    echo Директория %SOURCE_X64l% not found
    exit /b 1
)

rem Создаем ZIP архивы с именем версия-платформа.zip
powershell -Command "Compress-Archive -Path '%SOURCE_X64l%\fmu-api', '%SOURCE_X64l%\wwwroot' -DestinationPath '%OUTPUT_X64l%'"

echo Archives successfully created:
echo - %OUTPUT_X64l%

echo.
echo All publishing completed!
pause
