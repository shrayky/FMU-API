import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:package_info_plus/package_info_plus.dart';

import '../../application/message/message_center.dart';
import '../../application/shell/task_buttons.dart';
import '../../application/shell/task_registry.dart';
import '../../domain/message/app_message.dart';
import '../../domain/message/failure_text.dart';
import '../../domain/task/task_button.dart';
import '../../domain/task/task_section.dart';
import '../../domain/task/tsd_task.dart';
import '../../infrastructure/discovery/lan_fmu_scanner.dart';
import '../../infrastructure/settings/app_settings.dart';
import '../../infrastructure/settings/settings_store.dart';
import '../../infrastructure/update/app_update.dart';
import '../../infrastructure/update/app_update_flow.dart';
import '../../theme/webix_dark_theme.dart';
import '../settings/settings_pin_screen.dart';
import '../settings/settings_screen.dart';
import '../shell/journal_icon.dart';
import '../shell/shell_bottom_bar.dart';
import '../update/update_offer.dart';

/// Меню задач ТСД.
class HomeScreen extends StatefulWidget {
  const HomeScreen({
    super.key,
    this.lanScanner,
    this.updates,
    this.appVersion,
  });

  final LanFmuScanner? lanScanner;
  final AppUpdateFlow? updates;
  final String? appVersion;

  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

enum _MenuArea { tasks, exchange }

enum _AddressNotice { none, searching, missing }

class _HomeScreenState extends State<HomeScreen> {
  final _focus = FocusNode();
  final _settingsStore = SettingsStore();
  final _messages = MessageCenter();
  final _registry = const TaskRegistry();
  late final LanFmuScanner _lan;
  late final AppUpdateFlow _updates;
  late final List<TsdTask> _tasks;

  AppSettings _settings = const AppSettings();
  List<TaskButton> _buttons = const [];
  _MenuArea _area = _MenuArea.tasks;
  TaskSection? _section = TaskSection.documents;
  _AddressNotice _address = _AddressNotice.none;
  Timer? _updateTimer;
  AppUpdate? _offer;
  bool _updating = false;
  String? _version;
  var _opening = false;

  @override
  void initState() {
    super.initState();
    _lan = widget.lanScanner ?? LanFmuScanner();
    _updates = widget.updates ?? AppUpdateFlow();
    _tasks = _registry.tasks();
    _version = widget.appVersion;
    _buttons = TaskButtons.resolve(_tasks, null);
    _showTasks();
    if (_version == null) {
      unawaited(_loadVersion());
    }
    unawaited(_loadSettings());
    WidgetsBinding.instance.addPostFrameCallback((_) => _focus.requestFocus());
  }

  @override
  void dispose() {
    _updateTimer?.cancel();
    _lan.cancel();
    _focus.dispose();
    super.dispose();
  }

  Future<void> _loadVersion() async {
    try {
      final info = await PackageInfo.fromPlatform();
      if (!mounted) {
        return;
      }
      setState(() => _version = info.version);
    } catch (_) {}
  }

  void _showTasks() {
    final documents = TaskButtons.visible(_buttons, TaskSection.documents);
    final service = TaskButtons.visible(_buttons, TaskSection.service);
    _area = _MenuArea.tasks;
    if (documents.isNotEmpty) {
      _section = TaskSection.documents;
    } else if (service.isNotEmpty) {
      _section = TaskSection.service;
    } else {
      _section = null;
    }
  }

  Future<void> _loadSettings() async {
    final settings = await _settingsStore.load();
    if (!mounted) {
      return;
    }

    setState(() {
      _settings = settings;
      _buttons = TaskButtons.resolve(_tasks, settings.taskButtons);
      _showTasks();
    });
    if (settings.apiBaseUrl.isEmpty) {
      await _discoverApi();
      return;
    }

    setState(() => _address = _AddressNotice.none);
    _armUpdateChecks();
  }

  Future<void> _discoverApi() async {
    if (mounted) {
      setState(() => _address = _AddressNotice.searching);
    }
    final found = await _lan.findFirst();
    if (!mounted) {
      return;
    }

    if (found == null) {
      _updateTimer?.cancel();
      _updateTimer = null;
      setState(() => _address = _AddressNotice.missing);
      return;
    }

    final settings = _settings.copyWith(apiBaseUrl: found);
    await _settingsStore.save(settings);
    if (!mounted) {
      return;
    }

    setState(() {
      _settings = settings;
      _address = _AddressNotice.none;
    });
    _armUpdateChecks();
  }

  void _armUpdateChecks() {
    _updateTimer?.cancel();
    _updateTimer = null;
    if (_settings.apiBaseUrl.isEmpty) {
      return;
    }

    unawaited(_checkForUpdate());
    _updateTimer = Timer.periodic(Duration(minutes: _settings.updateCheckIntervalMinutes), (_) {
      unawaited(_checkForUpdate());
    });
  }

  Future<void> _checkForUpdate() async {
    if (_settings.apiBaseUrl.isEmpty) {
      return;
    }

    if (_updating) {
      return;
    }

    try {
      final offer = await _updates.check(_settings.apiBaseUrl);
      if (!mounted) {
        return;
      }
      setState(() => _offer = offer);
    } catch (_) {
      // Сбой проверки обновления не показываем: меню остаётся рабочим.
    }
  }

  Future<void> _installUpdate() async {
    final offer = _offer;
    if (offer == null || _updating) {
      return;
    }

    setState(() => _updating = true);
    try {
      await _updates.install(_settings.apiBaseUrl, offer);
    } catch (error) {
      if (!mounted) {
        return;
      }
      _messages.showSnack(
        context,
        AppMessage(
          kind: MessageKind.failure,
          text: describeFailure(error),
          data: {'error': '$error'},
        ),
      );
    } finally {
      if (mounted) {
        setState(() => _updating = false);
      }
    }
  }

  Future<void> _openSettings() async {
    final pin = _settings.settingsPin.trim();
    if (pin.isNotEmpty) {
      final allowed = await Navigator.of(context).push<bool>(
        MaterialPageRoute(builder: (_) => SettingsPinScreen(pin: pin)),
      );
      if (!mounted || allowed != true) {
        _focus.requestFocus();
        return;
      }
    }

    final result = await Navigator.of(context).push<AppSettings>(
      MaterialPageRoute(builder: (_) => SettingsScreen(settings: _settings)),
    );
    if (!mounted) {
      return;
    }
    if (result != null) {
      await _settingsStore.save(result);
    }
    await _loadSettings();
    if (!mounted) {
      return;
    }
    _focus.requestFocus();
  }

  TsdTask? _task(String id) {
    for (final task in _tasks) {
      if (task.id == id) {
        return task;
      }
    }
    return null;
  }

  Future<void> _openButton(TaskButton button, {required bool journal}) async {
    if (_opening) {
      return;
    }
    final task = _task(button.taskId);
    if (task == null) {
      return;
    }
    final screen = journal ? task.openJournal?.call(_settings) : task.open(_settings);
    if (screen == null) {
      return;
    }

    _opening = true;
    await Navigator.of(context).push(MaterialPageRoute(builder: (_) => screen));
    _opening = false;
    if (!mounted) {
      return;
    }
    _focus.requestFocus();
  }

  KeyEventResult _onKey(FocusNode node, KeyEvent event) {
    if (event is! KeyDownEvent) {
      return KeyEventResult.ignored;
    }

    final character = event.character;
    if (character == null) {
      return KeyEventResult.ignored;
    }

    final button = TaskButtons.matchHotkey(_buttons, character);
    if (button == null) {
      return KeyEventResult.ignored;
    }

    unawaited(_openButton(button, journal: false));
    return KeyEventResult.handled;
  }

  String get _title {
    final version = _version;
    if (version == null || version.isEmpty) {
      return 'Fmu-Api';
    }
    return 'Fmu-Api v $version';
  }

  @override
  Widget build(BuildContext context) {
    final documents = TaskButtons.visible(_buttons, TaskSection.documents);
    final service = TaskButtons.visible(_buttons, TaskSection.service);
    final showExchange = _settings.exchangeVariants.isNotEmpty;
    return Scaffold(
      appBar: AppBar(
        leadingWidth: 48,
        leading: const Padding(
          padding: EdgeInsets.all(10),
          child: Image(image: AssetImage('assets/logo_fmuapi.png')),
        ),
        title: Text(_title),
      ),
      body: Column(
        children: [
          if (_offer != null)
            UpdateOffer(
              versionName: _offer!.versionName,
              busy: _updating,
              onUpdate: _installUpdate,
            ),
          if (_address == _AddressNotice.searching) const _MenuBanner(text: 'Поиск Fmu-Api в сети…'),
          if (_address == _AddressNotice.missing)
            const _MenuBanner(text: 'Укажите адрес Fmu-Api в настройках'),
          Expanded(
            child: Focus(
              focusNode: _focus,
              autofocus: true,
              descendantsAreFocusable: false,
              onKeyEvent: _onKey,
              child: ColoredBox(
                color: WebixDarkTheme.background,
                child: _body(),
              ),
            ),
          ),
        ],
      ),
      bottomNavigationBar: ShellBottomBar(
        items: [
          if (documents.isNotEmpty)
            ShellBarItem(
              label: 'Документы',
              icon: Icons.article_outlined,
              selected: _area == _MenuArea.tasks && _section == TaskSection.documents,
              onTap: () {
                setState(() {
                  _area = _MenuArea.tasks;
                  _section = TaskSection.documents;
                });
                _focus.requestFocus();
              },
            ),
          if (service.isNotEmpty)
            ShellBarItem(
              label: 'Сервис',
              icon: Icons.handyman_outlined,
              selected: _area == _MenuArea.tasks && _section == TaskSection.service,
              onTap: () {
                setState(() {
                  _area = _MenuArea.tasks;
                  _section = TaskSection.service;
                });
                _focus.requestFocus();
              },
            ),
          if (showExchange)
            ShellBarItem(
              label: 'Обмен',
              icon: Icons.sync_alt,
              selected: _area == _MenuArea.exchange,
              onTap: () {
                setState(() => _area = _MenuArea.exchange);
                _focus.requestFocus();
              },
            ),
          ShellBarItem(
            label: 'Настройки',
            icon: Icons.settings_outlined,
            onTap: _openSettings,
          ),
          const ShellBarItem(
            label: 'Выход',
            icon: Icons.logout,
            onTap: SystemNavigator.pop,
          ),
        ],
      ),
    );
  }

  Widget _body() {
    if (_area == _MenuArea.exchange && _settings.exchangeVariants.isNotEmpty) {
      return const SizedBox.expand();
    }

    final section = _section;
    if (section == null) {
      return const Center(child: Text('Все задачи выключены'));
    }

    final buttons = TaskButtons.visible(_buttons, section);
    if (buttons.isEmpty) {
      return const Center(child: Text('Все задачи выключены'));
    }

    return ListView(
      children: [
        for (final button in buttons) _TaskRow(
          button: button,
          showJournal: _showsJournal(button),
          onOpen: () => _openButton(button, journal: false),
          onJournal: () => _openButton(button, journal: true),
        ),
      ],
    );
  }

  bool _showsJournal(TaskButton button) {
    final task = _task(button.taskId);
    if (task == null) {
      return false;
    }
    return TaskButtons.journalVisible(task, button);
  }
}

/// Строка задачи на меню: подпись и, если нужно, журнал.
class _TaskRow extends StatelessWidget {
  const _TaskRow({
    required this.button,
    required this.showJournal,
    required this.onOpen,
    required this.onJournal,
  });

  final TaskButton button;
  final bool showJournal;
  final VoidCallback onOpen;
  final VoidCallback onJournal;

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        Expanded(
          child: TextButton(
            onPressed: onOpen,
            style: TextButton.styleFrom(
              foregroundColor: WebixDarkTheme.accent,
              alignment: Alignment.centerLeft,
            ),
            child: Text(button.title, style: const TextStyle(color: WebixDarkTheme.accent, fontSize: 18)),
          ),
        ),
        if (showJournal)
          IconButton(
            tooltip: 'Журнал',
            onPressed: onJournal,
            icon: const JournalIcon(),
          ),
      ],
    );
  }
}

/// Баннер поиска адреса или пустого адреса.
class _MenuBanner extends StatelessWidget {
  const _MenuBanner({required this.text});

  final String text;

  @override
  Widget build(BuildContext context) {
    return Material(
      color: WebixDarkTheme.surface,
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 4),
        child: Align(alignment: Alignment.centerLeft, child: Text(text)),
      ),
    );
  }
}
