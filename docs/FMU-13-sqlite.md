# FMU-13. Поддержка SQLite как хранилища приложения

Спецификация для разработки. Исходный запрос задачи:

> Добавить поддержку альтернативной СУБД (sqlite).

Хранилище собственных данных приложения сейчас одно — CouchDB. В нём лежат марки, документы, статистика проверок, пивные краны, документы и марки ГИС МТ, каталог GTIN. Контракты уже в `FmuApiDomain`, реализации — в `src/Infrastructure/CouchDb`. SQLite даёт тот же набор данных файлом, без отдельного сервера CouchDB.

Подключение к базе Frontol остаётся Firebird.

## Поведение

В настройках базы (`databaseConnection.js`) появляется провайдер: CouchDB или SQLite.

Пока выбран CouchDB, форма как сейчас: адрес, пользователь, пароль, кнопка Fauxton, пакетная обработка, таймаут, флажок лога.

При выборе SQLite скрываются адрес, пользователь, пароль и Fauxton. Остаются путь к файлу, размер пакета, таймаут и флажок лога. Размер пакета задаёт шаг переноса из CouchDB. Адрес, логин и пароль CouchDB из `config.json` не стираются: они нужны как источник переноса.

Пустой путь означает файл по умолчанию:

| ОС | Путь |
|---|---|
| Windows | `%ProgramData%\Automation\FMU-API\fmu-api.db` |
| Linux | `/var/lib/FMU-API/fmu-api.db` |

Каталог — `Folders.CommonApplicationDataFolder(ApplicationInformation.Manufacture, ApplicationInformation.AppName)`, туда же пишется файловая очередь документов.

При первой установке, когда файла конфигурации ещё нет, провайдер — SQLite и база включена. Путь к файлу пустой, поэтому берётся путь по умолчанию.

Старый `config.json` без поля провайдера читается как CouchDB.

Смена провайдера применяется после перезапуска службы: клиент и репозитории собираются в `Program.cs` один раз.

Строка мониторинга «Статус базы данных» не меняется: `On-line`, `Off-line`, `Disabled`. Для SQLite её заполняет свой воркер статуса тем же флагом `CouchDbOnline()`. Флаг не переименовывать: на него завязаны документы, марки, мониторинг и сброс офлайн-очереди.

### Перенос из CouchDB

Отдельная страница «Сервис» в боковом меню, пункт перед «Информация». На странице кнопка «Перенести данные из CouchDB в SQLite» и текст хода: имя текущей базы, сколько документов записано в ней, сколько баз из семи готово, текст ошибки.

Кнопка доступна, когда провайдер уже SQLite и сохранённое подключение CouchDB включено (`Enable`, адрес, пользователь, пароль). Иначе на странице текст, чего не хватает: выбрать SQLite и перезапустить службу либо заполнить подключение CouchDB.

Перед запуском — подтверждение. Копирование идёт в рабочую базу и может занять долго.

Повторное нажатие после успешного переноса снова обходит базы и дописывает только строки, которых ещё нет. Прерванный перенос продолжается с последнего `_id`.

CouchDB после копирования не очищается. Возврат провайдера на CouchDB подхватывает исходные данные.

## Слои

| Часть | Проект | Зачем здесь |
|---|---|---|
| Провайдер, путь SQLite, контракт переноса, состояние хода | `src/Core/FmuApiDomain` | Настройки и контракт без EF и без CouchDB |
| Репозитории CouchDB, чтение `_all_docs` | `src/Infrastructure/CouchDb` | Источник переноса, без знания о SQLite |
| Файл SQLite, репозитории, воркер статуса, сервис переноса | `src/Infrastructure/Sqlite` | EF Core и запись в файл |
| Форма базы, страница «Сервис», контроллер | `src/Presentation/WebApi` | Ввод настроек и кнопка |

`Sqlite` ссылается на `FmuApiDomain` и на `CouchDb`. Обратной ссылки нет. Ссылка на `CouchDb` нужна сервису переноса: `CouchClient`, `CouchDoc<T>`, `DatabaseNames`.

Новый проект подключается так же, как остальные инфраструктурные:

- `dotnet sln FMU-API.sln add src/Infrastructure/Sqlite/Sqlite.csproj`
- `ProjectReference` в `src/Presentation/WebApi/WebApi.csproj`
- регистрация в `src/Presentation/WebApi/Program.cs` рядом с `CouchDbServicesRegistration.AddService`

Каркас: `net10.0`, `ImplicitUsings`, `Nullable`. Пакеты без версии в csproj, версии в `Directory.Packages.props`:

- `Microsoft.EntityFrameworkCore.Sqlite` — `10.0.11`, рядом с уже указанным `Microsoft.EntityFrameworkCore`
- `Microsoft.Extensions.Hosting.Abstractions` — уже есть в props

## Домен

`src/Core/FmuApiDomain/Configuration/Options/DatabaseProvider.cs`:

```csharp
public enum DatabaseProvider
{
    CouchDb = 0,
    Sqlite = 1
}
```

В `CouchDbConnection`:

```csharp
public DatabaseProvider Provider { get; set; } = DatabaseProvider.CouchDb;

public string SqlitePath { get; set; } = string.Empty;
```

`ConfigurationIsEnabled`:

- `Provider == Sqlite` — `Enable` и непустой путь (пустая строка перед проверкой заменяется путём по умолчанию);
- иначе текущее правило: `Enable`, адрес, пользователь и пароль.

`src/Core/FmuApiDomain/Database/Interfaces/ICouchDbToSqliteImport.cs` — запуск и чтение хода. Состояние:

```csharp
public enum CouchDbImportPhase
{
    Idle,
    Running,
    Completed,
    Failed
}

public class CouchDbImportState
{
    public CouchDbImportPhase Phase { get; init; } = CouchDbImportPhase.Idle;
    public string CurrentDatabase { get; init; } = string.Empty;
    public int CopiedInCurrent { get; init; }
    public int CompletedDatabases { get; init; }
    public int TotalDatabases { get; init; } = 7;
    public string Error { get; init; } = string.Empty;
}
```

`Start` возвращает `Result`. Повторный вызов в фазе `Running` — неуспех.

## SQLite

Один `DbContext`. Схема создаётся при старте (`Database.EnsureCreated`). Сущности остаются документами: в каждой таблице колонка `Id` и колонка `Json` с телом сущности, плюс колонки, по которым сейчас идёт выборка. Индекс на каждую такую колонку.

| Таблица | Колонки помимо `Id` и `Json` | Зачем |
|---|---|---|
| `Marks` | `MarkId`, `ReqTimestamp` | префикс марки и список по убыванию `TrueApiAnswerProperties.ReqTimestamp`, как `MarkMangoQueryBuilder` |
| `Documents` | — | чтение и удаление по `Id` |
| `MarkCheckingStatistic` | `SGtin`, `CheckDay`, `CheckDate` | последние проверки и срезы по дню |
| `BeerOnTap` | — | чтение по `Id` |
| `GisMtDocuments` | `Number`, `LoadedAt` | отбор по номеру и дате загрузки |
| `GisMtMarks` | `SGtin`, `Cis`, `ProductGroup`, `InfoLoadedAt`, `Sold`, `ExpireDate` | поиск, страница по `InfoLoadedAt` и очистка проданных или просроченных |
| `GtinCatalog` | `Gtin` | поиск по GTIN |
| `ImportProgress` | `LastId`, `Completed` | ключ — имя базы CouchDB |

`ReqTimestamp`, `CheckDay`, `LoadedAt`, `InfoLoadedAt`, `Sold`, `ExpireDate` заполняются из сущности при записи, чтобы запрос не разбирал JSON.

Семь репозиториев реализуют те же интерфейсы, что CouchDB:

- `IMarkInformationRepository`
- `IDocumentRepository`
- `ICheckStatisticRepository`
- `IBeerOnTapRepository`
- `IGisMtDocumentRepository`
- `IGisMtMarkRepository`
- `IGtinCatalogRepository`

Поиск марки без строки — страница по `ReqTimestamp` по убыванию. Со строкой — `MarkId`, который начинается с введённого текста, разбор страницы через `MarkMangoQueryBuilder.ResolveSearchPagination`. Поиск марок ГИС МТ — вхождение строки в `SGtin` или `Cis` и точное совпадение `ProductGroup`, сортировка по `InfoLoadedAt` по убыванию. Очистка марок ГИС МТ — `InfoLoadedAt` старше порога и (`Sold` либо `ExpireDate` раньше текущего UTC), с тем же лимитом, что `GetExpiredForCleanup`.

Недоступная база возвращает те же пустые результаты и тексты ошибок, что текущие репозитории CouchDB. Признак доступности — `CouchDbOnline()`.

`SqliteStatusWorker`, интервал 10 секунд, как `CouchDbStatusWorker`. База выключена — `UpdateCouchDbState(false)`. Иначе открыть соединение с файлом: успех — `true`, исключение — `false`. Смену состояния писать в лог.

При провайдере SQLite не стартуют `CouchDbStatusWorker`, `DatabaseCompactWorker`, `CouchDbMigrationTo102Worker` и индексация mango.

`FileOfflineDocumentStore` и `ClearingStorageOfStatisticsWorker` регистрируются при любом провайдере. Сейчас оба поднимаются внутри `CouchDbServicesRegistration.AddService`. Вынести их в отдельный метод этой регистрации и вызывать его из `Program.cs` всегда, чтобы при SQLite они не пропали и не зарегистрировались дважды.

## Перенос

`CouchDbToSqliteImport` в `src/Infrastructure/Sqlite`. Клиент CouchDB создаёт сам по сохранённым адресу, логину и паролю (`CouchClient`, те же `CouchClientOptions`, что в `CouchDbServicesRegistration`: таймаут `QueryTimeoutSeconds`, `JsonSerializerOptions.Web`). Клиент из DI не использовать: при выбранном SQLite его нет.

Семь баз из `DatabaseNames.Names()`, по очереди:

1. `fmu-api-marks`
2. `fmu-api-documents`
3. `fmu-api-mark-checking-statistic`
4. `fmu-api-beer-on-taps`
5. `fmu-api-gis-mt-documents`
6. `fmu-api-gis-mt-marks`
7. `fmu-api-gtin-catalog`

Обход — `GET _all_docs` с `include_docs=true` через `NewRequest()`, тем же приёмом, которым `ExecuteMangoCountAsync` ходит в `_find`. Размер пакета — `BulkBatchSize`. Документы с `_id`, начинающимся на `_design`, пропускаются. Следующая страница: `startkey` равен последнему `_id`, `skip=1`, чтобы не прочитать его снова.

Тело документа — `CouchDoc<T>`, поле `data`. Вставка в SQLite, только если строки с таким `Id` ещё нет. Продажа, записанная в SQLite пока идёт копирование, не затирается.

После каждого пакета обновляется `ImportProgress.LastId` этой базы и счётчики `CouchDbImportState`. Имя базы и число документов пакета пишутся в лог. Когда база дочитана, `Completed = true`.

Если у базы `Completed == false` и `LastId` непустой, кнопка продолжает с этого `_id`. Если все семь баз `Completed`, новый запуск обходит их с начала и снова вставляет только отсутствующие `Id`.

Исключение помечает фазу `Failed`, текст — `Exception.Message`. Следующее нажатие продолжает незавершённые базы. После перезапуска службы фаза в памяти снова `Idle`, таблица `ImportProgress` на диске сохраняется.

## API

`src/Presentation/WebApi/Controllers/System/CouchDbImportController.cs`, рядом с `SystemActionsController`.

`POST /api/service/couchdb-import` — вызывает `Start` и сразу отвечает. Запрос не ждёт конца копирования.

- провайдер не SQLite или подключение CouchDB не заполнено — `400` и `{ "message": "..." }`;
- перенос уже идёт — `409` и `{ "message": "Перенос уже выполняется" }`;
- иначе `200` и текущее `CouchDbImportState`.

`GET /api/service/couchdb-import` — текущее состояние, всегда `200`.

`src/Presentation/WebApi/wwwroot/js/services/CouchDbImportService.js` — `startCouchDbImport` и `getCouchDbImport`. Разбор ошибки как у `testFrontolConnection`: при `!response.ok` бросать `Error` с `data.message`.

## Интерфейс

`src/Presentation/WebApi/wwwroot/js/config/menu.js` — пункт `serviceView`, подпись «Сервис», иконка `mdi mdi-settings-b-roll` (Material Symbols `settings_b_roll`), в `buildMenuItems` перед `MENU_ITEMS.INFO`.

`src/Presentation/WebApi/wwwroot/js/views/index.js` — маршрут `serviceView`.

`src/Presentation/WebApi/wwwroot/js/modules/Service/serviceView.js`:

- заголовок тулбара «FMU-API: Сервис»;
- кнопка «Перенести данные из CouchDB в SQLite»;
- подпись состояния.

Пока фаза `Running`, кнопка недоступна, страница запрашивает `GET` каждые 2 секунды. После `Completed` или `Failed` опрос прекращается, кнопка снова доступна.

Подтверждение — `webix.confirm`. Текст: «Перенести данные из CouchDB в SQLite? Копирование может занять долго, база CouchDB не изменяется.»

`databaseConnection.js`:

- `richselect` «СУБД», значения `CouchDb` и `Sqlite`, поле `database.provider`;
- поле «Файл SQLite», `database.sqlitePath`, видно только при `Sqlite`;
- блок адреса, пользователя, пароля и кнопка Fauxton видны только при `CouchDb`.

## Файлы

| Файл | Что меняется |
|---|---|
| `Directory.Packages.props` | `Microsoft.EntityFrameworkCore.Sqlite` 10.0.11 |
| `src/Infrastructure/Sqlite/Sqlite.csproj` | новый проект |
| `src/Core/FmuApiDomain/Configuration/Options/DatabaseProvider.cs` | новый |
| `src/Core/FmuApiDomain/Configuration/Options/CouchDbConnection.cs` | `Provider`, `SqlitePath`, `ConfigurationIsEnabled` |
| `src/Core/FmuApiDomain/Database/Interfaces/ICouchDbToSqliteImport.cs` | новый |
| `src/Core/FmuApiDomain/Database/Models/CouchDbImportState.cs` | новый |
| `src/Infrastructure/Sqlite/SqliteDbContext.cs` | новый |
| `src/Infrastructure/Sqlite/Repositories/` | семь репозиториев |
| `src/Infrastructure/Sqlite/Workers/SqliteStatusWorker.cs` | новый |
| `src/Infrastructure/Sqlite/Services/CouchDbToSqliteImport.cs` | новый |
| `src/Infrastructure/Sqlite/SqliteService.cs` | регистрация |
| `src/Infrastructure/CouchDb/CouchDbServicesRegistration.cs` | общий метод для офлайн-очереди и очистки статистики |
| `FMU-API.sln` | проект Sqlite |
| `src/Presentation/WebApi/WebApi.csproj` | ссылка на Sqlite |
| `src/Presentation/WebApi/Program.cs` | выбор регистрации по провайдеру |
| `src/Presentation/WebApi/Controllers/System/CouchDbImportController.cs` | новый |
| `src/Presentation/WebApi/wwwroot/js/modules/settings/elements/databaseConnection.js` | провайдер и путь |
| `src/Presentation/WebApi/wwwroot/js/config/menu.js` | пункт «Сервис» |
| `src/Presentation/WebApi/wwwroot/js/views/index.js` | маршрут |
| `src/Presentation/WebApi/wwwroot/js/modules/Service/serviceView.js` | новый |
| `src/Presentation/WebApi/wwwroot/js/services/CouchDbImportService.js` | новый |

## Порядок работ

1. Домен: провайдер, путь, `ConfigurationIsEnabled`, контракт переноса.
2. Проект Sqlite: контекст, репозитории, воркер статуса, регистрация. Сборка `dotnet build FMU-API.sln`.
3. `Program.cs`: ветка провайдера, общий метод CouchDB для очереди и очистки статистики.
4. Форма настроек базы.
5. Сервис переноса, контроллер, страница «Сервис».
6. Сборка и проверка по сценариям ниже.

## Готово, когда

- Старый конфиг без `provider` поднимает CouchDB, страница марок и мониторинг ведут себя как до задачи.
- При провайдере SQLite и существующем файле служба стартует без CouchDB, мониторинг показывает `On-line`, марка, записанная после старта, читается после перезапуска.
- При выключенной базе мониторинг показывает `Disabled`, воркер файл не открывает.
- При недоступном пути к файлу мониторинг показывает `Off-line`.
- На странице «Сервис» кнопка недоступна, пока провайдер не SQLite или подключение CouchDB пустое.
- Нажатие переносит семь баз. Документ, который уже есть в SQLite, не перезаписывается. База CouchDB после переноса содержит те же документы.
- Обрыв на середине и повторное нажатие продолжают с последнего `_id`, уже скопированные строки не дублируются.
- Повторный `POST` во время переноса возвращает `409`.
- Возврат провайдера на CouchDB и перезапуск снова читают исходную базу CouchDB.
