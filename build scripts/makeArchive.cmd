@echo off
setlocal
set /p VERSION=Print software version (for example 11-1): 
if "%VERSION%"=="" (
    echo Sorry, wrong version number.
    exit /b 1
)

set "OUTPUT_X64=../builds/%VERSION%-x64-win.zip"
set "OUTPUT_X86=../builds/%VERSION%-x86-win.zip"
set "OUTPUT_X64l=../builds/%VERSION%-x64-linux.zip"
set SOURCE_X64="../builds/x64 full"
set SOURCE_X86="../builds/x86 full"
set SOURCE_X64l="../builds/x64 linux"
set SOURCE_WWW="../builds/wwwroot"

rem Удаляем старые архивы с теми же именами если существуют
if exist "%OUTPUT_X64%" del "%OUTPUT_X64%"
if exist "%OUTPUT_X86%" del "%OUTPUT_X86%"
if exist "%OUTPUT_X64l%" del "%OUTPUT_X64l%"

rem Проверяем наличие директорий
if not exist %SOURCE_X64% (
    echo Директория %SOURCE_X64% not found
    exit /b 1
)
if not exist %SOURCE_X64l% (
    echo Директория %SOURCE_X64l% not found
    exit /b 1
)
if not exist %SOURCE_X86% (
    echo Директория %SOURCE_X86% not found
    exit /b 1
)

rem Создаем ZIP архивы с именем версия-платформа.zip
powershell -Command "Compress-Archive -Path '%SOURCE_X64%\fmu-api.exe', '%SOURCE_WWW%' -DestinationPath '%OUTPUT_X64%'"
powershell -Command "Compress-Archive -Path '%SOURCE_X86%\fmu-api.exe', '%SOURCE_WWW%' -DestinationPath '%OUTPUT_X86%'"
powershell -Command "Compress-Archive -Path '%SOURCE_X64l%\fmu-api', '%SOURCE_WWW%' -DestinationPath '%OUTPUT_X64l%'"

echo Archives successfully created:
echo - %OUTPUT_X64%
echo - %OUTPUT_X86%
echo - %OUTPUT_X64l%

echo.
echo All publishing completed!
pause
