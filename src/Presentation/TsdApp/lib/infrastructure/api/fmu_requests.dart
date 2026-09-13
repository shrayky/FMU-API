import 'dart:convert';

/// Тела запросов к Fmu-Api для проверки марки.
class FmuRequests {
  static Map<String, dynamic> documentCheck({
    required String inn,
    required String mark,
  }) {
    return {
      'positions': [
        {
          'organisation': {'inn': inn},
          'marking_codes': [base64Encode(utf8.encode(mark))],
        },
      ],
      'action': 'check',
      'type': 'receipt',
    };
  }

  static Map<String, dynamic> cisesInfo({
    required String inn,
    required String mark,
  }) {
    return {
      'inn': inn,
      'cises': [mark],
    };
  }

  static String innFromOrganisationConfig(Map<String, dynamic> config) {
    final groups = config['printGroups'] ?? config['PrintGroups'];
    if (groups is! List || groups.isEmpty) {
      return '';
    }

    final first = groups.first;
    if (first is! Map) {
      return '';
    }

    return (first['inn'] ?? first['INN'] ?? '').toString().trim();
  }
}
