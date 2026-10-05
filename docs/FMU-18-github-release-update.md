# FMU-18. Автообновление из релиза GitHub

Спецификация для разработки. Исходный запрос задачи:

- последний релиз: `https://api.github.com/repos/shrayky/fmu-api/releases/latest`
- владелец и имя репозитория задаются в `ApplicationInformation`
- имя архива стабильное, например `12-2-x64-win.zip` (так же собирает `build/Build.cs`)
- установка та же, что обновление с FMU-API-Central

## Поведение

Третья вкладка настроек, после «FMU-API-Central» и «Автообновление». Заголовок вкладки: «GitHub».

На вкладке:

- флажок «Использовать»
- интервал проверки, минуты
- таблица интервалов установки: начало и окончание, как расписание установки обновления с центра

Пока флажок выключен, интервал и таблица недоступны.

Значения по умолчанию:

| Поле | Значение |
|---|---|
| `Enabled` | `false` |
| `CheckIntervalMinutes` | `120` |
| `InstallSchedule` | пустой список |

Пустой список интервалов — ставить в любое время. Если интервалы заданы, скачивание и установка выполняются только когда текущее локальное время попадает в один из них. Вне окна воркер пишет в лог, что обновление отложено, архив не качает и ждёт следующей проверки.

Одна служба на магазин. При интервале 2 часа это 12 запросов к GitHub в сутки. Лимит API без токена — 60 запросов в час на один IP.

Первая проверка после старта процесса — через 5 минут в Release и через 1 минуту в Debug, как у `CentralServerExchangeWorker`. Дальше проверка каждые `CheckIntervalMinutes`. Цикл раз в минуту смотрит, наступило ли время следующей проверки.

Каталожное автообновление (`AutoUpdateWorker`, вкладка «Автообновление») не меняется.

Токен и адрес репозитория в настройках не появляются. Репозиторий публичный.

## Слои

| Часть | Проект | Зачем здесь |
|---|---|---|
| Константы репозитория, настройки, выбор архива, проверка окна расписания, интерфейс установщика | `src/Core/FmuApiDomain` | Правило без HTTP и без распаковки |
| Реализация установки zip | `src/Infrastructure/CentralServerExchange` | Распаковка и `--install` уже здесь |
| Запрос релиза, скачивание, воркер | `src/Infrastructure/GitHubReleaseUpdate` | Отдельный внешний контур, свой `HttpClient` |
| Третья вкладка | `src/Presentation/WebApi/wwwroot/js/modules/settings` | Рядом с двумя существующими вкладками |

`GitHubReleaseUpdate` ссылается только на `FmuApiDomain`. На `CentralServerExchange` и `AutoUpdateWorkerService` ссылок нет: установщик приходит через DI по интерфейсу из домена.

Новый проект подключается так же, как остальные инфраструктурные:

- `dotnet sln FMU-API.sln add src/Infrastructure/GitHubReleaseUpdate/GitHubReleaseUpdate.csproj`
- `ProjectReference` в `src/Presentation/WebApi/WebApi.csproj`
- вызов регистрации в `src/Presentation/WebApi/Program.cs` рядом с `AutoUpdateRegistrationExtension.AddService`

Каркас проекта: `net10.0`, `ImplicitUsings`, `Nullable`, пакеты `CSharpFunctionalExtensions`, `Microsoft.Extensions.Hosting.Abstractions`, `Microsoft.Extensions.Http`.

## Домен

`src/Core/FmuApiDomain/Constants/ApplicationInformation.cs`:

```csharp
public const string GitHubOwner = "shrayky";
public const string GitHubRepository = "fmu-api";
```

Адрес метаданных релиза:

`https://api.github.com/repos/{GitHubOwner}/{GitHubRepository}/releases/latest`

`src/Core/FmuApiDomain/Configuration/Options/GitHubReleaseUpdateOptions.cs`:

```csharp
public class GitHubReleaseUpdateOptions
{
    public bool Enabled { get; set; }
    public int CheckIntervalMinutes { get; set; } = 120;
    public List<ScheduleTime> InstallSchedule { get; set; } = [];
}
```

`ScheduleTime` уже есть в `CentralServerConnectionProperties.cs` (`id`, `beginTime`, `endTime`). Новый тип интервала не заводить.

В `Parameters` добавить свойство `GitHubReleaseUpdate`. Старый `config.json` без поля поднимается значениями по умолчанию, отдельная миграция не нужна.

Проверка окна — статический метод рядом с `ScheduleTime`, его вызывают и `SoftwareUpdateDownloadService`, и воркер GitHub. Правило переносится из приватного `IsWithinSchedule` без изменения смысла:

- список пуст — установка разрешена
- `BeginTime <= EndTime` — текущее время внутри отрезка, включая границы
- `BeginTime > EndTime` — интервал через полночь

Выбор архива — тип в домене, без HTTP. Имя файла: `{версия}-{сборка}-{архитектура}-{ос}.zip`.

Архитектура процесса: `x64` или `x86` (`Environment.Is64BitProcess`). ОС: `win` или `linux` (`OperatingSystem.IsWindows`).

Брать файл текущей архитектуры и ОС, у которого пара (версия, сборка) больше `ApplicationInformation.AppVersion` и `ApplicationInformation.Assembly`. Сначала сравнивается версия, при равенстве — сборка. Оба числа целые.

Для запущенной службы 12.2:

- `12-2-x64-win.zip` — пропуск, это текущая сборка
- `12-1-x64-win.zip` — пропуск, старше
- `12-3-x64-win.zip` и `13-1-x64-win.zip` — кандидат
- `12-3-x86-win.zip` и `12-3-x64-linux.zip` на Windows x64 — пропуск

Если подходящих файлов несколько, берётся максимальная пара (версия, сборка).

Скачивание идёт по `browser_download_url` выбранного файла. Поля релиза, которые читает клиент: `assets[].name`, `assets[].browser_download_url`.

## Установщик

Интерфейс в домене:

```csharp
public interface ISoftwarePackageInstaller
{
    Task<Result> InstallAsync(string zipPath, string sha256);
}
```

Реализация — класс в `CentralServerExchange`, `[AutoRegisterService(ServiceLifetime.Singleton)]`. В него переносятся из `SoftwareUpdateDownloadService` методы установки Windows и Linux, проверка путей в zip и запись `checksum.txt`. `SoftwareUpdateDownloadService.InstallUpdate` вызывает этот класс. Поведение обновления с центра не меняется: проверка записей архива, распаковка, `fmu-api.exe --install --checksum {sha256} --waitForPid {pid}` либо копирование версий продукта, если host в пакете нет.

Воркер GitHub считает SHA-256 скачанного zip и передаёт его в `InstallAsync`. Перед вызовом сравнивает хэш с `checksum.txt` в каталоге данных службы (`CommonApplicationData` / `ApplicationInformation.Manufacture` / `ApplicationInformation.AppName`). Совпадение означает, что этот пакет уже установлен, повторный запуск установщика не нужен.

## Проект GitHubReleaseUpdate

Состав:

- регистрация `AddService`: hosted service и именованный `HttpClient`
- клиент релиза: GET метаданных и GET архива
- `GitHubReleaseUpdateWorker : BackgroundService`

`HttpClient`:

- `User-Agent: FMU-API` — без него GitHub отвечает 403
- `Accept: application/vnd.github+json` на запрос метаданных
- таймаут метаданных 30 секунд
- таймаут скачивания zip 10 минут
- докачка Range не делается: при ошибке следующий запрос через интервал

Архив сохраняется в `%TEMP%/FMU-API/updates`.

Один `try` на проход проверки. Ошибка сети или разбора логируется, процесс службы не завершается, следующая попытка — по интервалу.

Порядок прохода, если `Enabled`:

1. Запросить `releases/latest`.
2. Выбрать архив новее текущей версии для этой ОС и архитектуры. Нет кандидата — выйти из прохода.
3. Проверить `InstallSchedule`. Вне окна — запись в лог и выход без скачивания.
4. Скачать zip.
5. Посчитать SHA-256. Хэш совпал с `checksum.txt` — не ставить.
6. Вызвать `ISoftwarePackageInstaller.InstallAsync`.

Если `Enabled` равен `false`, запроса к GitHub нет. Следующее время проверки всё равно сдвигается на интервал, как у обмена с центра.

Комментарий на русском — у новых классов и у интерфейса установщика. У методов комментарий только там, где из сигнатуры не ясно правило (сравнение версии, пустое расписание). Вложенных `try` нет.

## Интерфейс

`src/Presentation/WebApi/wwwroot/js/modules/settings/elements/centralServerAutoUpdateSettings.js` — третья ячейка `tabview`, заголовок «GitHub», тело из нового модуля `githubReleaseUpdate.js`.

Имена полей формы, чтобы `complexData` записал объект настроек:

- `githubReleaseUpdate.enabled`
- `githubReleaseUpdate.checkIntervalMinutes`
- `githubReleaseUpdate.installSchedule`

Таблица интервалов выносится в общий модуль с префиксом id. Его используют вкладка GitHub и вкладка центра (`centralServerConnection.js`). Свои id обязательны: форма центра занимает `SchedulerUpdateInstall`, `schedulerBeginTime`, `schedulerEndTime`. Подсказка та же: если список пуст, обновление устанавливается в любое время. Проверка в форме: оба времени заданы, начало меньше окончания — как сейчас на вкладке центра.

Подписи на русском, в том же стиле, что блок центра.

## Тесты

Проект `src/tests/FmuApiApplication.Tests`, xUnit. Выбор архива и окно расписания живут в домене, новый инфраструктурный проект в тесты подключать не нужно.

- Windows x64, текущие 12 и 2: выбирается `12-3-x64-win.zip`, отбрасываются `12-2`, `12-1`, `x86`, `linux`
- `13-1-x64-win.zip` новее `12.2`
- несколько подходящих файлов — берётся старшая пара версии и сборки
- пустое расписание разрешает любое время
- интервал `02:00:00`–`04:00:00` разрешает `03:00`, запрещает `05:00`
- интервал через полночь (`22:00:00`–`02:00:00`) разрешает `23:00` и `01:00`, запрещает `12:00`

## Вне этой задачи

- замена каталожного автообновления
- токен GitHub и поле адреса в настройках
- докачка архива по HTTP Range
- смена имён архивов в `build/Build.cs`
