/// Нормализация и распознавание кода марки из буфера обмена.
class MarkCode {
  static String normalize(String value) {
    return value.replaceAll('\u241d', '\x1d').replaceAll(RegExp(r'[\r\n\t]+'), '').trim();
  }

  static bool looksLikeMark(String code) {
    final normalized = normalize(code);
    if (normalized.length < 14) {
      return false;
    }

    if (normalized.contains('\x1d')) {
      return true;
    }

    if (normalized.startsWith('01') && normalized.length >= 18) {
      return true;
    }

    return RegExp(r'^\d{13,14}$').hasMatch(normalized) || normalized.length >= 25;
  }
}
