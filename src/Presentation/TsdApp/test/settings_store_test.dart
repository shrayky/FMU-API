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
}
