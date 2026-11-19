# Списание марок на ТСД

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Кнопка «Списание марок» создаёт пустой документ на ТСД, тип списания выбирается в документе, фасовка запрашивает цену во Frontol, выгрузка кладёт черновик в журнал ГИС МТ.

**Architecture:** Задача живёт в `Presentation/TsdApp` и пишет строки в уже существующий `main.db`. Цена — отдельный запрос `api/frontol`, его выполняет `IFrontolSprTService.RetailPriceByBarcode`; название и масса марки — `POST /api/ts/cises/info`. Выгрузка — `POST /api/ts/gismt/journal/receipt` с полем `reason` и строками `mark`, `quantity`, `price`; сервер создаёт черновик, подставляет значения с ТСД, а недостающую цену берёт из Frontol. В Честный знак документ сам не отправляется.

**Tech Stack:** Flutter в `src/Presentation/TsdApp`, ASP.NET в `src/Presentation/WebApi`, служба в `src/Core/FmuApiApplication`, Frontol в `src/Infrastructure/FrontolDb`. Проверка: `flutter test` и `dotnet test`.

## Global Constraints

- Суффикса `Async` у новых методов нет. Комментарий — у класса и интерфейса, на русском. У метода комментарий, повторяющий имя, не писать.
- Вложенных `try/catch` нет: один на верхнем уровне вызова.
- Типы списания — уже существующие `PACKING` и `EXPIRATION`. Подписи: «Фасовка» и «Истечение срока». В новом документе тип `PACKING`.
- Пустой `apiBaseUrl` документ не создаёт. Текст: `Укажите адрес Fmu-Api в настройках`.
- Кнопка задачи на меню создаёт новый пустой документ и открывает его. Иконка журнала список не создаёт. «+» журнала создаёт такой же пустой документ.
- В уже сохранённые кнопки меню задача сама не вставляется. Её ставят формой задачи: раздел, порядок, подпись, горячая клавиша, журнал.
- Повтор марки ловится по КИ без криптохвоста. Текст: `Эта марка уже отсканирована!`. Не марка: `Это не марка`. Выгруженный документ скан не пишет: `Документ уже выгружен`.
- Состава 1С нет, лимит количества не проверяется.
- Нет подключения товаров Frontol, штрихкод не найден или цена пустая — `{ "price": null }` и код 200, база не открывается, марка остаётся. Ошибка чтения базы — ошибка запроса, марка тоже остаётся.
- В акт Честного знака при выгрузке уходит количество и цена строки с ТСД: цена сначала запрашивается из Frontol, количество — из сведений ЧЗ, у штучного товара ставится 1. Оператор может поправить оба значения перед выгрузкой.
- Журнал сбора марок документы списания не показывает. Старые строки остаются сбором.
- Выгрузка создаёт черновик. Отправка в Честный знак остаётся кнопкой журнала на сайте.

## Файлы

- `src/Presentation/WebApi/Controllers/FrontolData/WarePriceByBarcodeController.cs` — `GET /api/frontol/WarePriceByBarcode/{barcode}`.
- `src/Presentation/WebApi/Controllers/TrueSign/GisMtJournalController.cs` — `CreateReceiptRequest` получает `reason`.
- `src/Core/FmuApiDomain/GisMt/Interfaces/IGisMtReceiptService.cs` — создание черновика с причиной.
- `src/Core/FmuApiApplication/GisMt/Journal/GisMtReceiptService.cs` — `PACKING` как сейчас, `EXPIRATION` без цены.
- `src/Presentation/TsdApp/lib/infrastructure/documents/main_database.dart` — схема 5, колонка `kind`.
- `src/Presentation/TsdApp/lib/domain/documents/document_store.dart` — создание пустого списания и список по виду.
- `src/Presentation/TsdApp/lib/infrastructure/documents/sqlite_document_store.dart` — запись и фильтр.
- `src/Presentation/TsdApp/lib/application/shell/task_registry.dart` — задача `markWriteOff`.
- `src/Presentation/TsdApp/lib/infrastructure/api/fmu_api_client.dart` — цена и выгрузка.
- `src/Presentation/TsdApp/lib/presentation/mark_write_off/` — экран документа и журнал.
- Тесты: `src/tests/FmuApiApplication.Tests`, `src/Presentation/TsdApp/test`.

Вид документа в колонке `kind`: сбор — `markCollection`, списание — `writeOff`. Причина лежит в `body.reason`.

---

### Task 1: Цена по штрихкоду Frontol

**Files:**
- Create: `src/Presentation/WebApi/Controllers/FrontolData/WarePriceByBarcodeController.cs`
- Test: `src/tests/FmuApiApplication.Tests` — проверка отказа открывать базу без подключения, если логика вынесена в отдельный метод. Контроллер тонкий: подключение читает настройки, цену берёт `IFrontolSprTService`.

- [x] **Step 1: Контроллер**

Маршрут `api/frontol/[controller]`, группа Swagger `Frontol database`. Метод `GET {barcode}`.

Перед вызовом `RetailPriceByBarcode` прочитать настройки. `ResolveWareDataSourceId()` равен 0 или у найденного подключения `ConnectionEnable()` ложь — вернуть `Ok(new { price = (decimal?)null })`, репозиторий не вызывать.

Иначе вызвать `RetailPriceByBarcode`. `IsFailure` — `StatusCode(500, new { error = price.Error })`. Успех — `Ok(new { price = price.Value })`, в том числе когда значение `null`.

- [x] **Step 2: Сборка**

Run: `dotnet build FMU-API.sln`
Expected: сборка проходит.

### Task 2: Выгрузка черновика с причиной

**Files:**
- Modify: `src/Presentation/WebApi/Controllers/TrueSign/GisMtJournalController.cs`
- Modify: `src/Core/FmuApiDomain/GisMt/Interfaces/IGisMtReceiptService.cs`
- Modify: `src/Core/FmuApiApplication/GisMt/Journal/GisMtReceiptService.cs`
- Test: `src/tests/FmuApiApplication.Tests/GisMtReceiptServiceTests.cs`

- [x] **Step 1: Контракт**

`CreateReceiptRequest` дополняется полем `string? Reason`. Пустая и отсутствующая причина для текущего сайта остаётся `PACKING`.

`IGisMtReceiptService.Create` принимает причину. `GisMtWriteOffReasons.Resolve` приводит её к верхнему регистру. Причина не из пары `PACKING` / `EXPIRATION` — ошибка `Неизвестная причина списания`.

- [x] **Step 2: Фасовка**

`PACKING` идёт текущим `Create`: группа `milk`, строки через `BuildPreview`, цена из `LoadPrices`. Поле `Reason` документа — `PACKING`.

- [x] **Step 3: Истечение срока**

`EXPIRATION` пишет те же присланные марки строками акта без запроса цены во Frontol. `product_cost` пустой. Товарная группа берётся из сведений КИ. Марки разных групп — ошибка `Марки разных товарных групп`, черновик не создаётся. Группу определить нельзя — ошибка создания, черновик не создаётся.

Существующие `CreateExpiration` и подбор просроченных остатков не меняются: ими пользуется сайт.

- [x] **Step 4: Тест и сборка**

`dotnet test src/tests/FmuApiApplication.Tests/GisMtReceiptServiceTests.cs`

Ожидание: пустая причина создаёт акт с `PACKING`; `EXPIRATION` не вызывает цену Frontol; две группы в одном списке акт не создают.

### Task 3: Пустой документ в базе ТСД

**Files:**
- Modify: `src/Presentation/TsdApp/lib/infrastructure/documents/main_database.dart`
- Modify: `src/Presentation/TsdApp/lib/domain/documents/document_store.dart`
- Modify: `src/Presentation/TsdApp/lib/domain/documents/scanned_document.dart`
- Modify: `src/Presentation/TsdApp/lib/infrastructure/documents/sqlite_document_store.dart`
- Test: `src/Presentation/TsdApp/test/sqlite_document_store_test.dart`

- [x] **Step 1: Схема**

Версия `main.db` становится 5. В `documents` колонка `kind TEXT NOT NULL DEFAULT 'markCollection'`. Старая база при обновлении добавляет колонку с этим значением по умолчанию, строки сбора не переписываются.

- [x] **Step 2: Создание**

`DocumentStore.createWriteOff({required DateTime scannedAt})` вставляет строку и возвращает её:

| Колонка | Значение |
|---|---|
| `id` | новый идентификатор |
| `kind` | `writeOff` |
| `presentation` | `Списание` |
| `body` | `{"reason":"PACKING"}` |
| `uploaded` | 0 |
| `number`, `counterparty`, `erp_uid` | пустые |

Марок у новой строки нет.

- [x] **Step 3: Список**

`documents({String query = '', String kind = ''})`. Пустой `kind` оставляет текущий список сбора: строки `markCollection` и строки без вида. Журнал списания передаёт `writeOff`. Поиск по представлению, номеру и контрагенту остаётся.

Журнал сбора вызывает список с видом сбора, чтобы списание туда не попадало.

- [x] **Step 4: Причина**

Смена типа пишет `body.reason` и не трогает марки. Чтение документа отдаёт причину из тела. Нет ключа — `PACKING`.

- [x] **Step 5: Тест**

`flutter test test/sqlite_document_store_test.dart`

Ожидание: пустое списание не видно в списке сбора; сбор не виден в списке списания; старая база версии 4 открывается, прежние документы остаются сбором.

### Task 4: Кнопка меню

**Files:**
- Modify: `src/Presentation/TsdApp/lib/application/shell/task_registry.dart`
- Create: `src/Presentation/TsdApp/lib/presentation/mark_write_off/mark_write_off_screen.dart`
- Create: `src/Presentation/TsdApp/lib/presentation/mark_write_off/mark_write_off_journal_screen.dart`
- Test: `src/Presentation/TsdApp/test/task_buttons_test.dart`

- [x] **Step 1: Задача**

В `TaskRegistry.tasks()` третья задача:

- `id`: `markWriteOff`
- `defaultTitle`: `Списание марок`
- `defaultSection`: `TaskSection.documents`
- `hasJournal`: true
- `open`: экран, который создаёт документ
- `openJournal`: журнал списания

`TaskButtons.resolve` не менять. Сохранённый список кнопок новую задачу не получает, пока её не добавят в форме.

- [x] **Step 2: Создание с кнопки и из журнала**

Экран с флагом создания в `initState` вызывает `createWriteOff`, если `apiBaseUrl` не пустой, и показывает этот документ. Пустой адрес строку не пишет и оставляет текст `Укажите адрес Fmu-Api в настройках`.

Кнопка меню открывает экран с этим флагом. «+» журнала открывает тот же экран с тем же флагом. Строка журнала открывает экран с уже существующим `id` и новый документ не создаёт.

Каждое такое открытие при заполненном адресе — новая строка.

- [x] **Step 3: Тест**

Виджет-тест: пустой адрес не увеличивает число строк `writeOff`. Заполненный адрес по кнопке меню добавляет одну строку и открывает её. Повторное нажатие добавляет вторую. Иконка журнала число строк не меняет.

### Task 5: Экран документа

**Files:**
- Modify: `src/Presentation/TsdApp/lib/presentation/mark_write_off/mark_write_off_screen.dart`
- Modify: `src/Presentation/TsdApp/lib/infrastructure/api/fmu_api_client.dart`
- Test: `src/Presentation/TsdApp/test/mark_write_off_test.dart`

- [x] **Step 1: Тип и сканер**

На экране выбор типа: «Фасовка» и «Истечение срока». Смена сохраняется в `body.reason`.

Скан идёт по одному. Код проверяется `MarkCode.looksLikeMark`. Марка пишется в `marks` с `units` 1 и пустым `nomenclatureUid`. Повтор по `cis` не пишется.

- [x] **Step 2: Цена**

После удачной записи марки ТСД запрашивает `POST {apiBaseUrl}/api/ts/cises/info` с ИНН и маркой: из ответа берутся название товара и масса. Если причина `PACKING`, берётся GTIN марки и вызывается `GET {apiBaseUrl}/api/frontol/WarePriceByBarcode/{barcode}`. GTIN — 14 цифр сразу после `01`, а у марки длиной 29 — 14 цифр с начала. Цена и количество показываются в строке таблицы; масса из ЧЗ ставится количеством, у штучного товара остаётся 1. `null`, ошибка HTTP и обрыв сети строку марки не удаляют; цена на экране пустая. Оператор правит количество и цену в строке, строка удаляется отдельной кнопкой.

Причина не `PACKING` — цену не запрашивает и колонку не показывает. Смена на фасовку запрашивает цену по маркам, которые уже лежат в документе.

- [x] **Step 3: Выгрузка**

Кнопка «Выгрузить» при пустом списке марок не шлёт запрос. Текст: `В документе нет ни одной марки`.

Иначе `POST {apiBaseUrl}/api/ts/gismt/journal/receipt`. Тело: `inn`, `reason`, `lines` (`mark`, `quantity`, `price`). ИНН — тот же `GET /api/configuration/OrganisationConfig`, что у проверки марки. `mark` — текст марки как в базе; цена у истечения срока не передаётся.

Ответ 200 ставит `uploaded` в 1. Документ после этого скан не принимает. Другой код и обрыв `uploaded` не меняют, текст ошибки показывается на экране.

- [x] **Step 4: Тест**

`flutter test test/mark_write_off_test.dart`

Ожидание: фасовка после скана один раз вызывает `WarePriceByBarcode`; истечение срока этот адрес не вызывает; выгрузка шлёт `reason` и марки; ошибка выгрузки оставляет `uploaded` равным 0.

### Task 6: Журнал списания

**Files:**
- Modify: `src/Presentation/TsdApp/lib/presentation/mark_write_off/mark_write_off_journal_screen.dart`
- Test: `src/Presentation/TsdApp/test/mark_write_off_journal_test.dart`

- [x] **Step 1: Список**

Журнал читает `documents(kind: 'writeOff')` от новых к старым. Колонки те же, что у сбора: документ, «Выгружен» при `uploaded`, номер, дата. Поиск и срок хранения используются как есть. Срок удаляет просроченные строки вместе с их марками и справочники не трогает.

- [x] **Step 2: Тест**

В списке списания нет документа сбора. «Выгружен» стоит только у строки с `uploaded` 1. «+» при пустом адресе список не удлиняет.

## Дополнение

- Без выбранной организации марка не принимается: «Выберите организацию»; когда в Fmu-Api нет ни одной организации — «В Fmu-Api не задан ИНН организации». Скан ничего не пишет в `marks`.
- Пока идёт выгрузка, поверх экрана виден спиннер, кнопка «Выгрузить» заблокирована; спиннер снимается по завершении запроса.
