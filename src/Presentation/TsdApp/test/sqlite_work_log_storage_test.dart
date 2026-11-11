import 'package:flutter_test/flutter_test.dart';
import 'package:sqflite_common_ffi/sqflite_ffi.dart';
import 'package:tsd_app/domain/journal/work_log_entry.dart';
import 'package:tsd_app/infrastructure/journal/journal_database.dart';
import 'package:tsd_app/infrastructure/journal/sqlite_work_log_storage.dart';

void main() {
  sqfliteFfiInit();

  late Database database;
  late SqliteWorkLogStorage storage;

  setUp(() async {
    database = await JournalDatabase(
      factory: databaseFactoryFfi,
      path: inMemoryDatabasePath,
    ).open();
    storage = SqliteWorkLogStorage(open: () async => database);
  });

  tearDown(() => database.close());

  test('запись читается обратно вместе с JSON-подробностями', () async {
    await storage.add(
      WorkLogEntry(
        at: DateTime(2026, 11, 4, 12, 30, 5),
        level: WorkLogLevel.error,
        text: 'ошибка',
        data: const {'endpoint': '10.0.0.5:80', 'code': 503},
      ),
    );

    final entry = (await storage.recent(limit: 10)).single;

    expect(entry.text, 'ошибка');
    expect(entry.level, WorkLogLevel.error);
    expect(entry.at, DateTime(2026, 11, 4, 12, 30, 5));
    expect(entry.data['endpoint'], '10.0.0.5:80');
    expect(entry.data['code'], 503);
  });

  test('свежие записи идут первыми и уважают лимит', () async {
    for (var index = 0; index < 3; index += 1) {
      await storage.add(
        WorkLogEntry(
          at: DateTime(2026, 11, 4, 10 + index),
          level: WorkLogLevel.info,
          text: 'запись $index',
        ),
      );
    }

    final entries = await storage.recent(limit: 2);

    expect(entries.map((entry) => entry.text), ['запись 2', 'запись 1']);
  });

  test('trim убирает старые записи и лишние строки', () async {
    final now = DateTime.now();
    await storage.add(
      WorkLogEntry(at: now.subtract(const Duration(days: 8)), level: WorkLogLevel.info, text: 'старая'),
    );
    await storage.add(
      WorkLogEntry(at: now.subtract(const Duration(days: 1)), level: WorkLogLevel.info, text: 'свежая'),
    );

    await storage.trim(age: const Duration(days: 7), limit: 1);

    final entries = await storage.recent(limit: 10);
    expect(entries.map((entry) => entry.text), ['свежая']);
  });

  test('clear опустошает журнал', () async {
    await storage.add(
      WorkLogEntry(at: DateTime(2026, 11, 4), level: WorkLogLevel.warning, text: 'сообщение'),
    );

    await storage.clear();

    expect(await storage.recent(limit: 10), isEmpty);
  });
}
