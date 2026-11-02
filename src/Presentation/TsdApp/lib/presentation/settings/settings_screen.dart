import 'package:flutter/material.dart';
import 'package:package_info_plus/package_info_plus.dart';

import '../../infrastructure/discovery/lan_fmu_scanner.dart';
import '../../infrastructure/settings/app_settings.dart';
import '../../infrastructure/update/app_update.dart';
import '../../infrastructure/update/app_update_flow.dart';
import '../../theme/webix_dark_theme.dart';
import '../update/update_offer.dart';
import 'broadcast_monitor_screen.dart';

/// Адрес Fmu-Api, параметры сканера и проверка обновления.
class SettingsScreen extends StatefulWidget {
  const SettingsScreen({
    super.key,
    required this.settings,
    this.lanScanner,
    this.appVersion,
    this.updates,
  });

  final AppSettings settings;
  final LanFmuScanner? lanScanner;
  final String? appVersion;
  final AppUpdateFlow? updates;

  @override
  State<SettingsScreen> createState() => _SettingsScreenState();
}

class _SettingsScreenState extends State<SettingsScreen> {
  late final TextEditingController _api;
  late final TextEditingController _action;
  late final TextEditingController _extra;
  late final TextEditingController _interval;
  late final LanFmuScanner _lan;
  late final AppUpdateFlow _updates;
  late bool _camera;
  late bool _continuous;
  bool _searching = false;
  bool _checkingUpdate = false;
  bool _updating = false;
  AppUpdate? _offer;
  String? _version;

  @override
  void initState() {
    super.initState();
    _api = TextEditingController(text: widget.settings.apiBaseUrl);
    _action = TextEditingController(text: widget.settings.intentAction);
    _extra = TextEditingController(text: widget.settings.intentExtra);
    _interval = TextEditingController(text: '${widget.settings.updateCheckIntervalMinutes}');
    _camera = widget.settings.cameraScanEnabled;
    _continuous = widget.settings.continuousScanEnabled;
    _lan = widget.lanScanner ?? LanFmuScanner();
    _updates = widget.updates ?? AppUpdateFlow();
    _version = widget.appVersion;
    if (_version == null) {
      _loadVersion();
    }
  }

  Future<void> _loadVersion() async {
    try {
      final info = await PackageInfo.fromPlatform();
      if (!mounted) {
        return;
      }
      setState(() => _version = '${info.version} (${info.buildNumber})');
    } catch (_) {}
  }

  @override
  void dispose() {
    _lan.cancel();
    _api.dispose();
    _action.dispose();
    _extra.dispose();
    _interval.dispose();
    super.dispose();
  }

  void _save() {
    Navigator.of(context).pop(
      AppSettings(
        apiBaseUrl: _api.text.trim(),
        intentAction: _action.text.trim(),
        intentExtra: _extra.text.trim().isEmpty ? 'barcode' : _extra.text.trim(),
        cameraScanEnabled: _camera,
        continuousScanEnabled: _continuous,
        updateCheckIntervalMinutes: _intervalMinutes(),
      ),
    );
  }

  int _intervalMinutes() {
    final parsed = int.tryParse(_interval.text.trim());
    if (parsed == null || parsed < 1) {
      return 60;
    }
    return parsed;
  }

  Future<void> _checkUpdate() async {
    if (_checkingUpdate || _updating) {
      return;
    }

    setState(() => _checkingUpdate = true);
    try {
      final offer = await _updates.check(_api.text);
      if (!mounted) {
        return;
      }
      setState(() => _offer = offer);
      if (offer == null) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Обновление не найдено')),
        );
      }
    } catch (error) {
      if (!mounted) {
        return;
      }
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('$error')));
    } finally {
      if (mounted) {
        setState(() => _checkingUpdate = false);
      }
    }
  }

  Future<void> _installUpdate() async {
    final offer = _offer;
    if (offer == null || _updating) {
      return;
    }

    setState(() => _updating = true);
    try {
      await _updates.install(_api.text, offer);
    } catch (error) {
      if (!mounted) {
        return;
      }
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('$error')));
    } finally {
      if (mounted) {
        setState(() => _updating = false);
      }
    }
  }

  Future<void> _findApi() async {
    setState(() => _searching = true);
    try {
      final found = await _lan.findFirst();
      if (!mounted) {
        return;
      }

      if (found == null) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Fmu-Api в сети не найден')),
        );
        return;
      }

      _api.text = found;
    } finally {
      if (mounted) {
        setState(() => _searching = false);
      }
    }
  }

  Future<void> _openMonitor() async {
    final pick = await Navigator.of(context).push<BroadcastPick>(
      MaterialPageRoute(
        builder: (_) => BroadcastMonitorScreen(intentAction: _action.text),
      ),
    );
    if (pick == null || !mounted) {
      return;
    }

    _action.text = pick.action;
    _extra.text = pick.extra;
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Настройки'),
        actions: [
          TextButton(
            onPressed: _save,
            child: const Text('Сохранить', style: TextStyle(color: WebixDarkTheme.accent)),
          ),
        ],
      ),
      body: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          TextField(
            controller: _api,
            keyboardType: TextInputType.url,
            decoration: const InputDecoration(
              labelText: 'Адрес Fmu-Api',
              hintText: 'http://192.168.1.10:2578',
            ),
          ),
          const SizedBox(height: 12),
          Align(
            alignment: Alignment.centerLeft,
            child: TextButton(
              onPressed: _searching ? null : _findApi,
              child: Text(_searching ? 'Поиск…' : 'Найти в сети'),
            ),
          ),
          const SizedBox(height: 8),
          SwitchListTile(
            contentPadding: EdgeInsets.zero,
            title: const Text('Сканирование камерой'),
            subtitle: const Text('Штрихкоды маркировки, камерой устройства'),
            value: _camera,
            onChanged: (value) => setState(() => _camera = value),
          ),
          SwitchListTile(
            contentPadding: EdgeInsets.zero,
            title: const Text('Непрерывное сканирование'),
            subtitle: const Text('Марки копятся в таблице, проверка идёт в фоне'),
            value: _continuous,
            onChanged: (value) => setState(() => _continuous = value),
          ),
          const SizedBox(height: 8),
          TextField(
            controller: _action,
            decoration: const InputDecoration(
              labelText: 'Intent action',
              hintText: 'ru.fmuapi.tsd.SCAN',
            ),
          ),
          const SizedBox(height: 20),
          TextField(
            controller: _extra,
            decoration: const InputDecoration(
              labelText: 'Intent extra',
              hintText: 'barcode',
            ),
          ),
          const SizedBox(height: 12),
          Align(
            alignment: Alignment.centerLeft,
            child: TextButton(
              onPressed: _openMonitor,
              child: const Text('Сканер: broadcast'),
            ),
          ),
          if (_version != null) ...[
            const SizedBox(height: 24),
            Text(
              'Версия $_version',
              style: const TextStyle(color: WebixDarkTheme.muted, fontSize: 13),
            ),
          ],
          const SizedBox(height: 12),
          TextField(
            controller: _interval,
            keyboardType: TextInputType.number,
            decoration: const InputDecoration(
              labelText: 'Проверка обновления, мин',
            ),
          ),
          Align(
            alignment: Alignment.centerLeft,
            child: TextButton(
              onPressed: _checkingUpdate ? null : _checkUpdate,
              child: Text(_checkingUpdate ? 'Проверка…' : 'Проверить обновление'),
            ),
          ),
          if (_offer != null)
            UpdateOffer(
              versionName: _offer!.versionName,
              busy: _updating,
              onUpdate: _installUpdate,
            ),
        ],
      ),
    );
  }
}
