/// Нормализация и распознавание кода марки: GS1 и штучный табак.
class MarkCode {
  static String normalize(String value) {
    final text = value
        .replaceAll('\u241d', '\x1d')
        .replaceAll(r'\u001d', '\x1d')
        .replaceAll(RegExp(r'[\r\n\t]+'), '')
        .trim();
    return _trimInsignificant(_stripSymbology(text));
  }

  /// Сканеры ставят перед кодом идентификатор символики `]d2`, `]C1`, `]Q3`.
  static String _stripSymbology(String code) {
    final prefix = RegExp(r'^\][A-Za-z]\d').firstMatch(code);
    if (prefix == null) {
      return code;
    }
    return code.substring(prefix.end).trim();
  }

  /// По краям кода стоят служебные символы: GS, STX/RS, пробел, неразрывный пробел.
  /// Значимая часть марки начинается с идентификатора `01` или GTIN.
  static String _trimInsignificant(String code) {
    var start = 0;
    var end = code.length;
    while (start < end && _insignificant(code.codeUnitAt(start))) {
      start += 1;
    }
    while (end > start && _insignificant(code.codeUnitAt(end - 1))) {
      end -= 1;
    }
    return code.substring(start, end);
  }

  static bool _insignificant(int unit) => unit <= 0x20 || unit == 0x7f || unit == 0xa0;

  static bool looksLikeMark(String code) {
    final normalized = normalize(code);

    if (normalized.isEmpty) {
      return false;
    }

    if (_isGs1(normalized)) {
      return true;
    }

    if (normalized.startsWith('1') && _isGs1('0$normalized')) {
      return true;
    }

    return normalized.length == 29 && RegExp(r'^\d{14}').hasMatch(normalized);
  }

  static bool _isGs1(String code) {
    if (code.length < 19 || !code.startsWith('01') || code.substring(16, 18) != '21') {
      return false;
    }

    if (!RegExp(r'^\d{14}$').hasMatch(code.substring(2, 16))) {
      return false;
    }

    return !code.substring(18).startsWith('\x1d');
  }

  static String shortMark(String code) {
    final head = normalize(code).split('\x1d').first;
    final serial = _serial(head);
    final visible = serial.isNotEmpty ? serial : head;
    if (visible.length <= 16) {
      return visible;
    }

    return '${visible.substring(0, 16)}…';
  }

  static String _serial(String head) {
    if (!head.startsWith('01') || head.length < 18 || head.substring(16, 18) != '21') {
      return '';
    }

    return head.substring(18);
  }
}
