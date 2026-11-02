/// Нормализация и распознавание кода марки: GS1 и штучный табак.
class MarkCode {
  static String normalize(String value) {
    return value.replaceAll('\u241d', '\x1d').replaceAll(RegExp(r'[\r\n\t]+'), '').trim();
  }

  static bool looksLikeMark(String code) {
    var normalized = normalize(code);
    if (normalized.startsWith('\x1d')) {
      normalized = normalized.substring(1);
    }

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
