import 'dart:async';

import 'package:flutter/material.dart';

import '../../application/message/message_center.dart';
import '../../application/shell/task_buttons.dart';
import '../../application/shell/task_registry.dart';
import '../../domain/exchange/exchange_server.dart';
import '../../domain/exchange/exchange_variant.dart';
import '../../domain/message/app_message.dart';
import '../../domain/message/failure_text.dart';
import '../../domain/task/task_button.dart';
import '../../domain/task/task_section.dart';
import '../../infrastructure/discovery/lan_fmu_scanner.dart';
import '../../infrastructure/documents/document_database_export.dart';
import '../../infrastructure/settings/app_settings.dart';
import '../../infrastructure/settings/settings_store.dart';
import '../../theme/webix_dark_theme.dart';
import '../shell/shell_bottom_bar.dart';
import 'exchange_server_list_screen.dart';
import 'exchange_variant_form_screen.dart';
import 'task_form_screen.dart';
import 'work_log_screen.dart';

/// Настройка Fmu-Api: основные, оборудование, задачи и обмен.
class SettingsScreen extends StatefulWidget {
  const SettingsScreen({
    super.key,
    required this.settings,
    this.lanScanner,
    this.databaseExport,
  });

  final AppSettings settings;
  final LanFmuScanner? lanScanner;
  final DocumentDatabaseExport? databaseExport;

  @override
  State<SettingsScreen> createState() => _SettingsScreenState();
}

class _SettingsScreenState extends State<SettingsScreen> {
  final _store = SettingsStore();
  final _messages = MessageCenter();
  final _tasks = const TaskRegistry().tasks();
  late final TextEditingController _api;
  late final TextEditingController _pin;
  late final TextEditingController _retention;
  late AppSettings _base;
  late bool _camera;
  late bool _continuous;
  late bool _registerPackage;
  late bool _checkOwner;
  late bool _requestStatus;
  late bool _requestPackage;
  late final LanFmuScanner _lan;
  late final DocumentDatabaseExport _export;
  late String _markApi;
  late String _serverId;
  late List<ExchangeServer> _servers;
  late List<ExchangeVariant> _variants;
  late List<TaskButton> _buttons;
  var _tab = 0;
  var _searching = false;
  var _sharing = false;

  @override
  void initState() {
    super.initState();
    final settings = widget.settings;
    _lan = widget.lanScanner ?? LanFmuScanner();
    _export = widget.databaseExport ?? DocumentDatabaseExport();
    _base = settings;
    _api = TextEditingController(text: settings.apiBaseUrl);
    _pin = TextEditingController(text: settings.settingsPin);
    _retention = TextEditingController(text: '${settings.documentRetentionDays}');
    _camera = settings.cameraScanEnabled;
    _continuous = settings.continuousScanEnabled;
    _registerPackage = settings.registerPackageBarcode;
    _checkOwner = settings.checkMarkOwner;
    _requestStatus = settings.requestSelectionStatus;
    _requestPackage = settings.requestPackageViaTrueApi;
    _markApi = settings.markCheckApi;
    _serverId = settings.exchangeServerId;
    _servers = [...settings.exchangeServers];
    _variants = [...settings.exchangeVariants];
    _buttons = TaskButtons.resolve(_tasks, settings.taskButtons);
  }

  @override
  void dispose() {
    _lan.cancel();
    _api.dispose();
    _pin.dispose();
    _retention.dispose();
    super.dispose();
  }

  AppSettings _current() {
    return AppSettings(
      apiBaseUrl: _api.text.trim(),
      intentAction: _base.intentAction,
      intentExtra: _base.intentExtra,
      cameraScanEnabled: _camera,
      continuousScanEnabled: _continuous,
      updateCheckIntervalMinutes: _base.updateCheckIntervalMinutes,
      settingsPin: _pin.text,
      registerPackageBarcode: _registerPackage,
      checkMarkOwner: _checkOwner,
      requestSelectionStatus: _requestStatus,
      requestPackageViaTrueApi: _requestPackage,
      markCheckApi: _markApi,
      exchangeServerId: _serverId,
      documentRetentionDays: _retentionDays,
      exchangeServers: _servers,
      exchangeVariants: _variants,
      taskButtons: _buttons,
    );
  }

  Future<void> _publish() async {
    final next = _current();
    _base = next;
    await _store.save(next);
  }

  void _exit() {
    final next = _current();
    unawaited(_store.save(next));
    Navigator.of(context).pop(next);
  }

  int get _retentionDays {
    final value = int.tryParse(_retention.text.trim()) ?? 0;
    return value < 0 ? 0 : value;
  }

  String get _serverName {
    for (final server in _servers) {
      if (server.id == _serverId) {
        return server.name;
      }
    }
    return '';
  }

  String _taskTitle(TaskButton button) {
    final title = button.title.trim();
    if (title.isNotEmpty) {
      return title;
    }
    for (final task in _tasks) {
      if (task.id == button.taskId) {
        return task.defaultTitle;
      }
    }
    return title;
  }

  Future<void> _openServers() async {
    final selectedId = await Navigator.of(context).push<String>(
      MaterialPageRoute(
        builder: (_) => ExchangeServerListScreen(
          servers: _servers,
          onChanged: (servers) {
            setState(() {
              _servers = servers;
              if (!_servers.any((server) => server.id == _serverId)) {
                _serverId = '';
              }
            });
            unawaited(_publish());
          },
        ),
      ),
    );
    if (!mounted || selectedId == null) {
      return;
    }

    setState(() => _serverId = selectedId);
    await _publish();
  }

  Future<void> _findApi() async {
    setState(() => _searching = true);
    try {
      final found = await _lan.findFirst();
      if (!mounted) {
        return;
      }
      if (found == null) {
        _messages.showSnack(
          context,
          const AppMessage(kind: MessageKind.warning, text: 'Fmu-Api в сети не найден'),
        );
        return;
      }
      _api.text = found;
      await _publish();
    } finally {
      if (mounted) {
        setState(() => _searching = false);
      }
    }
  }

  /// Копия main.db уходит в системное «Поделиться»: оттуда файл забирают себе.
  Future<void> _shareDatabase() async {
    if (_sharing) {
      return;
    }
    setState(() => _sharing = true);
    try {
      final path = await _export.shareDatabase();
      if (!mounted) {
        return;
      }
      if (path == null) {
        _messages.showSnack(
          context,
          const AppMessage(kind: MessageKind.warning, text: 'База документов ещё не создана'),
        );
        return;
      }
      _messages.showSnack(
        context,
        AppMessage(
          kind: MessageKind.success,
          text: 'Копия базы подготовлена: $path',
          data: {'path': path},
        ),
      );
    } catch (error) {
      if (!mounted) {
        return;
      }
      _messages.showSnack(
        context,
        AppMessage(kind: MessageKind.failure, text: describeFailure(error), data: {'error': '$error'}),
      );
    } finally {
      if (mounted) {
        setState(() => _sharing = false);
      }
    }
  }

  Future<void> _editTask(TaskButton? current) async {
    final result = await Navigator.of(context).push<TaskFormResult>(
      MaterialPageRoute(
        builder: (_) => TaskFormScreen(tasks: _tasks, buttons: _buttons, current: current),
      ),
    );
    if (result == null || !mounted) {
      return;
    }

    final next = [..._buttons];
    if (result.deleted) {
      final id = current?.taskId;
      if (id != null) {
        next.removeWhere((button) => button.taskId == id);
      }
    } else {
      final button = result.button;
      if (button == null) {
        return;
      }
      final index = next.indexWhere((item) => item.taskId == button.taskId);
      if (index >= 0) {
        next[index] = button;
      } else {
        next.add(button);
      }
    }
    setState(() => _buttons = next);
    await _publish();
  }

  Future<void> _editVariant(ExchangeVariant? current) async {
    final result = await Navigator.of(context).push<ExchangeVariantFormResult>(
      MaterialPageRoute(builder: (_) => ExchangeVariantFormScreen(current: current)),
    );
    if (result == null || !mounted) {
      return;
    }

    final next = [..._variants];
    if (result.deleted) {
      final id = current?.id;
      if (id != null) {
        next.removeWhere((variant) => variant.id == id);
      }
    } else {
      final variant = result.variant;
      if (variant == null) {
        return;
      }
      final index = next.indexWhere((item) => item.id == variant.id);
      if (index >= 0) {
        next[index] = variant;
      } else {
        next.add(variant);
      }
    }
    setState(() => _variants = next);
    await _publish();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Настройка Fmu-Api')),
      body: switch (_tab) {
        0 => _main(),
        1 => _equipment(),
        2 => _taskList(),
        _ => _exchange(),
      },
      bottomNavigationBar: ShellBottomBar(
        items: [
          ShellBarItem(
            label: 'Основные',
            icon: Icons.tune,
            selected: _tab == 0,
            onTap: () => setState(() => _tab = 0),
          ),
          ShellBarItem(
            label: 'Оборудование',
            icon: Icons.phone_android,
            selected: _tab == 1,
            onTap: () => setState(() => _tab = 1),
          ),
          ShellBarItem(
            label: 'Задачи',
            icon: Icons.checklist,
            selected: _tab == 2,
            onTap: () => setState(() => _tab = 2),
          ),
          ShellBarItem(
            label: 'Обмен',
            icon: Icons.sync_alt,
            selected: _tab == 3,
            onTap: () => setState(() => _tab = 3),
          ),
          ShellBarItem(label: 'Выход', icon: Icons.logout, onTap: _exit),
        ],
      ),
    );
  }

  Widget _main() {
    final apis = <String>{_markApi, 'True api'}.toList();
    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        ListTile(
          contentPadding: EdgeInsets.zero,
          title: const Text('Сервер обмена'),
          subtitle: Text(_serverName, style: const TextStyle(color: WebixDarkTheme.accent)),
          onTap: _openServers,
        ),
        TextField(
          controller: _pin,
          obscureText: true,
          decoration: const InputDecoration(labelText: 'Пин код настроек'),
          onChanged: (_) => unawaited(_publish()),
        ),
        TextField(
          controller: _retention,
          keyboardType: TextInputType.number,
          decoration: const InputDecoration(labelText: 'Сколько дней хранить документы'),
          onChanged: (_) => unawaited(_publish()),
        ),
        _group('Документ Сбор марок'),
        _switch('Регистрация штрихкода упаковок (по умолчанию)', _registerPackage, (value) {
          setState(() => _registerPackage = value);
          unawaited(_publish());
        }),
        _switch('Проверять владельца марки', _checkOwner, (value) {
          setState(() => _checkOwner = value);
          unawaited(_publish());
        }),
        _switch('Запрос статуса подбора марки', _requestStatus, (value) {
          setState(() => _requestStatus = value);
          unawaited(_publish());
        }),
        _switch('Запрашивать упаковку марки по true api', _requestPackage, (value) {
          setState(() => _requestPackage = value);
          unawaited(_publish());
        }),
        _group('Настройка проверки марки'),
        _switch('Непрерывное сканирование', _continuous, (value) {
          setState(() => _continuous = value);
          unawaited(_publish());
        }),
        _group('Настройка связи с FMU-API'),
        TextField(
          controller: _api,
          keyboardType: TextInputType.url,
          decoration: const InputDecoration(
            labelText: 'Адрес',
            hintText: 'http://10.10.10.41:2578',
          ),
          onChanged: (_) => unawaited(_publish()),
        ),
        Align(
          alignment: Alignment.centerLeft,
          child: TextButton(
            onPressed: _searching ? null : _findApi,
            child: Text(_searching ? 'Поиск…' : 'Найти в сети'),
          ),
        ),
        DropdownButtonFormField<String>(
          initialValue: _markApi,
          isExpanded: true,
          decoration: const InputDecoration(labelText: 'API для проверки марки'),
          items: [for (final api in apis) DropdownMenuItem(value: api, child: Text(api))],
          onChanged: (value) {
            if (value == null) {
              return;
            }
            setState(() => _markApi = value);
            unawaited(_publish());
          },
        ),
        ListTile(
          contentPadding: EdgeInsets.zero,
          title: const Text('Логи работы'),
          onTap: () {
            Navigator.of(context).push(
              MaterialPageRoute(builder: (_) => const WorkLogScreen()),
            );
          },
        ),
        ListTile(
          contentPadding: EdgeInsets.zero,
          enabled: !_sharing,
          title: const Text('Скопировать базу документов'),
          subtitle: const Text('Файл main.db через «Поделиться»'),
          trailing: _sharing
              ? const SizedBox(
                  width: 18,
                  height: 18,
                  child: CircularProgressIndicator(strokeWidth: 2),
                )
              : const Icon(Icons.share_outlined),
          onTap: _shareDatabase,
        ),
      ],
    );
  }

  Widget _equipment() {
    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        _switch('Сканирование камерой мобильного устройства', _camera, (value) {
          setState(() => _camera = value);
          unawaited(_publish());
        }),
      ],
    );
  }

  Widget _taskList() {
    return Column(
      children: [
        Align(
          alignment: Alignment.centerLeft,
          child: TextButton(onPressed: () => _editTask(null), child: const Text('Добавить задачу')),
        ),
        const _Captions(left: 'Экран задачи ТСД / Экран', right: 'Горячая клавиша'),
        Expanded(
          child: ListView.separated(
            itemCount: _buttons.length,
            separatorBuilder: (_, _) => const Divider(height: 1, color: WebixDarkTheme.border),
            itemBuilder: (context, index) {
              final button = _buttons[index];
              return ListTile(
                title: Text(_taskTitle(button)),
                subtitle: Text(taskSectionLabel(button.section)),
                trailing: Text(button.hotkey),
                onTap: () => _editTask(button),
              );
            },
          ),
        ),
      ],
    );
  }

  Widget _exchange() {
    return Column(
      children: [
        Align(
          alignment: Alignment.centerLeft,
          child: TextButton(
            onPressed: () => _editVariant(null),
            child: const Text('Добавить вариант обмена'),
          ),
        ),
        const _Captions(left: 'Операция', right: 'Порядок / Горячая клавиша'),
        Expanded(
          child: ListView.separated(
            itemCount: _variants.length,
            separatorBuilder: (_, _) => const Divider(height: 1, color: WebixDarkTheme.border),
            itemBuilder: (context, index) {
              final variant = _variants[index];
              return ListTile(
                title: Text(variant.operation),
                trailing: Text('${variant.order} / ${variant.hotkey}'),
                onTap: () => _editVariant(variant),
              );
            },
          ),
        ),
      ],
    );
  }

  Widget _group(String title) {
    return Padding(
      padding: const EdgeInsets.only(top: 20, bottom: 4),
      child: Text(title, style: const TextStyle(color: WebixDarkTheme.muted, fontSize: 13)),
    );
  }

  Widget _switch(String title, bool value, ValueChanged<bool> onChanged) {
    return SwitchListTile(
      contentPadding: EdgeInsets.zero,
      title: Text(title),
      value: value,
      onChanged: onChanged,
    );
  }
}

/// Заголовки колонок списка.
class _Captions extends StatelessWidget {
  const _Captions({required this.left, required this.right});

  final String left;
  final String right;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.fromLTRB(16, 4, 16, 4),
      child: Row(
        children: [
          Expanded(child: Text(left, style: const TextStyle(color: WebixDarkTheme.muted, fontSize: 12))),
          Text(right, style: const TextStyle(color: WebixDarkTheme.muted, fontSize: 12)),
        ],
      ),
    );
  }
}
