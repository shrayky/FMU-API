/// Настройки клиента ТСД.
class AppSettings {
  const AppSettings({
    this.apiBaseUrl = '',
    this.intentAction = '',
    this.intentExtra = 'barcode',
    this.cameraScanEnabled = false,
    this.continuousScanEnabled = false,
    this.updateCheckIntervalMinutes = 60,
  });

  final String apiBaseUrl;
  final String intentAction;
  final String intentExtra;
  final bool cameraScanEnabled;
  final bool continuousScanEnabled;
  final int updateCheckIntervalMinutes;

  AppSettings copyWith({
    String? apiBaseUrl,
    String? intentAction,
    String? intentExtra,
    bool? cameraScanEnabled,
    bool? continuousScanEnabled,
    int? updateCheckIntervalMinutes,
  }) {
    return AppSettings(
      apiBaseUrl: apiBaseUrl ?? this.apiBaseUrl,
      intentAction: intentAction ?? this.intentAction,
      intentExtra: intentExtra ?? this.intentExtra,
      cameraScanEnabled: cameraScanEnabled ?? this.cameraScanEnabled,
      continuousScanEnabled: continuousScanEnabled ?? this.continuousScanEnabled,
      updateCheckIntervalMinutes: updateCheckIntervalMinutes ?? this.updateCheckIntervalMinutes,
    );
  }
}
