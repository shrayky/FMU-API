import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:tsd_app/application/journal/work_log.dart';
import 'package:tsd_app/domain/journal/work_log_entry.dart';
import 'package:tsd_app/domain/journal/work_log_storage.dart';
import 'package:tsd_app/presentation/settings/work_log_screen.dart';
import 'package:tsd_app/theme/webix_dark_theme.dart';

void main() {
  testWidgets('записи журнала видны и очищаются', (tester) async {
    final log = WorkLog(storage: MemoryWorkLogStorage())
      ..add(
        level: WorkLogLevel.error,
        text: 'Проверка связи завершилась с ошибкой: Сервер не ответил за 10 сек',
        data: {
          'endpoint': '10.71.105.246:80/ideald',
          'raw': 'TimeoutException after 0:00:10.000000: Future not completed',
        },
        at: DateTime(2026, 11, 4, 12, 30, 5),
      );

    await tester.pumpWidget(
      MaterialApp(theme: WebixDarkTheme.data(), home: WorkLogScreen(log: log)),
    );
    await tester.pumpAndSettle();

    expect(find.text('Проверка связи завершилась с ошибкой: Сервер не ответил за 10 сек'), findsOneWidget);
    expect(find.textContaining('endpoint: 10.71.105.246:80/ideald'), findsOneWidget);
    expect(find.textContaining('TimeoutException'), findsOneWidget);
    expect(find.text('04.11 12:30:05'), findsOneWidget);

    await tester.tap(find.text('Очистить'));
    await tester.pumpAndSettle();

    expect(find.text('Нет данных для отображения'), findsOneWidget);
  });

  testWidgets('пустой журнал показывает заглушку без очистки', (tester) async {
    await tester.pumpWidget(
      MaterialApp(
        theme: WebixDarkTheme.data(),
        home: WorkLogScreen(log: WorkLog(storage: MemoryWorkLogStorage())),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text('Нет данных для отображения'), findsOneWidget);
    expect(find.text('Очистить'), findsNothing);
    expect(find.text('Закрыть'), findsOneWidget);
  });
}

class MemoryWorkLogStorage implements WorkLogStorage {
  final entries = <WorkLogEntry>[];

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
  Future<void> trim({required Duration age, required int limit}) async {}

  @override
  Future<void> clear() async {
    entries.clear();
  }
}
