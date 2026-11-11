import 'dart:convert';

import 'package:http/http.dart' as http;

import '../../domain/exchange/exchange_call_result.dart';
import '../../domain/exchange/exchange_cancellation.dart';
import '../../domain/exchange/exchange_protocol.dart';
import '../../domain/exchange/exchange_server.dart';
import '../../domain/message/failure_text.dart';

/// Запросы DS TСД к публикации учётной базы. Один экземпляр обслуживает одну проверку связи.
class DsTsdProtocol implements ExchangeProtocol {
  DsTsdProtocol({http.Client? httpClient}) : _http = httpClient ?? http.Client();

  final http.Client _http;
  var _closed = false;

  @override
  Future<ExchangeCallResult> probe(ExchangeServer server, ExchangeCancellation cancellation) {
    return _get(server, cancellation, _apiUri(server, 'ping'));
  }

  @override
  Future<ExchangeCallResult> documentByBarcode(
    ExchangeServer server,
    ExchangeCancellation cancellation, {
    required String command,
    required String barcode,
  }) {
    return _get(
      server,
      cancellation,
      _apiUri(server, 'get', query: {'Команда': command, 'Штрихкод': barcode}),
    );
  }

  @override
  Future<ExchangeCallResult> markStatus(
    ExchangeServer server,
    ExchangeCancellation cancellation, {
    required String mark,
  }) {
    return _get(
      server,
      cancellation,
      _apiUri(server, 'get', query: {'Команда': markStatusCommand, 'Марка': mark}),
    );
  }

  @override
  Future<ExchangeCallResult> trueApiToken(ExchangeServer server, ExchangeCancellation cancellation) {
    return _get(
      server,
      cancellation,
      _apiUri(server, 'get', query: {'Команда': trueApiTokenCommand}),
    );
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
    return _post(server, cancellation, _apiUri(server, 'post'), packet);
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

  /// Команда, которая есть в сети. Пустой адрес, порт 0 и таймаут 0 её не отправляют.
  Future<ExchangeCallResult> _get(ExchangeServer server, ExchangeCancellation cancellation, Uri uri) async {
    final blocked = probeTransportBlocked(server);
    if (blocked != null) {
      return blocked;
    }

    cancellation.onCancel(_closeClient);
    try {
      final response = await _http
          .get(uri, headers: {'Authorization': _basicAuthorization(server.user, server.password)})
          .timeout(Duration(seconds: server.timeoutSeconds));
      final text = utf8.decode(response.bodyBytes);
      if (response.statusCode == 200) {
        return ExchangeCallResult(success: true, body: text);
      }
      return ExchangeCallResult(
        success: false,
        error: describeHttpStatus(response.statusCode, text),
        details: text,
      );
    } catch (error) {
      return ExchangeCallResult(
        success: false,
        error: describeFailure(error),
        details: '$error',
      );
    } finally {
      _closeClient();
    }
  }

  /// Выгрузка отправляет JSON пакета телом запроса; успех — тот же код 200.
  Future<ExchangeCallResult> _post(
    ExchangeServer server,
    ExchangeCancellation cancellation,
    Uri uri,
    Map<String, Object?> packet,
  ) async {
    final blocked = probeTransportBlocked(server);
    if (blocked != null) {
      return blocked;
    }

    cancellation.onCancel(_closeClient);
    try {
      final response = await _http
          .post(
            uri,
            headers: {
              'Authorization': _basicAuthorization(server.user, server.password),
              'Content-Type': 'application/json',
            },
            body: jsonEncode(packet),
          )
          .timeout(Duration(seconds: server.timeoutSeconds));
      final text = utf8.decode(response.bodyBytes);
      if (response.statusCode == 200) {
        return ExchangeCallResult(success: true, body: text);
      }
      return ExchangeCallResult(
        success: false,
        error: describeHttpStatus(response.statusCode, text),
        details: text,
      );
    } catch (error) {
      return ExchangeCallResult(
        success: false,
        error: describeFailure(error),
        details: '$error',
      );
    } finally {
      _closeClient();
    }
  }

  void _closeClient() {
    if (_closed) {
      return;
    }
    _closed = true;
    _http.close();
  }

  /// Адрес команды ds-tsd: сегмент публикации ставится между портом и `dstsdapi`.
  Uri _apiUri(ExchangeServer server, String path, {Map<String, String>? query}) {
    final scheme = server.secure ? 'https' : 'http';
    final publication = server.publication.trim().replaceAll(RegExp(r'^/+|/+$'), '');
    final prefix = publication.isEmpty ? '' : '/$publication';
    final uri = Uri.parse('$scheme://${server.host}:${server.port}$prefix/dstsdapi/$path');
    return query == null ? uri : uri.replace(queryParameters: query);
  }

  String _basicAuthorization(String user, String password) {
    return 'Basic ${base64Encode(utf8.encode('$user:$password'))}';
  }
}
