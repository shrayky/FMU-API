/// Уровень записи журнала работы.
enum WorkLogLevel { info, warning, error }

/// Запись журнала работы: время, уровень, текст и JSON-подробности.
class WorkLogEntry {
  const WorkLogEntry({
    required this.at,
    required this.level,
    required this.text,
    this.data = const {},
  });

  final DateTime at;
  final WorkLogLevel level;
  final String text;

  /// Структурные подробности для диагностики: адрес, исключение, тело ответа.
  final Map<String, Object?> data;
}
