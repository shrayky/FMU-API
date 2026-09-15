import 'package:flutter/material.dart';

import '../../infrastructure/discovery/lan_fmu_scanner.dart';
import '../../infrastructure/settings/app_settings.dart';
import '../../theme/webix_dark_theme.dart';
import 'broadcast_monitor_screen.dart';

/// Адрес Fmu-Api и параметры broadcast-интента сканера.
class SettingsScreen extends StatefulWidget {
  const SettingsScreen({
    super.key,
    required this.settings,
    this.lanScanner,
  });

  final AppSettings settings;
  final LanFmuScanner? lanScanner;

  @override
  State<SettingsScreen> createState() => _SettingsScreenState();
}

class _SettingsScreenState extends State<SettingsScreen> {
  late final TextEditingController _api;
  late final TextEditingController _action;
  late final TextEditingController _extra;
  late final LanFmuScanner _lan;
  late bool _camera;
  bool _searching = false;

  @override
  void initState() {
    super.initState();
    _api = TextEditingController(text: widget.settings.apiBaseUrl);
    _action = TextEditingController(text: widget.settings.intentAction);
    _extra = TextEditingController(text: widget.settings.intentExtra);
    _camera = widget.settings.cameraScanEnabled;
    _lan = widget.lanScanner ?? LanFmuScanner();
  }

  @override
  void dispose() {
    _lan.cancel();
    _api.dispose();
    _action.dispose();
    _extra.dispose();
    super.dispose();
  }

  void _save() {
    Navigator.of(context).pop(
      AppSettings(
        apiBaseUrl: _api.text.trim(),
        intentAction: _action.text.trim(),
        intentExtra: _extra.text.trim().isEmpty ? 'barcode' : _extra.text.trim(),
        cameraScanEnabled: _camera,
      ),
    );
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
        ],
      ),
    );
  }
}
