import 'dart:convert';
import 'dart:io';

import 'package:http/http.dart' as http;

import 'app_update.dart';

/// Запрос метаданных и файла обновления у Fmu-Api.
class AppUpdateClient {
  AppUpdateClient({http.Client? httpClient}) : _http = httpClient ?? http.Client();

  final http.Client _http;

  Future<AppUpdate?> latest({required String baseUrl, required String abi}) async {
    final response = await _http.get(_uri(baseUrl, '/api/tsd/update?abi=${Uri.encodeQueryComponent(abi)}'));
    if (response.statusCode == 404) {
      return null;
    }
    if (response.statusCode != 200) {
      throw Exception('Не удалось проверить обновление: ${response.statusCode}');
    }

    final update = AppUpdate.parse(jsonDecode(response.body), abi: abi);
    if (update == null) {
      throw Exception('Некорректный ответ проверки обновления');
    }

    return update;
  }

  Future<File> download({
    required String baseUrl,
    required AppUpdate update,
    required String directory,
  }) async {
    final response = await _http.send(http.Request('GET', _uri(baseUrl, update.url)));
    if (response.statusCode != 200) {
      throw Exception('Не удалось скачать обновление: ${response.statusCode}');
    }

    final file = File('$directory${Platform.pathSeparator}${update.fileName}');
    await response.stream.pipe(file.openWrite());
    return file;
  }

  Uri _uri(String baseUrl, String path) {
    if (path.startsWith('http://') || path.startsWith('https://')) {
      return Uri.parse(path);
    }

    final normalized = baseUrl.endsWith('/') ? baseUrl.substring(0, baseUrl.length - 1) : baseUrl;
    final suffix = path.startsWith('/') ? path : '/$path';
    return Uri.parse('$normalized$suffix');
  }
}
