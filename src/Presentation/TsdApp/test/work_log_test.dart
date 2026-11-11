import 'package:flutter_test/flutter_test.dart';
import 'package:tsd_app/application/journal/work_log.dart';
import 'package:tsd_app/domain/journal/work_log_entry.dart';
import 'package:tsd_app/domain/journal/work_log_storage.dart';

void main() {
  test('новая запись идёт первой и попадает в базу', () async {
    final storage = MemoryWorkLogStorage();
    final log = WorkLog(storage: storage);

    log.add(level: WorkLogLevel.info, text: 'первая');
    log.add(level: WorkLogLevel.error, text: 'вторая', data: {'raw': 'TimeoutException'});

    expect(log.entries.map((entry) => entry.text), ['вторая', 'первая']);
    expect(log.entries.first.level, WorkLogLevel.error);
    expect(log.entries.first.data['raw'], 'TimeoutException');

    await Future<void>.delayed(Duration.zero);
    expect(storage.entries.length, 2);
  });

  test('в памяти только последние видимые записи', () async {
    final log = WorkLog(storage: MemoryWorkLogStorage(), visible: 2);

    for (var index = 0; index < 4; index += 1) {
      log.add(level: WorkLogLevel.info, text: 'запись $index');
    }

    expect(log.entries.map((entry) => entry.text), ['запись 3', 'запись 2']);
  });

  test('загрузка не теряет записи, добавленные до неё, и чистит базу', () async {
    final now = DateTime.now();
    final storage = MemoryWorkLogStorage()
      ..entries.add(
        WorkLogEntry(at: now.subtract(const Duration(days: 1)), level: WorkLogLevel.info, text: 'из базы'),
      );
    final log = WorkLog(storage: storage);

    log.add(level: WorkLogLevel.info, text: 'живая', at: now);
    await log.load();

    expect(log.entries.map((entry) => entry.text), ['живая', 'из базы']);
    expect(storage.trims, greaterThanOrEqualTo(1));
  });

  test('длинная подробность обрезается до 2000 символов', () async {
    final log = WorkLog(storage: MemoryWorkLogStorage())
      ..add(level: WorkLogLevel.error, text: 'ответ', data: {'raw': 'x' * 3000});

    final raw = '${log.entries.single.data['raw']}';
    expect(raw.length, 2001);
    expect(raw, endsWith('…'));
  });

  test('очистка убирает записи и базу', () async {
    final storage = MemoryWorkLogStorage();
    final log = WorkLog(storage: storage)..add(level: WorkLogLevel.warning, text: 'сообщение');

    await log.clear();

    expect(log.entries, isEmpty);
    expect(storage.entries, isEmpty);
    expect(storage.clears, 1);
  });
}

class MemoryWorkLogStorage implements WorkLogStorage {
  final entries = <WorkLogEntry>[];
  var trims = 0;
  var clears = 0;

  @override
  Future<List<WorkLogEntry>> recent({required int limit}) async {
    final sorted = [...entries]..sort((left, right) => right.at.compareTo(left.at));
    return sorted.take(limit).toList();
  }

  @override
  Future<void> add(WorkLogEntry entry) async {
    entries.add(entry);
  }

  @override
  Future<void> trim({required Duration age, required int limit}) async {
    trims += 1;
    final border = DateTime.now().subtract(age);
    entries.removeWhere((entry) => entry.at.isBefore(border));
    final sorted = [...entries]..sort((left, right) => right.at.compareTo(left.at));
    for (final entry in sorted.skip(limit)) {
      entries.remove(entry);
    }
  }

  @override
  Future<void> clear() async {
    clears += 1;
    entries.clear();
  }
}
