/// Настройки клиента ТСД.
class AppSettings {
  const AppSettings({
    this.apiBaseUrl = '',
    this.intentAction = '',
    this.intentExtra = 'barcode',
    this.cameraScanEnabled = false,
  });

  final String apiBaseUrl;
  final String intentAction;
  final String intentExtra;
  final bool cameraScanEnabled;

  AppSettings copyWith({
    String? apiBaseUrl,
    String? intentAction,
    String? intentExtra,
    bool? cameraScanEnabled,
  }) {
    return AppSettings(
      apiBaseUrl: apiBaseUrl ?? this.apiBaseUrl,
      intentAction: intentAction ?? this.intentAction,
      intentExtra: intentExtra ?? this.intentExtra,
      cameraScanEnabled: cameraScanEnabled ?? this.cameraScanEnabled,
    );
  }
}
