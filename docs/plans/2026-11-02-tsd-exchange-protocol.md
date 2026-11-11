# Протокол обмена ТСД — план реализации

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** На карточке сервера обмена проверка связи вызывает `GET dstsdapi/ping` протокола `DS TСД`, а остальные команды ds-tsd и весь Data Mobile остаются в интерфейсе, но в сеть не ходят.

**Architecture:** Домен описывает протокол, отмену и результат и не знает про `package:http`. Реестр в application по имени карточки создаёт класс infrastructure. `DsTsdProtocol` выполняет только ping и сам закрывает свой `http.Client`. Форма до открытия экрана «Ожидание...» отсекает пустой адрес, таймаут 0 и неизвестное имя; отмена возвращает на карточку без диалога.

**Tech Stack:** Flutter, Dart 3.13, `package:http` 1.5, `flutter_test`. Каталог `src/Presentation/TsdApp`.

## Global Constraints

- Спецификация: `docs/tsd-exchange-protocol.md`. Оболочка экранов уже зафиксирована в `docs/tsd-shell.md` и не переоткрывается.
- Суффикса `Async` у новых методов нет. Комментарий — у класса и интерфейса, на русском. У метода комментарий не писать.
- Вложенных `try/catch` нет. В `probe` один `try/catch` на всё исключение, включая таймаут.
- Домен не импортирует `package:http`. `FmuApiClient` этим срезом не вызывается.
- Имя по умолчанию: `DS T\u0421Д` (буква С — кириллица U+0421). Второе имя: `Data Mobile`.
- Успех HTTP — только код 200. Текст другого кода: `Код состяния ответа {код}.\n{тело}` (опечатка «состяния» как в 1С). Тело в поле результата попадает только при 200.
- Basic-авторизация ставится всегда, в том числе для пустого пользователя.
- Пустой адрес и порт 0: `Не задан адрес сервера обмена`. Таймаут 0: `Не задан таймаут запроса`. Имя не из реестра: `Неизвестный протокол обмена`.
- Неподключённая команда ds-tsd: `Команда ещё не подключена`. Data Mobile на любом методе: `Протокол Data Mobile ещё не подключён`.
- Диалог успеха: `Связь с сервером установлена`. Диалог ошибки: `Проверка связи завершилась с ошибкой: {текст}`. Кнопка диалога `Ок`, цвет `WebixDarkTheme.accent`. Коралл на экране не использовать.
- В этом срезе не делать: разбор JSON `goods` и токена True API, отправку JSON выгрузки и `setPrice`, загрузку номенклатуры по группам, обход токена через адрес сервиса проверки марок.
- Команда документа сбора, которую позже передают в метод строкой: `СборМарокПоДокументу`.
- Проверка: `flutter test` в `src/Presentation/TsdApp`.

## File Structure

- `src/Presentation/TsdApp/lib/domain/exchange/exchange_server.dart` — имена протоколов. Уже есть `ExchangeServer`.
- `src/Presentation/TsdApp/lib/domain/exchange/exchange_call_result.dart` — успех, текст ошибки, тело строкой.
- `src/Presentation/TsdApp/lib/domain/exchange/exchange_cancellation.dart` — отмена без знания об HTTP.
- `src/Presentation/TsdApp/lib/domain/exchange/exchange_protocol.dart` — интерфейс, тексты, проверка «запрос слать нельзя».
- `src/Presentation/TsdApp/lib/application/exchange/exchange_protocols.dart` — реестр двух имён. Импортирует infrastructure: так спецификация размещает выбор класса.
- `src/Presentation/TsdApp/lib/infrastructure/exchange/ds_tsd_protocol.dart` — ping и заглушки остальных команд.
- `src/Presentation/TsdApp/lib/infrastructure/exchange/data_mobile_protocol.dart` — бронь без сети.
- `src/Presentation/TsdApp/lib/presentation/settings/exchange_probe_screen.dart` — экран «Ожидание...», уже есть.
- `src/Presentation/TsdApp/lib/presentation/settings/exchange_server_form_screen.dart` — карточка, уже есть.
- `src/Presentation/TsdApp/test/exchange_guard_test.dart` — домен без сети.
- `src/Presentation/TsdApp/test/exchange_protocol_test.dart` — реестр и HTTP на подменном клиенте.
- `src/Presentation/TsdApp/test/exchange_probe_screen_test.dart` — диалоги и отмена.

---

### Task 1: Доменный контракт

**Files:**
- Modify: `src/Presentation/TsdApp/lib/domain/exchange/exchange_server.dart`
- Create: `src/Presentation/TsdApp/lib/domain/exchange/exchange_call_result.dart`
- Create: `src/Presentation/TsdApp/lib/domain/exchange/exchange_cancellation.dart`
- Create: `src/Presentation/TsdApp/lib/domain/exchange/exchange_protocol.dart`
- Test: `src/Presentation/TsdApp/test/exchange_guard_test.dart`

**Interfaces:**
- Consumes: `ExchangeServer` из `exchange_server.dart`.
- Produces:
  - `const defaultExchangeProtocol = 'DS T\u0421Д'`
  - `const dataMobileProtocol = 'Data Mobile'`
  - `const exchangeProtocolNames = <String>[defaultExchangeProtocol, dataMobileProtocol]`
  - `const markCollectionDocumentCommand = 'СборМарокПоДокументу'`
  - `const exchangeAddressMissing`, `exchangeTimeoutMissing`, `exchangeProtocolUnknown`, `exchangeCommandPending`, `dataMobileReserved`, `exchangeProbeSuccess`, `exchangeProbeFailedPrefix` — строки из Global Constraints. Префикс ошибки диалога заканчивается пробелом: `'Проверка связи завершилась с ошибкой: '`.
  - `class ExchangeCallResult { const ExchangeCallResult({required bool success, String error = '', String body = ''}); final bool success; final String error; final String body; }`
  - `class ExchangeCancellation { bool get isCancelled; void cancel(); void onCancel(void Function() listener); }`
  - `ExchangeCallResult? probeTransportBlocked(ExchangeServer server)` — пустой адрес, порт 0 или таймаут 0.
  - `ExchangeCallResult? probeBlocked(ExchangeServer server)` — сначала transport, затем имя не из `exchangeProtocolNames`.
  - `abstract interface class ExchangeProtocol` с методами `probe`, `documentByBarcode`, `markStatus`, `trueApiToken`, `loadGoods`, `uploadDocument`, `setPrice`. У всех первые два позиционных аргумента: `ExchangeServer server`, `ExchangeCancellation cancellation`. Дальше именованные, где они есть. Возврат `Future<ExchangeCallResult>`.

- [ ] **Step 1: Write the failing test**

Создать `src/Presentation/TsdApp/test/exchange_guard_test.dart`:

```dart
import 'package:flutter_test/flutter_test.dart';
import 'package:tsd_app/domain/exchange/exchange_protocol.dart';
import 'package:tsd_app/domain/exchange/exchange_server.dart';

void main() {
  test('имена протоколов и команда документа сбора', () {
    expect(defaultExchangeProtocol, 'DS T\u0421Д');
    expect(dataMobileProtocol, 'Data Mobile');
    expect(exchangeProtocolNames, [defaultExchangeProtocol, dataMobileProtocol]);
    expect(markCollectionDocumentCommand, 'СборМарокПоДокументу');
  });

  test('пустой адрес и порт 0 блокируют проверку одним текстом', () {
    final emptyHost = probeTransportBlocked(_server(host: '  '));
    final zeroPort = probeTransportBlocked(_server(port: 0));

    expect(emptyHost?.success, isFalse);
    expect(emptyHost?.error, exchangeAddressMissing);
    expect(zeroPort?.error, exchangeAddressMissing);
  });

  test('таймаут 0 блокирует проверку после адреса', () {
    final blocked = probeBlocked(_server(timeoutSeconds: 0));

    expect(blocked?.error, exchangeTimeoutMissing);
  });

  test('неизвестное имя блокирует проверку и в сеть его не закладывает домен', () {
    final blocked = probeBlocked(_server(protocol: 'Старый'));

    expect(blocked?.error, exchangeProtocolUnknown);
    expect(probeTransportBlocked(_server(protocol: 'Старый')), isNull);
  });

  test('готовые параметры проверку не блокируют', () {
    expect(probeBlocked(_server()), isNull);
    expect(probeBlocked(_server(protocol: dataMobileProtocol)), isNull);
  });

  test('отмена вызывает слушателя один раз и сразу, если уже отменено', () {
    final cancellation = ExchangeCancellation();
    var calls = 0;
    cancellation.onCancel(() => calls += 1);
    cancellation.cancel();
    cancellation.cancel();
    expect(calls, 1);

    cancellation.onCancel(() => calls += 1);
    expect(calls, 2);
    expect(cancellation.isCancelled, isTrue);
  });
}

ExchangeServer _server({
  String host = '10.0.0.5',
  int port = 80,
  int timeoutSeconds = 5,
  String protocol = defaultExchangeProtocol,
}) {
  return ExchangeServer(
    id: '1',
    host: host,
    port: port,
    timeoutSeconds: timeoutSeconds,
    protocol: protocol,
  );
}
```

- [ ] **Step 2: Run test to verify it fails**

Run from `src/Presentation/TsdApp`:

```text
flutter test test/exchange_guard_test.dart
```

Expected: FAIL, компиляция не находит `exchange_protocol.dart` или `probeTransportBlocked`.

- [ ] **Step 3: Write minimal implementation**

В `exchange_server.dart` заменить комментарий и объявление имени. Строку `ExchangeServer` не трогать. Было:

```dart
/// Имя единственного протокола на форме сервера обмена.
const defaultExchangeProtocol = 'DS TСД';
```

Стало:

```dart
/// Имена протоколов на форме сервера обмена.
const defaultExchangeProtocol = 'DS T\u0421Д';
const dataMobileProtocol = 'Data Mobile';
const exchangeProtocolNames = <String>[defaultExchangeProtocol, dataMobileProtocol];
```

Создать `exchange_call_result.dart`:

```dart
/// Результат вызова команды обмена. Тело — сырой ответ, разбор делает срез команды.
class ExchangeCallResult {
  const ExchangeCallResult({
    required this.success,
    this.error = '',
    this.body = '',
  });

  final bool success;
  final String error;
  final String body;
}
```

Создать `exchange_cancellation.dart`:

```dart
/// Отмена запроса обмена. Реализация протокола закрывает по ней свой HTTP-клиент.
class ExchangeCancellation {
  var _cancelled = false;
  final List<void Function()> _listeners = [];

  bool get isCancelled => _cancelled;

  void cancel() {
    if (_cancelled) {
      return;
    }
    _cancelled = true;
    for (final listener in List<void Function()>.of(_listeners)) {
      listener();
    }
  }

  void onCancel(void Function() listener) {
    if (_cancelled) {
      listener();
      return;
    }
    _listeners.add(listener);
  }
}
```

Создать `exchange_protocol.dart`:

```dart
import 'exchange_call_result.dart';
import 'exchange_cancellation.dart';
import 'exchange_server.dart';

const markCollectionDocumentCommand = 'СборМарокПоДокументу';
const exchangeAddressMissing = 'Не задан адрес сервера обмена';
const exchangeTimeoutMissing = 'Не задан таймаут запроса';
const exchangeProtocolUnknown = 'Неизвестный протокол обмена';
const exchangeCommandPending = 'Команда ещё не подключена';
const dataMobileReserved = 'Протокол Data Mobile ещё не подключён';
const exchangeProbeSuccess = 'Связь с сервером установлена';
const exchangeProbeFailedPrefix = 'Проверка связи завершилась с ошибкой: ';

ExchangeCallResult? probeTransportBlocked(ExchangeServer server) {
  if (server.host.trim().isEmpty || server.port == 0) {
    return const ExchangeCallResult(success: false, error: exchangeAddressMissing);
  }
  if (server.timeoutSeconds == 0) {
    return const ExchangeCallResult(success: false, error: exchangeTimeoutMissing);
  }
  return null;
}

ExchangeCallResult? probeBlocked(ExchangeServer server) {
  final transport = probeTransportBlocked(server);
  if (transport != null) {
    return transport;
  }
  if (!exchangeProtocolNames.contains(server.protocol)) {
    return const ExchangeCallResult(success: false, error: exchangeProtocolUnknown);
  }
  return null;
}

/// Команды обмена с сервером учётной базы. Имя протокола выбирает реестр, не этот тип.
abstract interface class ExchangeProtocol {
  Future<ExchangeCallResult> probe(ExchangeServer server, ExchangeCancellation cancellation);

  Future<ExchangeCallResult> documentByBarcode(
    ExchangeServer server,
    ExchangeCancellation cancellation, {
    required String command,
    required String barcode,
  });

  Future<ExchangeCallResult> markStatus(
    ExchangeServer server,
    ExchangeCancellation cancellation, {
    required String mark,
  });

  Future<ExchangeCallResult> trueApiToken(ExchangeServer server, ExchangeCancellation cancellation);

  Future<ExchangeCallResult> loadGoods(ExchangeServer server, ExchangeCancellation cancellation);

  Future<ExchangeCallResult> uploadDocument(
    ExchangeServer server,
    ExchangeCancellation cancellation, {
    required Map<String, Object?> packet,
  });

  Future<ExchangeCallResult> setPrice(
    ExchangeServer server,
    ExchangeCancellation cancellation, {
    required String nomenclature,
    required num price,
  });
}
```

- [ ] **Step 4: Run test to verify it passes**

Run from `src/Presentation/TsdApp`:

```text
flutter test test/exchange_guard_test.dart
```

Expected: PASS, все 6 тестов.

- [ ] **Step 5: Commit**

```text
git add -- src/Presentation/TsdApp/lib/domain/exchange/exchange_server.dart src/Presentation/TsdApp/lib/domain/exchange/exchange_call_result.dart src/Presentation/TsdApp/lib/domain/exchange/exchange_cancellation.dart src/Presentation/TsdApp/lib/domain/exchange/exchange_protocol.dart src/Presentation/TsdApp/test/exchange_guard_test.dart
git commit -m "feat: контракт протокола обмена ТСД"
```

В коммит не класть посторонние изменения GisMt, CouchDB, Sqlite, WebApi.

---

### Task 2: ping ds-tsd и бронь Data Mobile

**Files:**
- Create: `src/Presentation/TsdApp/lib/infrastructure/exchange/ds_tsd_protocol.dart`
- Create: `src/Presentation/TsdApp/lib/infrastructure/exchange/data_mobile_protocol.dart`
- Create: `src/Presentation/TsdApp/lib/application/exchange/exchange_protocols.dart`
- Test: `src/Presentation/TsdApp/test/exchange_protocol_test.dart`

**Interfaces:**
- Consumes: типы Task 1. `probeTransportBlocked` вызывается только из `DsTsdProtocol.probe`. Data Mobile и заглушки ds-tsd его не вызывают: пустой адрес у них не подменяет их собственный текст.
- Produces:
  - `class ExchangeProtocols { const ExchangeProtocols(); ExchangeProtocol? byName(String name, {http.Client Function()? createClient}); }`
  - `class DsTsdProtocol implements ExchangeProtocol { DsTsdProtocol({http.Client? httpClient}); }`
  - `class DataMobileProtocol implements ExchangeProtocol { const DataMobileProtocol(); }`
  - `byName('DS T\u0421Д')` вызывает `createClient` один раз и возвращает `DsTsdProtocol`. Без фабрики создаёт `http.Client()` внутри.
  - `byName('Data Mobile')` возвращает `DataMobileProtocol` и фабрику не вызывает.
  - `byName` любого другого имени возвращает `null` и фабрику не вызывает.
  - URL ping: `{http|https}://{host}:{port}/{publication}/dstsdapi/ping`. Пустая публикация и публикация из одних слэшей дают путь `/dstsdapi/ping`. Крайние слэши публикации снимаются.
  - Заголовок `Authorization: Basic {base64(utf8("$user:$password"))}`.
  - Экземпляр `DsTsdProtocol` закрывает клиент в конце `probe` и повторно из `cancel`. Повторное закрытие не падает. Следующая проверка — новый экземпляр из `byName`.

- [ ] **Step 1: Write the failing test**

Создать `src/Presentation/TsdApp/test/exchange_protocol_test.dart`:

```dart
import 'dart:async';
import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:tsd_app/application/exchange/exchange_protocols.dart';
import 'package:tsd_app/domain/exchange/exchange_protocol.dart';
import 'package:tsd_app/domain/exchange/exchange_server.dart';
import 'package:tsd_app/infrastructure/exchange/data_mobile_protocol.dart';
import 'package:tsd_app/infrastructure/exchange/ds_tsd_protocol.dart';

void main() {
  test('реестр знает два имени и не создаёт клиент для остальных', () {
    final registry = ExchangeProtocols();
    var created = 0;
    http.Client factory() {
      created += 1;
      return RecordingClient(200);
    }

    expect(registry.byName(defaultExchangeProtocol, createClient: factory), isA<DsTsdProtocol>());
    expect(created, 1);
    expect(registry.byName(dataMobileProtocol, createClient: factory), isA<DataMobileProtocol>());
    expect(registry.byName('Старый', createClient: factory), isNull);
    expect(created, 1);
  });

  test('ping собирает URL, basic-авторизацию и принимает только 200', () async {
    final client = RecordingClient(200, body: 'pong');
    final protocol = DsTsdProtocol(httpClient: client);

    final result = await protocol.probe(
      _server(user: 'user', password: 'secret', publication: 'erp'),
      ExchangeCancellation(),
    );

    expect(result.success, isTrue);
    expect(result.body, 'pong');
    expect(result.error, isEmpty);
    expect(client.sent, 1);
    expect(client.request?.method, 'GET');
    expect(client.request?.url, Uri.parse('http://10.0.0.5:80/erp/dstsdapi/ping'));
    expect(
      client.request?.headers['authorization'],
      'Basic ${base64Encode(utf8.encode('user:secret'))}',
    );
  });

  test('пустая публикация и безопасное соединение меняют только URL', () async {
    final client = RecordingClient(200);
    final protocol = DsTsdProtocol(httpClient: client);

    await protocol.probe(
      _server(secure: true, port: 443, publication: '/', user: '', password: ''),
      ExchangeCancellation(),
    );

    expect(client.request?.url, Uri.parse('https://10.0.0.5:443/dstsdapi/ping'));
    expect(client.request?.headers['authorization'], 'Basic ${base64Encode(utf8.encode(':'))}');
  });

  test('код не 200 кладёт тело в текст ошибки и оставляет поле тела пустым', () async {
    final client = RecordingClient(503, body: 'нет');
    final result = await DsTsdProtocol(httpClient: client).probe(_server(), ExchangeCancellation());

    expect(result.success, isFalse);
    expect(result.body, isEmpty);
    expect(result.error, 'Код состяния ответа 503.\nнет');
  });

  test('сбой соединения обрабатывается один раз', () async {
    final client = ThrowingClient();
    final result = await DsTsdProtocol(httpClient: client).probe(_server(), ExchangeCancellation());

    expect(result.success, isFalse);
    expect(result.error, contains('socket'));
    expect(client.sent, 1);
  });

  test('пустой адрес и таймаут 0 не вызывают сеть', () async {
    final client = RecordingClient(200);
    final protocol = DsTsdProtocol(httpClient: client);

    final missingHost = await protocol.probe(_server(host: ''), ExchangeCancellation());
    final zeroPort = await protocol.probe(_server(port: 0), ExchangeCancellation());
    final missingTimeout = await protocol.probe(_server(timeoutSeconds: 0), ExchangeCancellation());

    expect(missingHost.error, exchangeAddressMissing);
    expect(zeroPort.error, exchangeAddressMissing);
    expect(missingTimeout.error, exchangeTimeoutMissing);
    expect(client.sent, 0);
  });

  test('остальные команды ds-tsd сеть не вызывают', () async {
    final client = RecordingClient(200);
    final protocol = DsTsdProtocol(httpClient: client);
    final cancellation = ExchangeCancellation();
    final server = _server(host: '');

    final results = await Future.wait([
      protocol.documentByBarcode(server, cancellation, command: markCollectionDocumentCommand, barcode: '1'),
      protocol.markStatus(server, cancellation, mark: '1'),
      protocol.trueApiToken(server, cancellation),
      protocol.loadGoods(server, cancellation),
      protocol.uploadDocument(server, cancellation, packet: const {}),
      protocol.setPrice(server, cancellation, nomenclature: '1', price: 10),
    ]);

    expect(results.map((result) => result.error), everyElement(exchangeCommandPending));
    expect(client.sent, 0);
  });

  test('Data Mobile на любом методе возвращает бронь и клиент не создаётся', () async {
    final protocol = const DataMobileProtocol();
    final cancellation = ExchangeCancellation();
    final server = _server();

    final results = await Future.wait([
      protocol.probe(server, cancellation),
      protocol.documentByBarcode(server, cancellation, command: 'x', barcode: '1'),
      protocol.markStatus(server, cancellation, mark: '1'),
      protocol.trueApiToken(server, cancellation),
      protocol.loadGoods(server, cancellation),
      protocol.uploadDocument(server, cancellation, packet: const {}),
      protocol.setPrice(server, cancellation, nomenclature: '1', price: 1),
    ]);

    expect(results.map((result) => result.error), everyElement(dataMobileReserved));
    expect(results.every((result) => result.success == false), isTrue);
  });

  test('отмена закрывает клиент один раз', () async {
    final client = GateClient();
    final protocol = DsTsdProtocol(httpClient: client);
    final cancellation = ExchangeCancellation();
    final pending = protocol.probe(_server(), cancellation);

    cancellation.cancel();
    final result = await pending;

    expect(result.success, isFalse);
    expect(client.closeCount, 1);
  });
}

ExchangeServer _server({
  String host = '10.0.0.5',
  int port = 80,
  bool secure = false,
  String publication = '',
  String user = 'user',
  String password = 'secret',
  int timeoutSeconds = 5,
}) {
  return ExchangeServer(
    id: '1',
    host: host,
    port: port,
    secure: secure,
    publication: publication,
    user: user,
    password: password,
    timeoutSeconds: timeoutSeconds,
    protocol: defaultExchangeProtocol,
  );
}

class RecordingClient extends http.BaseClient {
  RecordingClient(this.status, {this.body = ''});

  final int status;
  final String body;
  var sent = 0;
  http.BaseRequest? request;

  @override
  Future<http.StreamedResponse> send(http.BaseRequest request) async {
    sent += 1;
    this.request = request;
    return http.StreamedResponse(Stream.value(utf8.encode(body)), status);
  }

  @override
  void close() {}
}

class ThrowingClient extends http.BaseClient {
  var sent = 0;

  @override
  Future<http.StreamedResponse> send(http.BaseRequest request) {
    sent += 1;
    throw http.ClientException('socket', request.url);
  }

  @override
  void close() {}
}

class GateClient extends http.BaseClient {
  final _gate = Completer<http.StreamedResponse>();
  var closeCount = 0;

  @override
  Future<http.StreamedResponse> send(http.BaseRequest request) => _gate.future;

  @override
  void close() {
    closeCount += 1;
    if (!_gate.isCompleted) {
      _gate.completeError(http.ClientException('closed'));
    }
  }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run from `src/Presentation/TsdApp`:

```text
flutter test test/exchange_protocol_test.dart
```

Expected: FAIL, нет `exchange_protocols.dart`.

- [ ] **Step 3: Write minimal implementation**

Создать `data_mobile_protocol.dart`:

```dart
import '../../domain/exchange/exchange_call_result.dart';
import '../../domain/exchange/exchange_cancellation.dart';
import '../../domain/exchange/exchange_protocol.dart';
import '../../domain/exchange/exchange_server.dart';

/// Забронированный протокол Data Mobile. Контракта запросов нет, сеть не вызывается.
class DataMobileProtocol implements ExchangeProtocol {
  const DataMobileProtocol();

  static const _reserved = ExchangeCallResult(success: false, error: dataMobileReserved);

  @override
  Future<ExchangeCallResult> probe(ExchangeServer server, ExchangeCancellation cancellation) async {
    return _reserved;
  }

  @override
  Future<ExchangeCallResult> documentByBarcode(
    ExchangeServer server,
    ExchangeCancellation cancellation, {
    required String command,
    required String barcode,
  }) async {
    return _reserved;
  }

  @override
  Future<ExchangeCallResult> markStatus(
    ExchangeServer server,
    ExchangeCancellation cancellation, {
    required String mark,
  }) async {
    return _reserved;
  }

  @override
  Future<ExchangeCallResult> trueApiToken(ExchangeServer server, ExchangeCancellation cancellation) async {
    return _reserved;
  }

  @override
  Future<ExchangeCallResult> loadGoods(ExchangeServer server, ExchangeCancellation cancellation) async {
    return _reserved;
  }

  @override
  Future<ExchangeCallResult> uploadDocument(
    ExchangeServer server,
    ExchangeCancellation cancellation, {
    required Map<String, Object?> packet,
  }) async {
    return _reserved;
  }

  @override
  Future<ExchangeCallResult> setPrice(
    ExchangeServer server,
    ExchangeCancellation cancellation, {
    required String nomenclature,
    required num price,
  }) async {
    return _reserved;
  }
}
```

Создать `ds_tsd_protocol.dart`:

```dart
import 'dart:convert';

import 'package:http/http.dart' as http;

import '../../domain/exchange/exchange_call_result.dart';
import '../../domain/exchange/exchange_cancellation.dart';
import '../../domain/exchange/exchange_protocol.dart';
import '../../domain/exchange/exchange_server.dart';

/// Запросы DS TСД к публикации учётной базы. Один экземпляр обслуживает одну проверку связи.
class DsTsdProtocol implements ExchangeProtocol {
  DsTsdProtocol({http.Client? httpClient}) : _http = httpClient ?? http.Client();

  final http.Client _http;
  var _closed = false;

  @override
  Future<ExchangeCallResult> probe(ExchangeServer server, ExchangeCancellation cancellation) async {
    final blocked = probeTransportBlocked(server);
    if (blocked != null) {
      return blocked;
    }

    cancellation.onCancel(_closeClient);
    try {
      final response = await _http
          .get(_pingUri(server), headers: {'Authorization': _basicAuthorization(server.user, server.password)})
          .timeout(Duration(seconds: server.timeoutSeconds));
      if (response.statusCode == 200) {
        return ExchangeCallResult(success: true, body: response.body);
      }
      return ExchangeCallResult(
        success: false,
        error: 'Код состяния ответа ${response.statusCode}.\n${response.body}',
      );
    } catch (error) {
      return ExchangeCallResult(success: false, error: error.toString());
    } finally {
      _closeClient();
    }
  }

  @override
  Future<ExchangeCallResult> documentByBarcode(
    ExchangeServer server,
    ExchangeCancellation cancellation, {
    required String command,
    required String barcode,
  }) {
    return _pending();
  }

  @override
  Future<ExchangeCallResult> markStatus(
    ExchangeServer server,
    ExchangeCancellation cancellation, {
    required String mark,
  }) {
    return _pending();
  }

  @override
  Future<ExchangeCallResult> trueApiToken(ExchangeServer server, ExchangeCancellation cancellation) {
    return _pending();
  }

  @override
  Future<ExchangeCallResult> loadGoods(ExchangeServer server, ExchangeCancellation cancellation) {
    return _pending();
  }

  @override
  Future<ExchangeCallResult> uploadDocument(
    ExchangeServer server,
    ExchangeCancellation cancellation, {
    required Map<String, Object?> packet,
  }) {
    return _pending();
  }

  @override
  Future<ExchangeCallResult> setPrice(
    ExchangeServer server,
    ExchangeCancellation cancellation, {
    required String nomenclature,
    required num price,
  }) {
    return _pending();
  }

  Future<ExchangeCallResult> _pending() async {
    return const ExchangeCallResult(success: false, error: exchangeCommandPending);
  }

  void _closeClient() {
    if (_closed) {
      return;
    }
    _closed = true;
    _http.close();
  }

  Uri _pingUri(ExchangeServer server) {
    final scheme = server.secure ? 'https' : 'http';
    final publication = server.publication.trim().replaceAll(RegExp(r'^/+|/+$'), '');
    final prefix = publication.isEmpty ? '' : '/$publication';
    return Uri.parse('$scheme://${server.host}:${server.port}$prefix/dstsdapi/ping');
  }

  String _basicAuthorization(String user, String password) {
    return 'Basic ${base64Encode(utf8.encode('$user:$password'))}';
  }
}
```

Создать `exchange_protocols.dart`:

```dart
import 'package:http/http.dart' as http;

import '../../domain/exchange/exchange_protocol.dart';
import '../../domain/exchange/exchange_server.dart';
import '../../infrastructure/exchange/data_mobile_protocol.dart';
import '../../infrastructure/exchange/ds_tsd_protocol.dart';

/// Выбор реализации обмена по имени на карточке сервера.
class ExchangeProtocols {
  const ExchangeProtocols();

  ExchangeProtocol? byName(String name, {http.Client Function()? createClient}) {
    switch (name) {
      case defaultExchangeProtocol:
        return DsTsdProtocol(httpClient: createClient?.call());
      case dataMobileProtocol:
        return const DataMobileProtocol();
      default:
        return null;
    }
  }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run from `src/Presentation/TsdApp`:

```text
flutter test test/exchange_guard_test.dart test/exchange_protocol_test.dart
```

Expected: PASS. Если тест отмены зависает, `cancel` не закрыл клиент и `GateClient.send` не завершился.

- [ ] **Step 5: Commit**

```text
git add -- src/Presentation/TsdApp/lib/infrastructure/exchange/ds_tsd_protocol.dart src/Presentation/TsdApp/lib/infrastructure/exchange/data_mobile_protocol.dart src/Presentation/TsdApp/lib/application/exchange/exchange_protocols.dart src/Presentation/TsdApp/test/exchange_protocol_test.dart
git commit -m "feat: проверка связи ds-tsd и бронь Data Mobile"
```

---

### Task 3: Карточка вызывает проверку

**Files:**
- Modify: `src/Presentation/TsdApp/lib/presentation/settings/exchange_probe_screen.dart`
- Modify: `src/Presentation/TsdApp/lib/presentation/settings/exchange_server_form_screen.dart`
- Test: `src/Presentation/TsdApp/test/exchange_probe_screen_test.dart`

**Interfaces:**
- Consumes: `probeBlocked`, тексты и `ExchangeProtocol` из Task 1, `ExchangeProtocols.byName` из Task 2.
- Produces: `ExchangeServerFormScreen({String initialProtocol = defaultExchangeProtocol, ExchangeProtocol? Function(String name)? protocols})`. `protocols == null` значит `ExchangeProtocols().byName`. `ExchangeProbeScreen({required String addressLine, required Future<ExchangeCallResult> Function(ExchangeCancellation cancellation) probe})`. Успех и ошибка закрывают экран ожидания и показывают диалог на карточке. `null` из `Navigator.pop` — отмена, диалога нет. Пустой адрес, порт 0, таймаут 0 и неизвестный протокол диалог показывают сразу, экран «Ожидание...» не открывают. Список протокола — ровно `exchangeProtocolNames`. Неизвестное начальное имя в список не добавляется, в состояние карточки остаётся, и «Проверить» показывает `exchangeProtocolUnknown`.

- [ ] **Step 1: Write the failing test**

Создать `src/Presentation/TsdApp/test/exchange_probe_screen_test.dart`:

```dart
import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:tsd_app/domain/exchange/exchange_protocol.dart';
import 'package:tsd_app/domain/exchange/exchange_server.dart';
import 'package:tsd_app/presentation/settings/exchange_server_form_screen.dart';
import 'package:tsd_app/theme/webix_dark_theme.dart';

void main() {
  testWidgets('пустой адрес показывает ошибку без экрана ожидания', (tester) async {
    await _open(tester);

    await tester.tap(find.text('Проверить'));
    await tester.pumpAndSettle();

    expect(find.text('Ожидание...'), findsNothing);
    expect(find.text('$exchangeProbeFailedPrefix$exchangeAddressMissing'), findsOneWidget);
  });

  testWidgets('таймаут 0 показывает ошибку без экрана ожидания', (tester) async {
    await _open(tester);
    await _fill(tester, timeout: '0');

    await tester.tap(find.text('Проверить'));
    await tester.pumpAndSettle();

    expect(find.text('Ожидание...'), findsNothing);
    expect(find.text('$exchangeProbeFailedPrefix$exchangeTimeoutMissing'), findsOneWidget);
  });

  testWidgets('неизвестный протокол в сеть не уходит', (tester) async {
    var lookups = 0;
    await _open(
      tester,
      initialProtocol: 'Старый',
      protocols: (name) {
        lookups += 1;
        return ScriptedProtocol((server, cancellation) async => const ExchangeCallResult(success: true));
      },
    );
    await _fill(tester);

    await tester.tap(find.text('Проверить'));
    await tester.pumpAndSettle();

    expect(find.text('Ожидание...'), findsNothing);
    expect(find.text('$exchangeProbeFailedPrefix$exchangeProtocolUnknown'), findsOneWidget);
    expect(lookups, 0);
  });

  testWidgets('успех показывает адрес и диалог', (tester) async {
    final gate = Completer<ExchangeCallResult>();
    await _open(
      tester,
      protocols: (name) => ScriptedProtocol((server, cancellation) => gate.future),
    );
    await _fill(tester, publication: 'erp');

    await tester.tap(find.text('Проверить'));
    await tester.pumpAndSettle();
    expect(find.text('10.0.0.5:80/erp'), findsOneWidget);

    gate.complete(const ExchangeCallResult(success: true, body: 'pong'));
    await tester.pumpAndSettle();
    expect(find.text(exchangeProbeSuccess), findsOneWidget);
    expect(find.text('Ок'), findsOneWidget);
  });

  testWidgets('ошибка проверки показывается диалогом карточки', (tester) async {
    await _open(
      tester,
      protocols: (name) => ScriptedProtocol(
        (server, cancellation) async => const ExchangeCallResult(success: false, error: 'нет связи'),
      ),
    );
    await _fill(tester);

    await tester.tap(find.text('Проверить'));
    await tester.pumpAndSettle();
    expect(find.text('${exchangeProbeFailedPrefix}нет связи'), findsOneWidget);
  });

  testWidgets('отмена закрывает ожидание без сообщения', (tester) async {
    final gate = Completer<ExchangeCallResult>();
    await _open(
      tester,
      protocols: (name) => ScriptedProtocol((server, cancellation) {
        cancellation.onCancel(() {
          if (!gate.isCompleted) {
            gate.complete(const ExchangeCallResult(success: false, error: 'closed'));
          }
        });
        return gate.future;
      }),
    );
    await _fill(tester);

    await tester.tap(find.text('Проверить'));
    await tester.pumpAndSettle();
    expect(find.text('Ожидание...'), findsOneWidget);

    await tester.tap(find.text('Отмена'));
    await tester.pumpAndSettle();

    expect(find.text('Ожидание...'), findsNothing);
    expect(find.textContaining('Проверка связи'), findsNothing);
    expect(find.text('Сервер обмена (создание)'), findsOneWidget);
  });

  testWidgets('в списке оба протокола, Data Mobile возвращает бронь', (tester) async {
    await _open(tester);
    await tester.tap(find.byType(DropdownButtonFormField<String>));
    await tester.pumpAndSettle();

    expect(find.text(dataMobileProtocol).hitTestable(), findsOneWidget);
    await tester.tap(find.text(dataMobileProtocol).hitTestable());
    await tester.pumpAndSettle();
    await _fill(tester);

    await tester.tap(find.text('Проверить'));
    await tester.pumpAndSettle();

    expect(find.text('$exchangeProbeFailedPrefix$dataMobileReserved'), findsOneWidget);
  });
}

Future<void> _open(
  WidgetTester tester, {
  String initialProtocol = defaultExchangeProtocol,
  ExchangeProtocol? Function(String name)? protocols,
}) async {
  await tester.pumpWidget(
    MaterialApp(
      theme: WebixDarkTheme.data(),
      home: ExchangeServerFormScreen(initialProtocol: initialProtocol, protocols: protocols),
    ),
  );
}

Future<void> _fill(WidgetTester tester, {String timeout = '5', String publication = ''}) async {
  await tester.enterText(_field('IP-адрес'), '10.0.0.5');
  await tester.enterText(_field('Порт'), '80');
  if (publication.isNotEmpty) {
    await tester.enterText(_field('Имя публикации базы'), publication);
  }
  final timeoutField = _field('Таймаут запроса (сек)');
  await tester.ensureVisible(timeoutField);
  await tester.enterText(timeoutField, timeout);
  await tester.pump();
}

Finder _field(String label) {
  return find.byWidgetPredicate((widget) => widget is TextField && (widget.decoration?.labelText == label));
}

class ScriptedProtocol implements ExchangeProtocol {
  ScriptedProtocol(this.onProbe);

  final Future<ExchangeCallResult> Function(ExchangeServer server, ExchangeCancellation cancellation) onProbe;

  @override
  Future<ExchangeCallResult> probe(ExchangeServer server, ExchangeCancellation cancellation) {
    return onProbe(server, cancellation);
  }

  @override
  Future<ExchangeCallResult> documentByBarcode(
    ExchangeServer server,
    ExchangeCancellation cancellation, {
    required String command,
    required String barcode,
  }) async {
    return const ExchangeCallResult(success: false, error: exchangeCommandPending);
  }

  @override
  Future<ExchangeCallResult> markStatus(
    ExchangeServer server,
    ExchangeCancellation cancellation, {
    required String mark,
  }) async {
    return const ExchangeCallResult(success: false, error: exchangeCommandPending);
  }

  @override
  Future<ExchangeCallResult> trueApiToken(ExchangeServer server, ExchangeCancellation cancellation) async {
    return const ExchangeCallResult(success: false, error: exchangeCommandPending);
  }

  @override
  Future<ExchangeCallResult> loadGoods(ExchangeServer server, ExchangeCancellation cancellation) async {
    return const ExchangeCallResult(success: false, error: exchangeCommandPending);
  }

  @override
  Future<ExchangeCallResult> uploadDocument(
    ExchangeServer server,
    ExchangeCancellation cancellation, {
    required Map<String, Object?> packet,
  }) async {
    return const ExchangeCallResult(success: false, error: exchangeCommandPending);
  }

  @override
  Future<ExchangeCallResult> setPrice(
    ExchangeServer server,
    ExchangeCancellation cancellation, {
    required String nomenclature,
    required num price,
  }) async {
    return const ExchangeCallResult(success: false, error: exchangeCommandPending);
  }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run from `src/Presentation/TsdApp`:

```text
flutter test test/exchange_probe_screen_test.dart
```

Expected: FAIL, у `ExchangeServerFormScreen` нет параметров `initialProtocol` и `protocols`.

- [ ] **Step 3: Write minimal implementation**

Заменить `exchange_probe_screen.dart` целиком:

```dart
import 'package:flutter/material.dart';

import '../../domain/exchange/exchange_call_result.dart';
import '../../domain/exchange/exchange_cancellation.dart';
import '../../theme/webix_dark_theme.dart';
import '../shell/screen_commands.dart';

/// Ожидание проверки связи. Ответ возвращает результат, отмена — ничего.
class ExchangeProbeScreen extends StatefulWidget {
  const ExchangeProbeScreen({
    super.key,
    required this.addressLine,
    required this.probe,
  });

  final String addressLine;
  final Future<ExchangeCallResult> Function(ExchangeCancellation cancellation) probe;

  @override
  State<ExchangeProbeScreen> createState() => _ExchangeProbeScreenState();
}

class _ExchangeProbeScreenState extends State<ExchangeProbeScreen> {
  final _cancellation = ExchangeCancellation();
  var _left = false;

  @override
  void initState() {
    super.initState();
    _run();
  }

  Future<void> _run() async {
    final result = await widget.probe(_cancellation);
    if (!mounted || _left) {
      return;
    }
    Navigator.of(context).pop(result);
  }

  void _cancel() {
    _left = true;
    _cancellation.cancel();
    if (mounted) {
      Navigator.of(context).pop();
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        automaticallyImplyLeading: false,
        title: const Text('Ожидание...'),
      ),
      body: ColoredBox(
        color: WebixDarkTheme.background,
        child: SizedBox.expand(
          child: Column(
            children: [
              const SizedBox(height: 32),
              const Text('Проверка связи с сервером'),
              const SizedBox(height: 8),
              Text(widget.addressLine),
            ],
          ),
        ),
      ),
      bottomNavigationBar: ScreenCommands(
        commands: [
          ScreenCommand(label: 'Отмена', onPressed: _cancel),
        ],
      ),
    );
  }
}
```

В `exchange_server_form_screen.dart` добавить импорты:

```dart
import '../../application/exchange/exchange_protocols.dart';
import '../../domain/exchange/exchange_call_result.dart';
import '../../domain/exchange/exchange_protocol.dart';
import '../../theme/webix_dark_theme.dart';
```

Конструктор и поле состояния:

```dart
class ExchangeServerFormScreen extends StatefulWidget {
  const ExchangeServerFormScreen({
    super.key,
    this.initialProtocol = defaultExchangeProtocol,
    this.protocols,
  });

  final String initialProtocol;
  final ExchangeProtocol? Function(String name)? protocols;

  @override
  State<ExchangeServerFormScreen> createState() => _ExchangeServerFormScreenState();
}
```

В state оставить `var _protocol = defaultExchangeProtocol;` и добавить `initState` рядом с существующим `dispose`:

```dart
@override
void initState() {
  super.initState();
  _protocol = widget.initialProtocol;
}
```

Заменить метод `_probe` и добавить показ диалога. `_close` и `_server` оставить: «Закрыть» по-прежнему возвращает сервер, «Проверить» его не сохраняет.

```dart
ExchangeProtocol? _protocolFor(String name) {
  final lookup = widget.protocols;
  if (lookup != null) {
    return lookup(name);
  }
  return const ExchangeProtocols().byName(name);
}

Future<void> _probe() async {
  final server = ExchangeServer(
    id: 'probe',
    name: _name.text.trim(),
    host: _host.text.trim(),
    port: int.tryParse(_port.text.trim()) ?? 0,
    secure: _secure,
    publication: _publication.text.trim(),
    user: _user.text.trim(),
    password: _password.text,
    timeoutSeconds: int.tryParse(_timeout.text.trim()) ?? 0,
    protocol: _protocol,
  );
  final blocked = probeBlocked(server);
  if (blocked != null) {
    await _showProbe(blocked);
    return;
  }

  final protocol = _protocolFor(server.protocol);
  if (protocol == null) {
    await _showProbe(const ExchangeCallResult(success: false, error: exchangeProtocolUnknown));
    return;
  }

  final result = await Navigator.of(context).push<ExchangeCallResult>(
    MaterialPageRoute(
      builder: (_) => ExchangeProbeScreen(
        addressLine: server.endpoint,
        probe: (cancellation) => protocol.probe(server, cancellation),
      ),
    ),
  );
  if (!mounted || result == null) {
    return;
  }
  await _showProbe(result);
}

Future<void> _showProbe(ExchangeCallResult result) {
  final text = result.success ? exchangeProbeSuccess : '$exchangeProbeFailedPrefix${result.error}';
  return showDialog<void>(
    context: context,
    builder: (dialogContext) {
      return AlertDialog(
        content: Text(text),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(dialogContext).pop(),
            child: const Text('Ок', style: TextStyle(color: WebixDarkTheme.accent)),
          ),
        ],
      );
    },
  );
}
```

Заменить список пунктов протокола:

```dart
DropdownButtonFormField<String>(
  initialValue: exchangeProtocolNames.contains(_protocol) ? _protocol : null,
  decoration: const InputDecoration(labelText: 'Протокол обмена'),
  items: [
    for (final name in exchangeProtocolNames)
      DropdownMenuItem(value: name, child: Text(name)),
  ],
  onChanged: (value) {
    if (value == null) {
      return;
    }
    setState(() => _protocol = value);
  },
),
```

- [ ] **Step 4: Run test to verify it passes**

Run from `src/Presentation/TsdApp`:

```text
flutter test
```

Expected: PASS, весь набор, включая прежние тесты оболочки.

- [ ] **Step 5: Commit**

```text
git add -- src/Presentation/TsdApp/lib/presentation/settings/exchange_probe_screen.dart src/Presentation/TsdApp/lib/presentation/settings/exchange_server_form_screen.dart src/Presentation/TsdApp/test/exchange_probe_screen_test.dart
git commit -m "feat: карточка сервера обмена проверяет связь"
```
