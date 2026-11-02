import 'dart:async';
import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:tsd_app/application/exchange/exchange_protocols.dart';
import 'package:tsd_app/domain/exchange/exchange_cancellation.dart';
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
