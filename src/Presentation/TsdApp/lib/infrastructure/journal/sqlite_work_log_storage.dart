import 'dart:convert';

import 'package:sqflite/sqflite.dart';

import '../../domain/journal/work_log_entry.dart';
import '../../domain/journal/work_log_storage.dart';
import 'journal_database.dart';

/// Журнал работы в отдельной базе `journal.db`: таблица `work_log` с JSON-подробностями.
class SqliteWorkLogStorage implements WorkLogStorage {
  SqliteWorkLogStorage({Future<Database> Function()? open})
      : _open = open ?? JournalDatabase().open;

  final Future<Database> Function() _open;
  Future<Database>? _database;

  @override
  Future<List<WorkLogEntry>> recent({required int limit}) async {
    final database = await _db();
    final rows = await database.query('work_log', orderBy: 'at DESC', limit: limit);
    return [for (final row in rows) _entry(row)];
  }

  @override
  Future<void> add(WorkLogEntry entry) async {
    final database = await _db();
    await database.insert('work_log', {
      'at': entry.at.millisecondsSinceEpoch,
      'level': entry.level.name,
      'text': entry.text,
      'payload': jsonEncode(entry.data),
    });
  }

  @override
  Future<void> trim({required Duration age, required int limit}) async {
    final database = await _db();
    final border = DateTime.now().subtract(age).millisecondsSinceEpoch;
    await database.delete('work_log', where: 'at < ?', whereArgs: [border]);
    await database.rawDelete(
      'DELETE FROM work_log WHERE id NOT IN '
      '(SELECT id FROM work_log ORDER BY at DESC LIMIT ?)',
      [limit],
    );
  }

  @override
  Future<void> clear() async {
    final database = await _db();
    await database.delete('work_log');
  }

  Future<Database> _db() => _database ??= _open();

  static WorkLogEntry _entry(Map<String, Object?> row) {
    return WorkLogEntry(
      at: DateTime.fromMillisecondsSinceEpoch(row['at'] as int? ?? 0),
      level: _level('${row['level']}'),
      text: '${row['text'] ?? ''}',
      data: _payload('${row['payload'] ?? ''}'),
    );
  }

  static Map<String, Object?> _payload(String raw) {
    if (raw.trim().isEmpty) {
      return const {};
    }

    try {
      final decoded = jsonDecode(raw);
      if (decoded is Map) {
        return {for (final item in decoded.entries) '${item.key}': item.value};
      }
    } catch (_) {
      return const {};
    }
    return const {};
  }

  static WorkLogLevel _level(String name) {
    for (final level in WorkLogLevel.values) {
      if (level.name == name) {
        return level;
      }
    }
    return WorkLogLevel.info;
  }
}
