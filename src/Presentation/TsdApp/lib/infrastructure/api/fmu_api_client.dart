import 'dart:convert';

import 'package:http/http.dart' as http;

import '../../application/mark/mark_card_mapper.dart';
import '../../domain/documents/write_off_api.dart';
import '../../domain/mark/mark_check_result.dart';
import 'fmu_requests.dart';

/// HTTP-клиент проверки марки и списания через Fmu-Api.
class FmuApiClient implements WriteOffApi {
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

  @override
  Future<List<WriteOffOrganisation>> organisations() async {
    try {
      final response = await _http
          .get(_uri('/api/configuration/OrganisationConfig'))
          .timeout(const Duration(seconds: 30));

      if (response.statusCode != 200) {
        return const [];
      }

      final body = jsonDecode(response.body);
      if (body is! Map) {
        return const [];
      }

      final groups = body['printGroups'] ?? body['PrintGroups'];
      if (groups is! List) {
        return const [];
      }

      final organisations = <WriteOffOrganisation>[];
      for (final item in groups) {
        if (item is! Map) {
          continue;
        }

        final inn = '${item['inn'] ?? item['INN'] ?? ''}'.trim();
        final id = _intValue(item['id'] ?? item['Id']);
        if (inn.isEmpty) {
          continue;
        }

        final duplicate = id > 0
            ? organisations.any((organisation) => organisation.id == id)
            : organisations.any((organisation) => organisation.id <= 0 && organisation.inn == inn);
        if (duplicate) {
          continue;
        }

        organisations.add(
          WriteOffOrganisation(
            id: id,
            inn: inn,
            name: '${item['name'] ?? item['Name'] ?? ''}'.trim(),
            kpp: '${item['kpp'] ?? item['Kpp'] ?? item['KPP'] ?? ''}'.trim(),
          ),
        );
      }

      return organisations;
    } catch (_) {
      return const [];
    }
  }

  static int _intValue(Object? value) {
    if (value is num) {
      return value.toInt();
    }
    if (value is String) {
      return int.tryParse(value.trim()) ?? 0;
    }
    return 0;
  }

  @override
  Future<WriteOffMarkInfo?> markInfo({required String inn, required String mark}) async {
    if (inn.isEmpty || mark.isEmpty) {
      return null;
    }

    try {
      final response = await _http
          .post(
            _uri('/api/ts/cises/info'),
            headers: {'Content-Type': 'application/json'},
            body: jsonEncode({'inn': inn, 'cises': [mark]}),
          )
          .timeout(const Duration(seconds: 30));

      if (response.statusCode != 200) {
        return null;
      }

      final decoded = jsonDecode(response.body);
      if (decoded is! Map) {
        return null;
      }

      final data = decoded['data'];
      if (data is! List || data.isEmpty) {
        return null;
      }

      final item = data.first;
      if (item is! Map) {
        return null;
      }

      final info = item['cisInfo'];
      if (info is! Map) {
        return null;
      }

      return WriteOffMarkInfo(
        name: '${info['productName'] ?? ''}'.trim(),
        gtin: '${info['gtin'] ?? ''}'.trim(),
        quantity: _weightKg(info['productWeightGr'], info['productWeight']),
      );
    } catch (_) {
      return null;
    }
  }

  /// Масса марки в килограммах: граммы из `cises/info` точнее килограммов.
  static num? _weightKg(Object? grams, Object? kilograms) {
    num? value;
    if (grams is num && grams > 0) {
      value = grams / 1000;
    } else if (kilograms is num && kilograms > 0) {
      value = kilograms;
    }

    if (value == null || value <= 0) {
      return null;
    }

    return (value * 1000).round() / 1000;
  }

  @override
  Future<num?> retailPriceByBarcode(String barcode) async {
    if (barcode.isEmpty) {
      return null;
    }

    try {
      final response = await _http
          .get(_uri('/api/frontol/WarePriceByBarcode/${Uri.encodeComponent(barcode)}'))
          .timeout(const Duration(seconds: 30));

      if (response.statusCode != 200) {
        return null;
      }

      final decoded = jsonDecode(response.body);
      if (decoded is! Map) {
        return null;
      }

      final price = decoded['price'];
      if (price is num) {
        return price;
      }
      if (price is String) {
        return num.tryParse(price.trim());
      }
      return null;
    } catch (_) {
      return null;
    }
  }

  @override
  Future<void> createReceipt({
    required String inn,
    required String reason,
    required List<WriteOffReceiptLine> lines,
    int? printGroupId,
    String uid = '',
    String number = '',
    String terminalId = '',
  }) async {
    final response = await _http
        .post(
          _uri('/api/ts/gismt/journal/receipt'),
          headers: {'Content-Type': 'application/json'},
          body: jsonEncode({
            'inn': inn,
            'reason': reason,
            if (printGroupId != null && printGroupId > 0) 'printGroupId': printGroupId,
            if (uid.trim().isNotEmpty) 'uid': uid.trim(),
            if (number.trim().isNotEmpty) 'number': number.trim(),
            if (terminalId.trim().isNotEmpty) 'terminalId': terminalId.trim(),
            'lines': [
              for (final line in lines)
                {
                  'mark': line.mark,
                  'quantity': line.quantity,
                  if (line.price != null) 'price': line.price,
                },
            ],
          }),
        )
        .timeout(const Duration(seconds: 30));

    if (response.statusCode == 200) {
      return;
    }

    var error = '';
    try {
      final decoded = jsonDecode(response.body);
      if (decoded is Map) {
        error = '${decoded['error'] ?? ''}'.trim();
      }
    } catch (_) {
      error = '';
    }

    throw Exception(error.isEmpty ? 'Ошибка сервера ${response.statusCode}' : error);
  }

  Future<List<MarkCardRow>> checkMark({
    required String inn,
    required String mark,
  }) async {
    final result = await checkMarkResult(inn: inn, mark: mark);
    return result.rows;
  }

  Future<MarkCheckResult> checkMarkResult({
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

    return MarkCardMapper.checkResult(
      trueApi: trueApi,
      permissive: permissive,
      organisationInn: inn,
    );
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
