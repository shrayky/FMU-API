import 'work_log_entry.dart';

/// Хранилище журнала работы между запусками приложения.
abstract interface class WorkLogStorage {
  /// Записи от новых к старым, не больше `limit`.
  Future<List<WorkLogEntry>> recent({required int limit});

  Future<void> add(WorkLogEntry entry);

  /// Убрать записи старше `age` и всё, что не влезло в последние `limit`.
  Future<void> trim({required Duration age, required int limit});

  Future<void> clear();
}
