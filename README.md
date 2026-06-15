# FMU-API
Свободная реализация API Frontol Mark Unit.

Используется в связке с Frontol 6 или Frontol xPos для разрешительного режима продажи маркированной продукции.

## Компиляция

Для сборки проекта выполните следующую команду:

```power shell
dotnet publish FmuApi.API/FmuApiAPI.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```

## Быстрая установка

1. Установите FMU-API через командную строку администратора:
   ```bash
   C:\FMU-API\fmu-api.exe --install
   ```
2. Опиционально
   - Frontol
   - Apache CouchDB

3. Настройте через веб-интерфейс http://localhost:2578/

Инструкция по установке доступна в [wiki](https://github.com/shrayky/FMU-API/wiki/%D0%A3%D1%81%D1%82%D0%B0%D0%BD%D0%BE%D0%B2%D0%BA%D0%B0-fmu%E2%80%90api)

Подробная инструкция по установке и настройке fmu-api с фронтол с иллюстрациями доступна в файле [docs/installation.pdf](docs/installation.pdf)

## Поддержка

Если у вас возникли проблемы с установкой или работой FMU-API, обратитесь за помощью в телеграм-канал: https://t.me/frntlsc
