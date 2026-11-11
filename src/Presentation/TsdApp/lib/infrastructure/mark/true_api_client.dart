import 'dart:convert';

import 'package:http/http.dart' as http;

import '../../domain/message/failure_text.dart';
import '../../domain/scan/mark_code.dart';

/// Адрес True API Честного знака: сведения о марке и история агрегации.
const trueApiBaseUrl = 'https://markirovka.crpt.ru';

/// Пакет марки по True API: код упаковки и марки, если история их отдала.
class TrueApiPackage {
  const TrueApiPackage({this.package = '', this.marks = const [], this.error = ''});

  final String package;
  final List<String> marks;
  final String error;
}

/// Запросы к True API по марке: `cises/info` и `cises/history/list`.
///
/// Прототип `ApiПроверкиМаркиTrueApiВызовСервера` ходит в Честный знак напрямую с Bearer-токеном:
/// `packageType = UNIT` у штучной марки, а код упаковки — `parent` последнего события агрегации.
class TrueApiClient {
  TrueApiClient({
    http.Client? httpClient,
    this.baseUrl = trueApiBaseUrl,
    this.timeout = const Duration(seconds: 30),
  }) : _http = httpClient ?? http.Client();

  final http.Client _http;
  final String baseUrl;
  final Duration timeout;

  static const _infoPath = '/api/v3/true-api/cises/info';
  static const _historyPath = '/api/v3/true-api/cises/history/list';

  /// Упаковка штучной марки и марки внутри неё.
  Future<TrueApiPackage> packageOf(String mark, String token) async {
    final head = markHead(mark);
    if (head.isEmpty || token.isEmpty) {
      return const TrueApiPackage();
    }

    try {
      final info = await _post(_infoPath, [head], token);
      if (!_isUnit(info)) {
        return const TrueApiPackage();
      }

      final package = await _parent(head, token);
      if (package.isEmpty) {
        return const TrueApiPackage();
      }

      return TrueApiPackage(package: package, marks: await _marksOf(package, token));
    } catch (error) {
      return TrueApiPackage(error: describeFailure(error));
    }
  }

  /// Первая часть марки до разделителя GS: её и знает True API.
  static String markHead(String mark) {
    return MarkCode.normalize(mark)
        .replaceAll(r'\u001d', '\x1d')
        .split('\x1d')
        .first
        .trim();
  }

  Future<String> _parent(String cis, String token) async {
    final events = _events(await _post(_historyPath, {'cis': cis}, token));
    var parent = '';
    for (final event in events) {
      final value = '${event['parent'] ?? ''}'.trim();
      if (value.isNotEmpty) {
        parent = value;
      }
    }
    return parent;
  }

  /// Марки упаковки: история агрегации отдаёт их, если упаковка собиралась из штук.
  Future<List<String>> _marksOf(String cis, String token) async {
    final events = _events(await _post(_historyPath, {'cis': cis}, token));
    final marks = <String>[];
    for (final event in events) {
      for (final key in const ['childs', 'children', 'cisList', 'sntins', 'codes']) {
        final value = event[key];
        if (value is! List) {
          continue;
        }
        for (final item in value) {
          final text = item is Map
              ? '${item['cis'] ?? item['code'] ?? item['sntin'] ?? ''}'.trim()
              : '$item'.trim();
          if (text.isNotEmpty && !marks.contains(text)) {
            marks.add(text);
          }
        }
      }
    }
    return marks;
  }

  /// `cises/info` отвечает массивом: у штучной марки `packageType` равен `UNIT`.
  static bool _isUnit(Object? answer) {
    final first = answer is List && answer.isNotEmpty ? answer.first : null;
    final info = _object(_object(first)?['cisInfo']);
    return '${info?['packageType'] ?? ''}'.trim().toUpperCase() == 'UNIT';
  }

  /// События истории агрегации: `cisAggregation` в ответе `cises/history/list`.
  static List<Map<String, Object?>> _events(Object? answer) {
    final events = _object(answer)?['cisAggregation'];
    if (events is! List) {
      return const [];
    }

    return [
      for (final event in events)
        if (_object(event) case final Map<String, Object?> item) item,
    ];
  }

  Future<Object?> _post(String path, Object body, String token) async {
    final base = baseUrl.replaceAll(RegExp(r'/+$'), '');
    final response = await _http
        .post(
          Uri.parse('$base$path'),
          headers: {
            'Content-Type': 'application/json',
            'Cache-Control': 'no-cache',
            'User-agent': 'ds-tsd',
            'Authorization': 'Bearer $token',
          },
          body: jsonEncode(body),
        )
        .timeout(timeout);
    final text = utf8.decode(response.bodyBytes);
    if (response.statusCode != 200) {
      throw Exception(describeHttpStatus(response.statusCode, text));
    }
    return _json(text);
  }

  static Object? _json(String body) {
    try {
      return jsonDecode(body);
    } catch (_) {
      return null;
    }
  }

  static Map<String, Object?>? _object(Object? value) {
    final parsed = value is String ? _json(value) : value;
    if (parsed is! Map) {
      return null;
    }

    return {for (final item in parsed.entries) '${item.key}': item.value};
  }
}
