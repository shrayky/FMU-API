import '../../domain/mark/mark_card_row.dart';

export '../../domain/mark/mark_card_row.dart';

const _statusLabels = <String, String>{
  'EMITTED': 'Эмитирован',
  'APPLIED': 'Нанесён',
  'INTRODUCED': 'В обороте',
  'WRITTEN_OFF': 'Списан',
  'RETIRED': 'Выбыл',
  'WITHDRAWN': 'Изъят',
  'DISAGGREGATION': 'Расформирован',
  'DISAGGREGATED': 'Расформирован',
  'WAIT_SHIPMENT': 'Ожидает отгрузку',
  'EXPORTED': 'Экспортирован',
};

/// Собирает строки карточки из ответов True API и разрешительного режима.
class MarkCardMapper {
  static List<MarkCardRow> fromResponses({
    required Map<String, dynamic>? trueApi,
    required Map<String, dynamic>? permissive,
  }) {
    final rows = <MarkCardRow>[];
    rows.addAll(_trueApiErrors(trueApi));

    final info = _cisInfo(trueApi);
    if (info != null) {
      final expireRaw = info['expireDate'] ?? info['expirationDate'];
      final verified = _verified(permissive);

      _add(rows, 'Название', _text(info['productName']));
      _add(rows, 'Статус', _status(info), _statusTone(info));
      _add(rows, 'Срок годности', _formatDate(expireRaw), _expireTone(expireRaw));
      _add(rows, 'Владелец', _owner(info));
      _add(rows, 'Бренд', _text(info['brand']));
      _add(rows, 'Производитель', _producer(info));
      _add(rows, 'Дата производства', _formatDate(info['producedDate'] ?? info['productionDate']));
      _add(rows, 'Криптозащита КМ', _crypto(verified), _cryptoTone(verified));
      _add(rows, 'GTIN', _text(info['gtin']));
      _add(rows, 'ТН ВЭД', _text(info['tnVedEaes']));
      _add(rows, 'Заявленный объём / вес нетто', _weight(info));
    }

    _addPermissiveError(rows, permissive);
    return rows;
  }

  static void _add(List<MarkCardRow> rows, String label, String value, [MarkCardTone tone = MarkCardTone.none]) {
    if (value.trim().isEmpty) {
      return;
    }

    rows.add(MarkCardRow(label: label, value: value, tone: tone));
  }

  static List<MarkCardRow> _trueApiErrors(Map<String, dynamic>? trueApi) {
    final rows = <MarkCardRow>[];
    if (trueApi != null && trueApi['status'] != 'ok') {
      final reason = _text(trueApi['reason']).isEmpty ? 'нет данных True API' : _text(trueApi['reason']);
      rows.add(MarkCardRow(
        label: '',
        value: 'True API: $reason',
        tone: MarkCardTone.error,
      ));
    }

    final itemError = _text(_firstItem(trueApi)?['errorMessage']);
    if (itemError.isNotEmpty) {
      rows.add(MarkCardRow(label: '', value: itemError, tone: MarkCardTone.error));
    }

    return rows;
  }

  static void _addPermissiveError(List<MarkCardRow> rows, Map<String, dynamic>? permissive) {
    final permissiveError = _text(permissive?['error']);
    if (permissiveError.isEmpty) {
      return;
    }

    rows.add(MarkCardRow(
      label: '',
      value: 'Разрешительный режим: $permissiveError',
    ));
  }

  static Map<String, dynamic>? _cisInfo(Map<String, dynamic>? trueApi) {
    if (trueApi == null || trueApi['status'] != 'ok') {
      return null;
    }

    final item = _firstItem(trueApi);
    final info = item?['cisInfo'];
    if (info is Map<String, dynamic>) {
      return info;
    }

    return null;
  }

  static Map<String, dynamic>? _firstItem(Map<String, dynamic>? trueApi) {
    final data = trueApi?['data'];
    if (data is! List || data.isEmpty) {
      return null;
    }

    for (final entry in data) {
      if (entry is Map<String, dynamic> && entry['cisInfo'] != null) {
        return entry;
      }
    }

    final first = data.first;
    return first is Map<String, dynamic> ? first : null;
  }

  static bool? _verified(Map<String, dynamic>? permissive) {
    final codes = permissive?['truemark_response']?['codes'] ??
        permissive?['truemark_responses']?[0]?['response']?['codes'];
    if (codes is! List || codes.isEmpty) {
      return null;
    }

    final first = codes.first;
    if (first is! Map || first['verified'] == null) {
      return null;
    }

    return first['verified'] == true;
  }

  static String _status(Map<String, dynamic> info) {
    if (info['markWithdraw'] == true && _text(info['status']).isEmpty) {
      return 'Выбыл';
    }

    final status = _text(info['status']);
    if (status.isEmpty) {
      return '';
    }

    return _statusLabels[status] ?? status;
  }

  static MarkCardTone _statusTone(Map<String, dynamic> info) {
    if (info['status'] == 'INTRODUCED') {
      return MarkCardTone.ok;
    }

    if (_text(info['status']).isNotEmpty || info['markWithdraw'] == true) {
      return MarkCardTone.warn;
    }

    return MarkCardTone.none;
  }

  static String _owner(Map<String, dynamic> info) {
    final name = _text(info['ownerName']);
    final inn = _text(info['ownerInn']);
    if (name.isNotEmpty && inn.isNotEmpty) {
      return '$name, ИНН $inn';
    }

    return name.isNotEmpty ? name : inn;
  }

  static String _producer(Map<String, dynamic> info) {
    final name = _text(info['producerName']);
    return name.isNotEmpty ? name : _text(info['producerInn']);
  }

  static String _crypto(bool? verified) {
    if (verified == null) {
      return '';
    }

    return verified ? 'Проверено' : 'Не проверено';
  }

  static MarkCardTone _cryptoTone(bool? verified) {
    if (verified == null) {
      return MarkCardTone.none;
    }

    return verified ? MarkCardTone.ok : MarkCardTone.warn;
  }

  static String _weight(Map<String, dynamic> info) {
    final volume = _text(info['volumeWeight']);
    if (volume.isNotEmpty) {
      return volume;
    }

    final raw = info['productWeight'];
    if (raw == null || raw == '') {
      return '';
    }

    if (raw is num) {
      return raw.truncateToDouble() == raw.toDouble() ? '${raw.toInt()} г' : '$raw г';
    }

    return '$raw г';
  }

  static MarkCardTone _expireTone(Object? value) {
    final date = _parseDate(value);
    if (date == null) {
      return MarkCardTone.ok;
    }

    return date.isBefore(DateTime.now()) ? MarkCardTone.warn : MarkCardTone.ok;
  }

  static String _formatDate(Object? value) {
    final date = _parseDate(value);
    if (date == null) {
      return _text(value);
    }

    final day = date.day.toString().padLeft(2, '0');
    final month = date.month.toString().padLeft(2, '0');
    return '$day.$month.${date.year}';
  }

  static DateTime? _parseDate(Object? value) {
    if (value == null) {
      return null;
    }

    if (value is DateTime) {
      return value;
    }

    return DateTime.tryParse(value.toString());
  }

  static String _text(Object? value) {
    if (value == null) {
      return '';
    }

    return value.toString().trim();
  }
}
