import 'dart:convert';

import 'package:shared_preferences/shared_preferences.dart';

import '../../domain/exchange/exchange_server.dart';
import '../../domain/exchange/exchange_variant.dart';
import '../../domain/task/task_button.dart';
import 'app_settings.dart';

/// Хранение настроек ТСД, включая кнопки задач и серверы обмена.
class SettingsStore {
  static const _apiKey = 'apiBaseUrl';
  static const _actionKey = 'intentAction';
  static const _extraKey = 'intentExtra';
  static const _cameraKey = 'cameraScanEnabled';
  static const _continuousKey = 'continuousScanEnabled';
  static const _updateIntervalKey = 'updateCheckIntervalMinutes';
  static const _pinKey = 'settingsPin';
  static const _registerPackageKey = 'registerPackageBarcode';
  static const _checkOwnerKey = 'checkMarkOwner';
  static const _requestStatusKey = 'requestSelectionStatus';
  static const _requestPackageKey = 'requestPackageViaTrueApi';
  static const _markApiKey = 'markCheckApi';
  static const _serverIdKey = 'exchangeServerId';
  static const _retentionKey = 'documentRetentionDays';
  static const _serversKey = 'exchangeServers';
  static const _variantsKey = 'exchangeVariants';
  static const _taskButtonsKey = 'taskButtons';

  Future<AppSettings> load() async {
    final prefs = await SharedPreferences.getInstance();
    return AppSettings(
      apiBaseUrl: prefs.getString(_apiKey) ?? '',
      intentAction: prefs.getString(_actionKey) ?? '',
      intentExtra: prefs.getString(_extraKey) ?? 'barcode',
      cameraScanEnabled: prefs.getBool(_cameraKey) ?? false,
      continuousScanEnabled: prefs.getBool(_continuousKey) ?? false,
      updateCheckIntervalMinutes: _minutes(prefs.getInt(_updateIntervalKey)),
      settingsPin: prefs.getString(_pinKey) ?? '',
      registerPackageBarcode: prefs.getBool(_registerPackageKey) ?? false,
      checkMarkOwner: prefs.getBool(_checkOwnerKey) ?? false,
      requestSelectionStatus: prefs.getBool(_requestStatusKey) ?? false,
      requestPackageViaTrueApi: prefs.getBool(_requestPackageKey) ?? false,
      markCheckApi: _markApi(prefs.getString(_markApiKey)),
      exchangeServerId: prefs.getString(_serverIdKey) ?? '',
      documentRetentionDays: _days(prefs.getInt(_retentionKey)),
      exchangeServers: _readServers(prefs.getString(_serversKey)),
      exchangeVariants: _readVariants(prefs.getString(_variantsKey)),
      taskButtons: _readButtons(prefs.getString(_taskButtonsKey)),
    );
  }

  Future<void> save(AppSettings settings) async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString(_apiKey, settings.apiBaseUrl.trim());
    await prefs.setString(_actionKey, settings.intentAction.trim());
    await prefs.setString(
      _extraKey,
      settings.intentExtra.trim().isEmpty ? 'barcode' : settings.intentExtra.trim(),
    );
    await prefs.setBool(_cameraKey, settings.cameraScanEnabled);
    await prefs.setBool(_continuousKey, settings.continuousScanEnabled);
    await prefs.setInt(_updateIntervalKey, _minutes(settings.updateCheckIntervalMinutes));
    await prefs.setString(_pinKey, settings.settingsPin);
    await prefs.setBool(_registerPackageKey, settings.registerPackageBarcode);
    await prefs.setBool(_checkOwnerKey, settings.checkMarkOwner);
    await prefs.setBool(_requestStatusKey, settings.requestSelectionStatus);
    await prefs.setBool(_requestPackageKey, settings.requestPackageViaTrueApi);
    await prefs.setString(_markApiKey, _markApi(settings.markCheckApi));
    await prefs.setString(_serverIdKey, settings.exchangeServerId);
    await prefs.setInt(_retentionKey, _days(settings.documentRetentionDays));
    await prefs.setString(_serversKey, jsonEncode([for (final server in settings.exchangeServers) server.toJson()]));
    await prefs.setString(_variantsKey, jsonEncode([for (final variant in settings.exchangeVariants) variant.toJson()]));
    final buttons = settings.taskButtons;
    if (buttons != null) {
      await prefs.setString(_taskButtonsKey, jsonEncode([for (final button in buttons) button.toJson()]));
    }
  }

  static int _minutes(int? value) {
    if (value == null || value < 1) {
      return 60;
    }
    return value;
  }

  static int _days(int? value) {
    if (value == null || value < 0) {
      return 30;
    }
    return value;
  }

  static String _markApi(String? value) {
    final text = value?.trim() ?? '';
    if (text.isEmpty) {
      return 'True api';
    }
    return text;
  }

  static List<TaskButton>? _readButtons(String? raw) {
    if (raw == null || raw.trim().isEmpty) {
      return null;
    }

    try {
      final decoded = jsonDecode(raw);
      if (decoded is! List) {
        return null;
      }

      final buttons = <TaskButton>[];
      for (final item in decoded) {
        final button = TaskButton.fromJson(item);
        if (button != null) {
          buttons.add(button);
        }
      }
      return buttons;
    } catch (_) {
      return null;
    }
  }

  static List<ExchangeServer> _readServers(String? raw) {
    return _readList(raw, ExchangeServer.fromJson);
  }

  static List<ExchangeVariant> _readVariants(String? raw) {
    return _readList(raw, ExchangeVariant.fromJson);
  }

  static List<T> _readList<T>(String? raw, T? Function(Object? json) parse) {
    if (raw == null || raw.trim().isEmpty) {
      return const [];
    }

    try {
      final decoded = jsonDecode(raw);
      if (decoded is! List) {
        return const [];
      }

      final items = <T>[];
      for (final item in decoded) {
        final parsed = parse(item);
        if (parsed != null) {
          items.add(parsed);
        }
      }
      return items;
    } catch (_) {
      return const [];
    }
  }
}
