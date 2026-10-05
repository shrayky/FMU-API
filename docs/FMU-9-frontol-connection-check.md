# FMU-9. Проверка связи с базой Frontol

Спецификация для разработки. Исходный запрос задачи:

> Страница мониторинга показывает доступность CouchDB, локальных модулей, ТС ПИоТ и токенов ГИС МТ. Связь с базой Frontol (Firebird, справочник товаров) нигде не проверяется: обрыв виден только когда запрос к SPRT или кранам уже падает. В настройках подключения тоже нет способа убедиться, что путь, пользователь и пароль верные, пока настройки не сохранены.

Два независимых входа в одну проверку Firebird:

- кнопка «Проверить связь» в окне подключения — по полям формы, до сохранения;
- фоновая проверка выбранной базы справочника товаров — статус на странице мониторинга.

## Поведение

### Тест из настроек

Окно редактирования подключения: `connectedFrontol.js`, форма `FrontolConnectionEditForm`.

Кнопка «Проверить связь» (`id` = `testFrontolConnection`) стоит в том же ряду, что «Сохранить» и «Закрыть», перед «Сохранить». Ширина каждой из трёх кнопок 200, чтобы ряд помещался в окно шириной 800.

Запрос уходит с текущими значениями полей пути, пользователя и пароля. Сохранение конфигурации не вызывается, окно не закрывается.

На время запроса кнопка недоступна, подпись «Проверка...». После ответа подпись и доступность возвращаются.

Ответ:

- успех — `webix.message` с текстом «Связь установлена»;
- ошибка — `webix.message` с `type: "error"` и текстом из `message` ответа (хост недоступен, неверный пароль, файл не найден, пустые поля).

Пустые путь, пользователь или пароль на сервере не доходят до Firebird: ответ 400, `message` — «Укажите путь, пользователя и пароль».

Поля берутся из формы, а не из строки таблицы: новое подключение ещё не сохранено, но проверяться должно так же, как сохранённое.

### Мониторинг

Проверяется одно сохранённое подключение: выбранное в «База справочника товаров Frontol», `ConnectedFrontolSettings.ResolveWareDataSourceId()`. Остальные подключения, включая базу кранов, воркер не трогает. Кнопка в окне проверяет любое открытое подключение, в том числе ещё не сохранённое.

Статусы в `MonitoringData`, строки как у CouchDB:

| Условие | Значение |
|---|---|
| id базы 0, запись не найдена или `ConnectionEnable()` равен false | `Disabled` |
| Firebird открыл соединение | `On-line` |
| таймаут или ошибка подключения | `Off-line` |

`GET /api/monitoring/systemstate` статус только читает. К Firebird из `MonitoringInformationService.Collect()` не обращаться: недоступный сервер задержит ответ мониторинга.

На странице мониторинга подпись «Статус базы Frontol» стоит сразу под «Статус базы данных». Цвета те же: синий `On-line`, красный `Off-line`, белый `Disabled`.

Первая проверка — сразу после старта процесса, дальше каждые 30 секунд. Смена настроек подхватывается следующим циклом, перезапуск службы не нужен.

## Общая проверка

И кнопка, и воркер вызывают один метод. Таймаут подключения 5 секунд, константа в реализации проверки. К строке `FrontolConnectionSettings.ConnectionStringBuild()` добавляется `Connection Timeout=5`, если его ещё нет.

Проверка: `FrontolDbContext` со этой строкой и `Database.CanConnectAsync`. Исключение ловится один раз на методе проверки. Успех — `Result.Success()`. Ошибка — `Result.Failure` с `Exception.Message`. Наружу из цикла воркера исключение не выходит.

`FrontolDbContext` создаётся конструктором со строкой подключения (`new FrontolDbContext(connectionString)`), а не через DI: конструктор с `IParametersService` читает только сохранённые настройки и для кнопки не подходит.

## Слои

| Часть | Проект | Зачем здесь |
|---|---|---|
| Интерфейс проверки, статус в состоянии приложения | `src/Core/FmuApiDomain` | Контракт без Firebird |
| Реализация проверки и воркер | `src/Infrastructure/FrontolDb` | Уже открывает Firebird, папка `Workers` пустая |
| Поле мониторинга | `src/Core/FmuApiApplication` | `MonitoringInformationService` собирает ответ |
| Кнопка, метод API, строка на мониторинге | `src/Presentation/WebApi` | Форма подключения и `monitorView.js` уже здесь |

## Домен

`src/Core/FmuApiDomain/Frontol/Interfaces/IFrontolConnectionProbe.cs`:

```csharp
using CSharpFunctionalExtensions;
using FmuApiDomain.Configuration.Options;

/// <summary>
/// Проверка доступности базы Frontol по параметрам подключения Firebird.
/// </summary>
public interface IFrontolConnectionProbe
{
    Task<Result> ProbeAsync(FrontolConnectionSettings connection, CancellationToken cancellationToken);
}
```

`IApplicationState` и `ApplicationState` — пара методов по образцу CouchDB:

```csharp
bool FrontolDbOnline();
void UpdateFrontolDbState(bool value);
```

Начальное значение `false`. Различие `Disabled` и `Off-line` считается при сборке мониторинга из настроек, не из этого флага.

## Infrastructure

В `src/Infrastructure/FrontolDb/FrontolDb.csproj` добавить пакет `Microsoft.Extensions.Hosting.Abstractions` — его ещё нет, без него `BackgroundService` не соберётся. Версия 10.0.11 берётся из `Directory.Packages.props`, номер в проекте не указывается.

`src/Infrastructure/FrontolDb/Services/FrontolConnectionProbe.cs` — реализация `IFrontolConnectionProbe`.

`src/Infrastructure/FrontolDb/Workers/FrontolDbStatusWorker.cs` — `BackgroundService`:

1. Прочитать настройки через `IParametersService.CurrentAsync()`.
2. Найти подключение по `ResolveWareDataSourceId()`.
3. Если подключения нет или `ConnectionEnable()` равен false — записать в состояние `false` и не открывать Firebird. Смену с `true` на `false` залогировать.
4. Иначе вызвать `ProbeAsync`. Успех — `UpdateFrontolDbState(true)`, неуспех — `false`.
5. Лог только при смене статуса, текстом как в `CouchDbStatusWorker`: было → стало.
6. `Task.Delay` 30 секунд в конце цикла, не в начале, чтобы первая проверка прошла сразу. `CancellationToken` пробрасывать в `Delay` и в `ProbeAsync`.

Регистрация в `FrontolDbService.AddService`:

```csharp
services.AddSingleton<IFrontolConnectionProbe, FrontolConnectionProbe>();
services.AddHostedService<FrontolDbStatusWorker>();
```

## API

`FrontolConnectionController`, маршрут рядом с `import-from-admin` и `load-beer-taps`:

`POST api/configuration/FrontolConnection/test`

Тело:

```json
{ "path": "server:C:\\Frontol\\main.gdb", "userName": "SYSDBA", "password": "masterkey" }
```

Контроллер собирает `FrontolConnectionSettings` и вызывает `IFrontolConnectionProbe`. Настройки приложения не меняет.

- успех — `200` и `{ "message": "Связь установлена" }`;
- пустые поля или ошибка Firebird — `400` и `{ "message": "..." }`.

`src/Presentation/WebApi/wwwroot/js/services/FrontolConnectionService.js` — функция `testFrontolConnection`. Разбор ответа как у `loadBeerTapsFromFrontol`: при `!response.ok` бросать `Error` с `data.message`.

## Мониторинг

`MonitoringData`:

```csharp
public string FrontolDbOnLine { get; init; } = string.Empty;
```

В `Collect()` заполнять так:

- выбранное подключение отсутствует или `ConnectionEnable()` равен false — `Disabled`;
- иначе `FrontolDbOnline()` — `On-line`, иначе `Off-line`.

`monitorView.js`:

- подпись в `LABELS`, id в `NAMES`, элемент `label` сразу после блока статуса CouchDB;
- в обработчике опроса вызывать обновление из `monitoringData.frontolDbOnLine`;
- цвета скопировать из `_updateDbState`.

## Файлы

| Файл | Что меняется |
|---|---|
| `src/Core/FmuApiDomain/Frontol/Interfaces/IFrontolConnectionProbe.cs` | новый |
| `src/Core/FmuApiDomain/State/Interfaces/IApplicationState.cs` | два метода |
| `src/Core/FmuApiApplication/State/ApplicationState.cs` | флаг и два метода |
| `src/Infrastructure/FrontolDb/FrontolDb.csproj` | пакет Hosting.Abstractions |
| `src/Infrastructure/FrontolDb/Services/FrontolConnectionProbe.cs` | новый |
| `src/Infrastructure/FrontolDb/Workers/FrontolDbStatusWorker.cs` | новый |
| `src/Infrastructure/FrontolDb/FrontolDbService.cs` | регистрация |
| `src/Presentation/WebApi/Controllers/Configuration/FrontolConnectionController.cs` | `POST test` |
| `src/Presentation/WebApi/wwwroot/js/services/FrontolConnectionService.js` | `testFrontolConnection` |
| `src/Presentation/WebApi/wwwroot/js/modules/settings/elements/connectedFrontol.js` | кнопка и обработчик |
| `src/Core/FmuApiApplication/Monitoring/Dto/MonitoringData.cs` | `FrontolDbOnLine` |
| `src/Core/FmuApiApplication/Monitoring/MonitoringInformationService.cs` | заполнение поля |
| `src/Presentation/WebApi/wwwroot/js/modules/Monitoring/monitorView.js` | строка статуса |

## Порядок работ

1. Домен: интерфейс проверки, методы состояния.
2. Infrastructure: проба и воркер, регистрация, `dotnet build`.
3. API и сервис JS: `POST test`, `testFrontolConnection`.
4. Кнопка в форме подключения.
5. Мониторинг: DTO, `Collect()`, `monitorView.js`.
6. Сборка и проверка по сценариям ниже.

## Готово, когда

- «Проверить связь» по несохранённым полям показывает успех на живой базе и текст ошибки на неверном пути или пароле. Конфигурация при этом не записывается.
- При живой выбранной базе на мониторинге `On-line`.
- При недоступной сохранённой базе — `Off-line`, страница мониторинга открывается без ожидания таймаута Firebird.
- Если база справочника не выбрана — `Disabled`, воркер к Firebird не подключается.
- Смена пути, пользователя, пароля или выбранной базы видна на мониторинге не позже чем через 30 секунд, без перезапуска службы.
