import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:tsd_app/domain/exchange/exchange_call_result.dart';
import 'package:tsd_app/domain/exchange/exchange_cancellation.dart';
import 'package:tsd_app/domain/exchange/exchange_protocol.dart';
import 'package:tsd_app/domain/exchange/exchange_server.dart';
import 'package:tsd_app/domain/mark/mark_online.dart';
import 'package:tsd_app/infrastructure/mark/exchange_mark_online.dart';
import 'package:tsd_app/infrastructure/settings/app_settings.dart';

import 'documents_support.dart';

void main() {
  test('владелец проверяется POST-массивом из одной марки', () async {
    final client = _Client(200, body: '{"Codes": [{"IsTracking": false}]}');
    final online = ExchangeMarkOnline(
      settings: const AppSettings(apiBaseUrl: 'http://10.0.0.5:2578'),
      httpClient: client,
    );

    final answer = await online.owner('010460000000000121abc', ExchangeCancellation());

    expect(answer.passed, isTrue);
    expect(client.request?.method, 'POST');
    expect(client.request?.url.path, '/api/ts/checkMark');
    expect(client.bodyText, jsonEncode(['010460000000000121abc']));
  });

  test('отслеживаемая марка без владельца считается чужой', () async {
    final client = _Client(200, body: '{"Codes": [{"IsTracking": true, "IsOwner": false}]}');
    final online = ExchangeMarkOnline(
      settings: const AppSettings(apiBaseUrl: 'http://10.0.0.5:2578'),
      httpClient: client,
    );

    final answer = await online.owner('010460000000000121abc', ExchangeCancellation());

    expect(answer.passed, isFalse);
    expect(answer.error, markOwnerForeign);
  });

  test('код не 200 считает марку чужой', () async {
    final client = _Client(404, body: 'нет');
    final online = ExchangeMarkOnline(
      settings: const AppSettings(apiBaseUrl: 'http://10.0.0.5:2578'),
      httpClient: client,
    );

    final answer = await online.owner('010460000000000121abc', ExchangeCancellation());

    expect(answer.error, markOwnerForeign);
  });

  test('пустой адрес Fmu-Api не пускает проверку владельца в сеть', () async {
    final client = _Client(200, body: '{}');
    final online = ExchangeMarkOnline(settings: const AppSettings(), httpClient: client);

    final answer = await online.owner('010460000000000121abc', ExchangeCancellation());

    expect(answer.error, exchangeAddressMissing);
    expect(client.sent, 0);
  });

  test('успешный статус подбора не несёт упаковку', () async {
    final online = ExchangeMarkOnline(
      settings: settingsWithServer(),
      httpClient: _Client(200),
      protocol: (_) => _StatusProtocol(
        const ExchangeCallResult(success: true, body: '{"Данные": {"Успех": true}}'),
      ),
    );

    final answer = await online.status('010460000000000121abc', ExchangeCancellation());

    expect(answer.passed, isTrue);
  });

  test('отказ статуса берёт Ошибку из Данные', () async {
    final online = ExchangeMarkOnline(
      settings: settingsWithServer(),
      httpClient: _Client(200),
      protocol: (_) => _StatusProtocol(
        const ExchangeCallResult(
          success: true,
          body: '{"Данные": {"Успех": false, "Ошибка": "Марка не найдена"}}',
        ),
      ),
    );

    final answer = await online.status('010460000000000121abc', ExchangeCancellation());

    expect(answer.passed, isFalse);
    expect(answer.error, 'Марка не найдена');
  });

  test('безымянный отказ статуса даёт текст про формат ответа', () async {
    final online = ExchangeMarkOnline(
      settings: settingsWithServer(),
      httpClient: _Client(200),
      protocol: (_) => _StatusProtocol(
        const ExchangeCallResult(success: true, body: '{"Данные": {"Успех": false}}'),
      ),
    );

    final answer = await online.status('010460000000000121abc', ExchangeCancellation());

    expect(answer.error, markStatusBadFormat);
  });

  test('токен читается у адреса проверки марок и срок проверяется', () async {
    final expired = DateTime.now().subtract(const Duration(days: 1)).toIso8601String();
    final online = ExchangeMarkOnline(
      settings: const AppSettings(apiBaseUrl: 'http://10.0.0.5:2578'),
      httpClient: _Client(200, body: '{"token": "abc", "expired": "$expired"}'),
    );

    final answer = await online.token(ExchangeCancellation());

    expect(answer.passed, isFalse);
    expect(answer.error, markTokenExpired);
  });

  test('живой токен приходит с адресом проверки марок', () async {
    final until = DateTime.now().add(const Duration(days: 1)).toIso8601String();
    final client = _Client(200, body: '{"token": "abc", "expired": "$until"}');
    final online = ExchangeMarkOnline(
      settings: const AppSettings(apiBaseUrl: 'http://10.0.0.5:2578'),
      httpClient: client,
    );

    final answer = await online.token(ExchangeCancellation());

    expect(answer.passed, isTrue);
    expect(answer.token, 'abc');
    expect(client.request?.url.path, '/api/ts/token');
  });

  test('токен запрашивается по ИНН организации документа', () async {
    final until = DateTime.now().add(const Duration(days: 1)).toIso8601String();
    final client = _Client(200, body: '{"token": "abc", "expired": "$until"}');
    final online = ExchangeMarkOnline(
      settings: const AppSettings(apiBaseUrl: 'http://10.0.0.5:2578'),
      httpClient: client,
    );

    await online.token(ExchangeCancellation(), inn: '246412218294');

    expect(client.request?.url.path, '/api/ts/token/inn');
    expect(client.request?.url.queryParameters['inn'], '246412218294');
  });

  test('без ИНН токен берётся общим адресом', () async {
    final until = DateTime.now().add(const Duration(days: 1)).toIso8601String();
    final client = _Client(200, body: '{"token": "abc", "expired": "$until"}');
    final online = ExchangeMarkOnline(
      settings: const AppSettings(apiBaseUrl: 'http://10.0.0.5:2578'),
      httpClient: client,
    );

    await online.token(ExchangeCancellation());

    expect(client.request?.url.path, '/api/ts/token');
  });

  test('без адреса токен берётся у сервера обмена командой ТокенТруАпи', () async {
    final online = ExchangeMarkOnline(
      settings: settingsWithServer(),
      httpClient: _Client(200),
      protocol: (_) => _StatusProtocol(
        const ExchangeCallResult(
          success: true,
          body: '{"Токен": "t", "ДействуетДо": "2099-01-01T00:00:00"}',
        ),
      ),
    );

    final answer = await online.token(ExchangeCancellation());

    expect(answer.passed, isTrue);
    expect(answer.token, 't');
  });

  test('Data Mobile статус марки не вычисляет', () async {
    final online = ExchangeMarkOnline(
      settings: settingsWithServer(protocol: dataMobileProtocol),
      httpClient: _Client(200),
    );

    final answer = await online.status('010460000000000121abc', ExchangeCancellation());

    expect(answer.passed, isFalse);
    expect(answer.error, dataMobileReserved);
  });
}

/// HTTP-клиент для тестов: запоминает запрос и отдаёт заданный код и тело.
class _Client extends http.BaseClient {
  _Client(this.status, {this.body = ''});

  final int status;
  final String body;
  var sent = 0;
  http.BaseRequest? request;
  var bodyText = '';

  @override
  Future<http.StreamedResponse> send(http.BaseRequest request) async {
    sent += 1;
    this.request = request;
    if (request is http.Request) {
      bodyText = request.body;
    }
    return http.StreamedResponse(Stream.value(utf8.encode(body)), status);
  }

  @override
  void close() {}
}

/// Протокол обмена для тестов статуса и токена: отдаёт заданный ответ.
class _StatusProtocol implements ExchangeProtocol {
  _StatusProtocol(this.answer);

  final ExchangeCallResult answer;

  @override
  Future<ExchangeCallResult> markStatus(
    ExchangeServer server,
    ExchangeCancellation cancellation, {
    required String mark,
  }) async {
    return answer;
  }

  @override
  Future<ExchangeCallResult> trueApiToken(ExchangeServer server, ExchangeCancellation cancellation) async {
    return answer;
  }

  @override
  Future<ExchangeCallResult> probe(ExchangeServer server, ExchangeCancellation cancellation) async => answer;

  @override
  Future<ExchangeCallResult> documentByBarcode(
    ExchangeServer server,
    ExchangeCancellation cancellation, {
    required String command,
    required String barcode,
  }) async {
    return answer;
  }

  @override
  Future<ExchangeCallResult> loadGoods(ExchangeServer server, ExchangeCancellation cancellation) async => answer;

  @override
  Future<ExchangeCallResult> uploadDocument(
    ExchangeServer server,
    ExchangeCancellation cancellation, {
    required Map<String, Object?> packet,
  }) async {
    return answer;
  }

  @override
  Future<ExchangeCallResult> setPrice(
    ExchangeServer server,
    ExchangeCancellation cancellation, {
    required String nomenclature,
    required num price,
  }) async {
    return answer;
  }
}
