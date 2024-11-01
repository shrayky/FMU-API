# FMU-API
Свободная реализация API Frontol Mark Unit.

Используется в связке с Frontol 6 или Frontol xPos для разрешительного режима продажи маркированной продукции.

## Компиляция

Для сборки проекта выполните следующую команду:

```bash
dotnet publish FmuApi.API/FmuApiAPI.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```

## Быстрая установка

1. Установите предварительные требования:
   - Frontol (не обязательно)
   - CouchDB

2. Установите FMU-API через командную строку администратора:
   ```bash
   C:\FMU-API\fmu-api.exe --install
   ```

3. Настройте через веб-интерфейс http://localhost:2578/

Подробная инструкция по установке с иллюстрациями доступна в файле [docs/installation.pdf](docs/installation.pdf)

## Поддержка

Если у вас возникли проблемы с установкой или работой FMU-API, обратитесь за помощью в телеграм-канал: https://t.me/frntlsc
