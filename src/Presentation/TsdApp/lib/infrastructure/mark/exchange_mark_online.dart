import 'dart:convert';

import 'package:http/http.dart' as http;

import '../../application/exchange/exchange_protocols.dart';
import '../../domain/documents/scanned_document.dart';
import '../../domain/exchange/exchange_call_result.dart';
import '../../domain/exchange/exchange_cancellation.dart';
import '../../domain/exchange/exchange_protocol.dart';
import '../../domain/exchange/exchange_server.dart';
import '../../domain/mark/mark_online.dart';
import '../../domain/message/failure_text.dart';
import '../settings/app_settings.dart';
import 'true_api_client.dart';

/// Ответ без токена: сервер ответил, но токена в теле нет.
const trueApiTokenMissing = 'Не получен токен True API';

/// Сетевые проверки подбора марки: владельца и токен отдаёт Fmu-Api, статус — сервер обмена.
class ExchangeMarkOnline implements MarkOnline {
  ExchangeMarkOnline({
    required this.settings,
    http.Client? httpClient,
    ExchangeProtocol? Function(String name)? protocol,
    TrueApiClient? trueApi,
  })  : _http = httpClient ?? http.Client(),
        _protocol = protocol ?? const ExchangeProtocols().byName,
        _trueApi = trueApi ?? TrueApiClient();

  final AppSettings settings;
  final http.Client _http;
  final ExchangeProtocol? Function(String name) _protocol;
  final TrueApiClient _trueApi;

  /// Сколько ждать Fmu-Api: у него нет таймаута в настройках, как у сервера обмена.
  static const fmuTimeout = Duration(seconds: 30);

  @override
  Future<MarkCheckAnswer> owner(String mark, ExchangeCancellation cancellation) async {
    final base = settings.apiBaseUrl.trim();
    if (base.isEmpty) {
      return const MarkCheckAnswer.rejected(exchangeAddressMissing);
    }

    try {
      final response = await _http
          .post(
            _uri(base, '/api/ts/checkMark'),
            headers: {'Content-Type': 'application/json'},
            body: jsonEncode([mark]),
          )
          .timeout(fmuTimeout);
      final text = utf8.decode(response.bodyBytes);
      if (response.statusCode != 200) {
        return const MarkCheckAnswer.rejected(markOwnerForeign);
      }

      for (final code in _codes(_json(text))) {
        if (flagOf(code['IsTracking']) && !flagOf(code['IsOwner'])) {
          return const MarkCheckAnswer.rejected(markOwnerForeign);
        }
      }
      return const MarkCheckAnswer.passed();
    } catch (error) {
      return MarkCheckAnswer.rejected(describeFailure(error));
    }
  }

  @override
  Future<MarkStatusAnswer> status(String mark, ExchangeCancellation cancellation) async {
    final call = await _exchange(
      cancellation,
      (protocol, server, cancel) => protocol.markStatus(server, cancel, mark: mark),
    );
    if (!call.success) {
      return MarkStatusAnswer.rejected(call.error);
    }

    final root = _object(_json(call.body));
    final data = _object(root?['Данные']) ?? _object(_object(root?['Пакет'])?['Данные']);
    if (data == null) {
      return const MarkStatusAnswer.rejected(markStatusBadFormat);
    }
    if (!flagOf(data['Успех'])) {
      final error = textOf(data['Ошибка']);
      return MarkStatusAnswer.rejected(error.isEmpty ? markStatusBadFormat : error);
    }
    return const MarkStatusAnswer.passed();
  }

  @override
  Future<MarkPackageAnswer> package(
    String mark,
    String token,
    ExchangeCancellation cancellation,
  ) async {
    if (cancellation.isCancelled) {
      return const MarkPackageAnswer(error: markCheckCancelled);
    }

    final answer = await _trueApi.packageOf(mark, token);
    return MarkPackageAnswer(
      package: answer.package,
      marks: answer.marks,
      error: answer.error,
    );
  }

  @override
  Future<TrueApiTokenAnswer> token(ExchangeCancellation cancellation, {String inn = ''}) async {
    final base = settings.apiBaseUrl.trim();
    if (base.isNotEmpty) {
      final organization = inn.trim();
      final path = organization.isEmpty
          ? '/api/ts/token'
          : '/api/ts/token/inn?inn=${Uri.encodeQueryComponent(organization)}';
      try {
        final response = await _http.get(_uri(base, path)).timeout(fmuTimeout);
        final text = utf8.decode(response.bodyBytes);
        if (response.statusCode != 200) {
          return TrueApiTokenAnswer(
            token: '',
            error: describeHttpStatus(response.statusCode, text),
          );
        }
        final body = _object(_json(text));
        return _tokenAnswer(textOf(body?['token']), body?['expired']);
      } catch (error) {
        return TrueApiTokenAnswer(token: '', error: describeFailure(error));
      }
    }

    final call = await _exchange(
      cancellation,
      (protocol, server, cancel) => protocol.trueApiToken(server, cancel),
    );
    if (!call.success) {
      return TrueApiTokenAnswer(token: '', error: call.error);
    }

    final body = _object(_json(call.body));
    return _tokenAnswer(textOf(body?['Токен']), body?['ДействуетДо']);
  }

  Future<ExchangeCallResult> _exchange(
    ExchangeCancellation cancellation,
    Future<ExchangeCallResult> Function(
      ExchangeProtocol protocol,
      ExchangeServer server,
      ExchangeCancellation cancellation,
    ) run,
  ) async {
    if (cancellation.isCancelled) {
      return const ExchangeCallResult(success: false, error: markCheckCancelled);
    }

    final server = settings.exchangeServer;
    if (server == null) {
      return const ExchangeCallResult(success: false, error: exchangeServerMissing);
    }

    final blocked = probeBlocked(server);
    if (blocked != null) {
      return blocked;
    }

    final protocol = _protocol(server.protocol);
    if (protocol == null) {
      return const ExchangeCallResult(success: false, error: exchangeProtocolUnknown);
    }

    return run(protocol, server, cancellation);
  }

  static TrueApiTokenAnswer _tokenAnswer(String token, Object? until) {
    final expiresAt = _date(until);
    if (token.isEmpty) {
      return const TrueApiTokenAnswer(token: '', error: trueApiTokenMissing);
    }
    if (expiresAt != null && expiresAt.isBefore(DateTime.now())) {
      return const TrueApiTokenAnswer(token: '', error: markTokenExpired);
    }
    return TrueApiTokenAnswer(token: token, expiresAt: expiresAt);
  }

  static Uri _uri(String base, String path) {
    final normalized = base.endsWith('/') ? base.substring(0, base.length - 1) : base;
    return Uri.parse('$normalized$path');
  }

  static DateTime? _date(Object? value) {
    final text = textOf(value);
    return text.isEmpty ? null : DateTime.tryParse(text);
  }

  /// Элементы `Codes`: по ним видно, отслеживается ли марка и наша ли она.
  static List<Map<String, Object?>> _codes(Object? body) {
    final codes = _object(body)?['Codes'];
    if (codes is! List) {
      return const [];
    }

    final items = <Map<String, Object?>>[];
    for (final item in codes) {
      final code = _object(item);
      if (code != null) {
        items.add(code);
      }
    }
    return items;
  }

  static Object? _json(String body) {
    try {
      return jsonDecode(body);
    } catch (_) {
      return null;
    }
  }

  /// `Пакет` и `Данные` в ответах 1С бывают и объектом, и строкой JSON.
  static Map<String, Object?>? _object(Object? value) {
    final parsed = value is String ? _json(value) : value;
    if (parsed is! Map) {
      return null;
    }

    return {for (final item in parsed.entries) '${item.key}': item.value};
  }
}
