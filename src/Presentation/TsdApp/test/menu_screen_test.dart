import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:tsd_app/application/shell/task_buttons.dart';
import 'package:tsd_app/application/shell/task_registry.dart';
import 'package:tsd_app/domain/task/task_button.dart';
import 'package:tsd_app/infrastructure/discovery/lan_fmu_scanner.dart';
import 'package:tsd_app/infrastructure/settings/app_settings.dart';
import 'package:tsd_app/infrastructure/settings/settings_store.dart';
import 'package:tsd_app/main.dart';
import 'package:tsd_app/presentation/home/home_screen.dart';
import 'package:tsd_app/presentation/scan/continuous_scan_table.dart';
import 'package:tsd_app/presentation/shell/journal_icon.dart';

void main() {
  testWidgets('на документах обе задачи, журнал у сбора, сервис скрыт', (tester) async {
    await _pumpMenu(tester, {'cameraScanEnabled': true, 'continuousScanEnabled': true});

    expect(find.text('Сбор марок'), findsOneWidget);
    expect(find.text('Проверка марки'), findsOneWidget);
    expect(find.byType(JournalIcon), findsOneWidget);
    expect(find.text('Сервис'), findsNothing);
    expect(find.text('Обмен'), findsNothing);
    expect(find.byType(ContinuousScanTable), findsNothing);
    expect(find.byIcon(Icons.qr_code_scanner), findsNothing);
  });

  testWidgets('пустой набор показывает «Все задачи выключены»', (tester) async {
    SharedPreferences.setMockInitialValues({});
    final buttons = [
      for (final button in TaskButtons.resolve(const TaskRegistry().tasks(), null))
        TaskButton(
          taskId: button.taskId,
          section: button.section,
          title: button.title,
          order: button.order,
          hotkey: button.hotkey,
          showJournal: button.showJournal,
          enabled: false,
        ),
    ];
    await SettingsStore().save(AppSettings(taskButtons: buttons));
    await _pumpMenu(tester, null);

    expect(find.text('Все задачи выключены'), findsOneWidget);
    expect(find.text('Сбор марок'), findsNothing);
    expect(find.text('Сервис'), findsNothing);
  });

  testWidgets('горячая клавиша открывает проверку марки', (tester) async {
    SharedPreferences.setMockInitialValues({});
    final buttons = [
      for (final button in TaskButtons.resolve(const TaskRegistry().tasks(), null))
        TaskButton(
          taskId: button.taskId,
          section: button.section,
          title: button.title,
          order: button.order,
          hotkey: button.taskId == 'markCheck' ? 'p' : '',
          showJournal: button.showJournal,
          enabled: button.enabled,
        ),
    ];
    await SettingsStore().save(AppSettings(taskButtons: buttons));
    await _pumpMenu(tester, null);

    await tester.sendKeyDownEvent(LogicalKeyboardKey.keyP, character: 'p');
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 400));

    expect(find.widgetWithText(AppBar, 'Проверка марки'), findsOneWidget);
  });
}

Future<void> _pumpMenu(WidgetTester tester, Map<String, Object>? preferences) async {
  if (preferences != null) {
    SharedPreferences.setMockInitialValues(preferences);
  }
  await tester.pumpWidget(
    TsdApp(
      home: HomeScreen(
        appVersion: '1.1.0',
        lanScanner: LanFmuScanner(listAddresses: () async => []),
      ),
    ),
  );
  await tester.pump();
  await tester.idle();
  await tester.pump();
}
