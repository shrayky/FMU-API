import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:tsd_app/infrastructure/mark/true_api_client.dart';

void main() {
  test('штучная марка получает упаковку из истории агрегации', () async {
    final client = _RouteClient({
      '/api/v3/true-api/cises/info': '[{"cisInfo": {"packageType": "UNIT"}}]',
      '/api/v3/true-api/cises/history/list': '{"cisAggregation": [{"parent": "0012345678905"}]}',
    });
    final api = TrueApiClient(httpClient: client);

    final answer = await api.packageOf('010460000000000121ABC\x1d93xyz', 'token-1');

    expect(answer.error, isEmpty);
    expect(answer.package, '0012345678905');
    expect(client.requests.first.method, 'POST');
    expect(client.requests.first.headers['Authorization'], 'Bearer token-1');
    expect(client.requests.first.body, jsonEncode(['010460000000000121ABC']));
  });

  test('марка не UNIT упаковки не даёт', () async {
    final client = _RouteClient({
      '/api/v3/true-api/cises/info': '[{"cisInfo": {"packageType": "GROUP"}}]',
    });
    final api = TrueApiClient(httpClient: client);

    final answer = await api.packageOf('0012345678905', 'token-1');

    expect(answer.package, isEmpty);
    expect(client.requests, hasLength(1));
  });

  test('история упаковки отдаёт её марки', () async {
    final client = _RouteClient({
      '/api/v3/true-api/cises/info': '[{"cisInfo": {"packageType": "UNIT"}}]',
      '/api/v3/true-api/cises/history/list': '''
        {"cisAggregation": [{
          "parent": "0012345678905",
          "childs": ["010460000000000121A1", "010460000000000121A2"]
        }]}
      ''',
    });
    final api = TrueApiClient(httpClient: client);

    final answer = await api.packageOf('010460000000000121ABC', 'token-1');

    expect(answer.package, '0012345678905');
    expect(answer.marks, ['010460000000000121A1', '010460000000000121A2']);
  });

  test('код ответа не 200 даёт текст протокола', () async {
    final client = _RouteClient({});
    final api = TrueApiClient(httpClient: client);

    final answer = await api.packageOf('010460000000000121ABC', 'token-1');

    expect(answer.error, contains('404'));
  });

  test('разделитель GS в заголовке марки отбрасывается', () {
    expect(TrueApiClient.markHead('010460000000000121ABC\u001d93xyz'), '010460000000000121ABC');
    expect(TrueApiClient.markHead(r'010460000000000121ABC\u001d93xyz'), '010460000000000121ABC');
  });
}

/// Клиент с ответами по пути запроса: остальные пути дают 404.
class _RouteClient extends http.BaseClient {
  _RouteClient(this.routes);

  final Map<String, String> routes;
  final requests = <http.Request>[];

  @override
  Future<http.StreamedResponse> send(http.BaseRequest request) async {
    final body = routes[request.url.path];
    if (request is http.Request) {
      requests.add(request);
    }
    return http.StreamedResponse(
      Stream.value(utf8.encode(body ?? 'нет')),
      body == null ? 404 : 200,
    );
  }

  @override
  void close() {}
}
