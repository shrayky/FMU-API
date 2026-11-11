import '../../domain/exchange/exchange_server.dart';
import '../../domain/exchange/exchange_variant.dart';
import '../../domain/task/task_button.dart';

/// Настройки клиента ТСД.
class AppSettings {
  const AppSettings({
    this.apiBaseUrl = '',
    this.intentAction = '',
    this.intentExtra = 'barcode',
    this.cameraScanEnabled = false,
    this.continuousScanEnabled = false,
    this.updateCheckIntervalMinutes = 60,
    this.settingsPin = '',
    this.registerPackageBarcode = false,
    this.checkMarkOwner = false,
    this.requestSelectionStatus = false,
    this.requestPackageViaTrueApi = false,
    this.markCheckApi = 'True api',
    this.exchangeServerId = '',
    this.documentRetentionDays = 30,
    this.exchangeServers = const [],
    this.exchangeVariants = const [],
    this.taskButtons,
  });

  final String apiBaseUrl;
  final String intentAction;
  final String intentExtra;
  final bool cameraScanEnabled;
  final bool continuousScanEnabled;
  final int updateCheckIntervalMinutes;
  final String settingsPin;
  final bool registerPackageBarcode;
  final bool checkMarkOwner;
  final bool requestSelectionStatus;
  final bool requestPackageViaTrueApi;
  final String markCheckApi;
  final String exchangeServerId;

  /// Сколько дней хранить документы сбора марок. Ноль документы не удаляет.
  final int documentRetentionDays;

  final List<ExchangeServer> exchangeServers;
  final List<ExchangeVariant> exchangeVariants;

  /// Сервер обмена, выбранный в основных настройках.
  ExchangeServer? get exchangeServer {
    for (final server in exchangeServers) {
      if (server.id == exchangeServerId) {
        return server;
      }
    }
    return null;
  }

  /// `null` — кнопки ещё не сохранялись, меню берёт набор по умолчанию.
  final List<TaskButton>? taskButtons;

  AppSettings copyWith({
    String? apiBaseUrl,
    String? intentAction,
    String? intentExtra,
    bool? cameraScanEnabled,
    bool? continuousScanEnabled,
    int? updateCheckIntervalMinutes,
    String? settingsPin,
    bool? registerPackageBarcode,
    bool? checkMarkOwner,
    bool? requestSelectionStatus,
    bool? requestPackageViaTrueApi,
    String? markCheckApi,
    String? exchangeServerId,
    int? documentRetentionDays,
    List<ExchangeServer>? exchangeServers,
    List<ExchangeVariant>? exchangeVariants,
    List<TaskButton>? taskButtons,
  }) {
    return AppSettings(
      apiBaseUrl: apiBaseUrl ?? this.apiBaseUrl,
      intentAction: intentAction ?? this.intentAction,
      intentExtra: intentExtra ?? this.intentExtra,
      cameraScanEnabled: cameraScanEnabled ?? this.cameraScanEnabled,
      continuousScanEnabled: continuousScanEnabled ?? this.continuousScanEnabled,
      updateCheckIntervalMinutes: updateCheckIntervalMinutes ?? this.updateCheckIntervalMinutes,
      settingsPin: settingsPin ?? this.settingsPin,
      registerPackageBarcode: registerPackageBarcode ?? this.registerPackageBarcode,
      checkMarkOwner: checkMarkOwner ?? this.checkMarkOwner,
      requestSelectionStatus: requestSelectionStatus ?? this.requestSelectionStatus,
      requestPackageViaTrueApi: requestPackageViaTrueApi ?? this.requestPackageViaTrueApi,
      markCheckApi: markCheckApi ?? this.markCheckApi,
      exchangeServerId: exchangeServerId ?? this.exchangeServerId,
      documentRetentionDays: documentRetentionDays ?? this.documentRetentionDays,
      exchangeServers: exchangeServers ?? this.exchangeServers,
      exchangeVariants: exchangeVariants ?? this.exchangeVariants,
      taskButtons: taskButtons ?? this.taskButtons,
    );
  }
}
