import 'package:shared_preferences/shared_preferences.dart';

import 'app_settings.dart';

/// Хранение адреса API, интента сканера и доступности камеры.
class SettingsStore {
  static const _apiKey = 'apiBaseUrl';
  static const _actionKey = 'intentAction';
  static const _extraKey = 'intentExtra';
  static const _cameraKey = 'cameraScanEnabled';

  Future<AppSettings> load() async {
    final prefs = await SharedPreferences.getInstance();
    return AppSettings(
      apiBaseUrl: prefs.getString(_apiKey) ?? '',
      intentAction: prefs.getString(_actionKey) ?? '',
      intentExtra: prefs.getString(_extraKey) ?? 'barcode',
      cameraScanEnabled: prefs.getBool(_cameraKey) ?? false,
    );
  }

  Future<void> save(AppSettings settings) async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString(_apiKey, settings.apiBaseUrl.trim());
    await prefs.setString(_actionKey, settings.intentAction.trim());
    await prefs.setString(_extraKey, settings.intentExtra.trim().isEmpty ? 'barcode' : settings.intentExtra.trim());
    await prefs.setBool(_cameraKey, settings.cameraScanEnabled);
  }
}
