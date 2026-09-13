/// Результат добавления символа или готового кода в буфер сканера.
class ScanPushResult {
  const ScanPushResult._(this.code);

  final String? code;

  bool get isComplete => code != null;
}

/// Накапливает ввод ТСД: GS остаётся в коде, завершение только по CR.
class ScanBuffer {
  final StringBuffer _buffer = StringBuffer();

  String get current => _buffer.toString();

  ScanPushResult pushChar(String char) {
    if (char == '\r' || char == '\n') {
      return _finish();
    }

    _buffer.write(char);
    return const ScanPushResult._(null);
  }

  ScanPushResult completeRaw(String code) {
    _buffer.clear();
    final trimmed = code.trim();
    if (trimmed.isEmpty) {
      return const ScanPushResult._(null);
    }

    return ScanPushResult._(trimmed);
  }

  ScanPushResult _finish() {
    final code = _buffer.toString();
    _buffer.clear();
    if (code.isEmpty) {
      return const ScanPushResult._(null);
    }

    return ScanPushResult._(code);
  }
}
