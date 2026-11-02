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
      final text = utf8.decode(response.bodyBytes);
      if (response.statusCode == 200) {
        return ExchangeCallResult(success: true, body: text);
      }
      return ExchangeCallResult(
        success: false,
        error: 'Код состяния ответа ${response.statusCode}.\n$text',
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
