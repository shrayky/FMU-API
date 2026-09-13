import 'dart:convert';

import 'package:http/http.dart' as http;

import '../../application/mark/mark_card_mapper.dart';
import 'fmu_requests.dart';

/// HTTP-клиент проверки марки через Fmu-Api.
class FmuApiClient {
  FmuApiClient({
    required this.baseUrl,
    http.Client? httpClient,
  }) : _http = httpClient ?? http.Client();

  final String baseUrl;
  final http.Client _http;

  Uri _uri(String path) {
    final normalized = baseUrl.endsWith('/') ? baseUrl.substring(0, baseUrl.length - 1) : baseUrl;
    return Uri.parse('$normalized$path');
  }

  Future<String> organisationInn() async {
    final response = await _http.get(_uri('/api/configuration/OrganisationConfig'));
    if (response.statusCode != 200) {
      throw Exception('Не удалось загрузить организации: ${response.statusCode}');
    }

    final body = jsonDecode(response.body);
    if (body is! Map<String, dynamic>) {
      return '';
    }

    return FmuRequests.innFromOrganisationConfig(body);
  }

  Future<List<MarkCardRow>> checkMark({
    required String inn,
    required String mark,
  }) async {
    final documentFuture = _postJson('/api/fmu/document', FmuRequests.documentCheck(inn: inn, mark: mark));
    final trueApiFuture = _postJson('/api/ts/cises/info', FmuRequests.cisesInfo(inn: inn, mark: mark));

    Map<String, dynamic> permissive;
    Map<String, dynamic> trueApi;

    try {
      permissive = await documentFuture;
    } catch (error) {
      permissive = {'error': error.toString()};
    }

    try {
      trueApi = await trueApiFuture;
    } catch (error) {
      trueApi = {'status': 'error', 'reason': error.toString()};
    }

    return MarkCardMapper.fromResponses(trueApi: trueApi, permissive: permissive);
  }

  Future<Map<String, dynamic>> _postJson(String path, Map<String, dynamic> body) async {
    final response = await _http
        .post(
          _uri(path),
          headers: {'Content-Type': 'application/json'},
          body: jsonEncode(body),
        )
        .timeout(const Duration(seconds: 30));

    if (response.statusCode < 200 || response.statusCode >= 300) {
      throw Exception('Ошибка сервера ${response.statusCode}: ${response.body}');
    }

    final decoded = jsonDecode(response.body);
    if (decoded is Map<String, dynamic>) {
      return decoded;
    }

    return {'value': decoded};
  }
}
