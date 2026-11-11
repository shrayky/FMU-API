import 'package:flutter_test/flutter_test.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:tsd_app/application/shell/task_buttons.dart';
import 'package:tsd_app/application/shell/task_registry.dart';
import 'package:tsd_app/domain/task/task_button.dart';
import 'package:tsd_app/domain/task/task_section.dart';
import 'package:tsd_app/infrastructure/settings/app_settings.dart';
import 'package:tsd_app/infrastructure/settings/settings_store.dart';

void main() {
  test('по умолчанию сканирование камерой выключено', () async {
    SharedPreferences.setMockInitialValues({});

    final settings = await SettingsStore().load();

    expect(settings.cameraScanEnabled, isFalse);
  });

  test('сохраняет включение сканирования камерой', () async {
    SharedPreferences.setMockInitialValues({});
    final store = SettingsStore();

    await store.save(const AppSettings(cameraScanEnabled: true));
    final loaded = await store.load();

    expect(loaded.cameraScanEnabled, isTrue);
  });

  test('по умолчанию непрерывное сканирование выключено', () async {
    SharedPreferences.setMockInitialValues({});

    final settings = await SettingsStore().load();

    expect(settings.continuousScanEnabled, isFalse);
  });

  test('по умолчанию обновление проверяется раз в час', () async {
    SharedPreferences.setMockInitialValues({});

    final settings = await SettingsStore().load();

    expect(settings.updateCheckIntervalMinutes, 60);
  });

  test('сохраняет интервал проверки обновления', () async {
    SharedPreferences.setMockInitialValues({});
    final store = SettingsStore();

    await store.save(const AppSettings(updateCheckIntervalMinutes: 15));
    final loaded = await store.load();

    expect(loaded.updateCheckIntervalMinutes, 15);
  });

  test('сохраняет непрерывное сканирование', () async {
    SharedPreferences.setMockInitialValues({});
    final store = SettingsStore();

    await store.save(const AppSettings(continuousScanEnabled: true));
    final loaded = await store.load();

    expect(loaded.continuousScanEnabled, isTrue);
  });

  test('по умолчанию документы хранятся 30 дней', () async {
    SharedPreferences.setMockInitialValues({});

    final settings = await SettingsStore().load();

    expect(settings.documentRetentionDays, 30);
  });

  test('сохраняет срок хранения документов', () async {
    SharedPreferences.setMockInitialValues({});
    final store = SettingsStore();

    await store.save(const AppSettings(documentRetentionDays: 0));
    expect((await store.load()).documentRetentionDays, 0);

    await store.save(const AppSettings(documentRetentionDays: 14));
    expect((await store.load()).documentRetentionDays, 14);
  });

  test('битый и пустой список кнопок даёт набор по умолчанию', () async {
    SharedPreferences.setMockInitialValues({'taskButtons': '{'});
    final broken = await SettingsStore().load();
    final fromBroken = TaskButtons.resolve(const TaskRegistry().tasks(), broken.taskButtons);

    expect(fromBroken.map((button) => button.taskId), ['markCollection', 'markCheck']);

    SharedPreferences.setMockInitialValues({'taskButtons': ''});
    final empty = await SettingsStore().load();
    final fromEmpty = TaskButtons.resolve(const TaskRegistry().tasks(), empty.taskButtons);

    expect(fromEmpty.map((button) => button.taskId), ['markCollection', 'markCheck']);
  });

  test('сохраняет кнопку задачи', () async {
    SharedPreferences.setMockInitialValues({});
    final store = SettingsStore();
    const button = TaskButton(
      taskId: 'markCheck',
      section: TaskSection.service,
      title: 'Своя',
      order: 4,
      hotkey: 'p',
      showJournal: false,
      enabled: true,
    );

    await store.save(const AppSettings(taskButtons: [button]));
    final loaded = await store.load();

    expect(loaded.taskButtons, isNotNull);
    expect(loaded.taskButtons!.single.taskId, 'markCheck');
    expect(loaded.taskButtons!.single.section, TaskSection.service);
    expect(loaded.taskButtons!.single.title, 'Своя');
    expect(loaded.taskButtons!.single.order, 4);
    expect(loaded.taskButtons!.single.hotkey, 'p');
  });
}
