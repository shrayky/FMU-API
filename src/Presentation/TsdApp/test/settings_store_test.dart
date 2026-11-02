import 'package:flutter_test/flutter_test.dart';
import 'package:shared_preferences/shared_preferences.dart';
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
}
