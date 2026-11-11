import 'dart:async';

import 'package:flutter/foundation.dart';

import '../../domain/journal/work_log_entry.dart';
import '../../domain/journal/work_log_storage.dart';
import '../../infrastructure/journal/sqlite_work_log_storage.dart';

/// Журнал работы приложения: свежие записи в памяти, хранение в отдельной базе.
class WorkLog extends ChangeNotifier {
  WorkLog({
    WorkLogStorage? storage,
    this.retention = const Duration(days: 7),
    this.limit = 5000,
    this.visible = 300,
  }) : _storage = storage ?? SqliteWorkLogStorage();

  /// Общий журнал приложения: диалоги и баннеры пишут в него.
  static final WorkLog instance = WorkLog();

  /// Как часто чистить базу при записи, если она растёт.
  static const _trimEvery = 25;
  static const _maxText = 2000;

  final WorkLogStorage _storage;

  /// Сколько времени хранится запись.
  final Duration retention;

  /// Сколько записей хранится в базе.
  final int limit;

  /// Сколько записей держится в памяти для экрана.
  final int visible;

  final _entries = <WorkLogEntry>[];
  Future<void>? _ready;
  var _writes = 0;

  List<WorkLogEntry> get entries => List.unmodifiable(_entries);

  /// Прочитать журнал из базы и почистить его. Повторный вызов ничего не делает.
  Future<void> load() => _ready ??= _read();

  void add({
    required WorkLogLevel level,
    required String text,
    Map<String, Object?> data = const {},
    DateTime? at,
  }) {
    final entry = WorkLogEntry(
      at: at ?? DateTime.now(),
      level: level,
      text: text,
      data: _short(data),
    );
    _entries.insert(0, entry);
    _trimMemory();
    notifyListeners();
    unawaited(_write(entry));
  }

  /// Очистить журнал: сначала дочитывает записи, иначе они вернутся из базы.
  Future<void> clear() async {
    await (_ready ??= _read());
    _entries.clear();
    notifyListeners();
    try {
      await _storage.clear();
    } catch (_) {
      return;
    }
  }

  Future<void> _read() async {
    try {
      await _storage.trim(age: retention, limit: limit);
      _merge(await _storage.recent(limit: visible));
      notifyListeners();
    } catch (_) {
      return;
    }
  }

  Future<void> _write(WorkLogEntry entry) async {
    try {
      await _storage.add(entry);
      _writes += 1;
      if (_writes % _trimEvery == 0) {
        await _storage.trim(age: retention, limit: limit);
      }
    } catch (_) {
      return;
    }
  }

  void _merge(List<WorkLogEntry> stored) {
    for (final entry in stored) {
      final known = _entries.any((item) => item.at == entry.at && item.text == entry.text);
      if (!known) {
        _entries.add(entry);
      }
    }
    _entries.sort((left, right) => right.at.compareTo(left.at));
    _trimMemory();
  }

  void _trimMemory() {
    if (_entries.length > visible) {
      _entries.removeRange(visible, _entries.length);
    }
  }

  /// Тело ответа 1С бывает большим: в журнале хватает начала.
  static Map<String, Object?> _short(Map<String, Object?> data) {
    return {
      for (final item in data.entries)
        item.key: switch (item.value) {
          final String text when text.length > _maxText => '${text.substring(0, _maxText)}…',
          final value => value,
        },
    };
  }
}
