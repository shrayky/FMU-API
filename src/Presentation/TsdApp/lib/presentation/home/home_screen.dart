import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

import '../../application/mark/scan_queue.dart';
import '../../domain/mark/mark_card_row.dart';
import '../../domain/mark/mark_check_result.dart';
import '../../domain/scan/mark_code.dart';
import '../../domain/scan/scan_buffer.dart';
import '../../infrastructure/api/fmu_api_client.dart';
import '../../infrastructure/discovery/lan_fmu_scanner.dart';
import '../../infrastructure/scan/clipboard_scan_watcher.dart';
import '../../infrastructure/scan/intent_scan_channel.dart';
import '../../infrastructure/settings/app_settings.dart';
import '../../infrastructure/settings/settings_store.dart';
import '../../infrastructure/update/app_update.dart';
import '../../infrastructure/update/app_update_flow.dart';
import '../../theme/webix_dark_theme.dart';
import '../cards/mark_cards_view.dart';
import '../cards/mark_detail_page.dart';
import '../scan/camera_scan_page.dart';
import '../scan/continuous_scan_table.dart';
import '../settings/settings_screen.dart';
import '../update/update_offer.dart';

/// Главный экран ТСД: одиночная карточка или непрерывное сканирование.
class HomeScreen extends StatefulWidget {
  const HomeScreen({super.key, this.lanScanner, this.clipboard, this.updates});

  final LanFmuScanner? lanScanner;
  final ClipboardScanWatcher? clipboard;
  final AppUpdateFlow? updates;

  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> with WidgetsBindingObserver {
  final _focus = FocusNode();
  final _buffer = ScanBuffer();
  final _queue = ScanQueue();
  final _settingsStore = SettingsStore();
  final _intent = IntentScanChannel();
  late final ClipboardScanWatcher _clipboard;
  late final LanFmuScanner _lan;

  AppSettings _settings = const AppSettings();
  List<MarkCardRow> _rows = const [];
  String _placeholder = 'Сканируйте марку';
  bool _checking = false;
  String? _inn;
  bool _clipboardOnHome = true;
  Timer? _updateTimer;
  AppUpdate? _offer;
  bool _updating = false;
  bool _updateCheckFailed = false;
  late final AppUpdateFlow _updates;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addObserver(this);
    _lan = widget.lanScanner ?? LanFmuScanner();
    _clipboard = widget.clipboard ?? ClipboardScanWatcher();
    _updates = widget.updates ?? AppUpdateFlow();
    _intent.onScan = _onReadyCode;
    _clipboard.onCode = (code) => _accept(_buffer.completeRaw(code));
    _intent.start();
    _clipboard.start();
    _loadSettings();
    WidgetsBinding.instance.addPostFrameCallback((_) => _focus.requestFocus());
  }

  @override
  void dispose() {
    WidgetsBinding.instance.removeObserver(this);
    _updateTimer?.cancel();
    _lan.cancel();
    _intent.stop();
    _clipboard.stop();
    _focus.dispose();
    super.dispose();
  }

  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    if (state == AppLifecycleState.resumed) {
      if (_clipboardOnHome) {
        _clipboard.start();
      }
      return;
    }

    if (state == AppLifecycleState.paused ||
        state == AppLifecycleState.hidden ||
        state == AppLifecycleState.detached) {
      _clipboard.stop();
    }
  }

  Future<void> _loadSettings() async {
    final settings = await _settingsStore.load();
    if (!mounted) {
      return;
    }

    setState(() => _settings = settings);
    unawaited(_configureIntent(settings));
    if (settings.apiBaseUrl.isEmpty) {
      await _discoverApi();
      return;
    }

    await _refreshInn();
    _armUpdateChecks();
  }

  Future<void> _configureIntent(AppSettings settings) async {
    try {
      await _intent.configure(action: settings.intentAction, extra: settings.intentExtra);
    } catch (_) {}
  }

  void _clearUpdateFailure() {
    if (!_updateCheckFailed || !mounted) {
      return;
    }
    setState(() => _updateCheckFailed = false);
  }

  Future<void> _discoverApi() async {
    _clearUpdateFailure();
    setState(() => _placeholder = 'Поиск Fmu-Api в сети…');
    final found = await _lan.findFirst();
    if (!mounted) {
      return;
    }

    if (found == null) {
      _updateTimer?.cancel();
      _clearUpdateFailure();
      setState(() => _placeholder = 'Укажите адрес Fmu-Api в настройках');
      return;
    }

    final settings = _settings.copyWith(apiBaseUrl: found);
    await _settingsStore.save(settings);
    if (!mounted) {
      return;
    }

    setState(() {
      _settings = settings;
      _placeholder = 'Найден $found';
    });
    await _refreshInn();
    _armUpdateChecks();
  }

  void _armUpdateChecks() {
    _updateTimer?.cancel();
    _updateTimer = null;
    if (_settings.apiBaseUrl.isEmpty) {
      _clearUpdateFailure();
      return;
    }

    unawaited(_checkForUpdate());
    _updateTimer = Timer.periodic(Duration(minutes: _settings.updateCheckIntervalMinutes), (_) {
      unawaited(_checkForUpdate());
    });
  }

  Future<void> _checkForUpdate() async {
    if (_settings.apiBaseUrl.isEmpty) {
      _clearUpdateFailure();
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
      setState(() {
        _offer = offer;
        _updateCheckFailed = false;
      });
    } catch (_) {
      if (!mounted) {
        return;
      }
      setState(() => _updateCheckFailed = _offer == null);
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
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('$error')));
    } finally {
      if (mounted) {
        setState(() => _updating = false);
      }
    }
  }

  Future<void> _refreshInn() async {
    if (_settings.apiBaseUrl.isEmpty) {
      _inn = null;
      return;
    }

    try {
      _inn = await FmuApiClient(baseUrl: _settings.apiBaseUrl).organisationInn();
    } catch (_) {
      _inn = null;
    }
  }

  Future<void> _openSettings() async {
    _clipboardOnHome = false;
    _clipboard.stop();
    final result = await Navigator.of(context).push<AppSettings>(
      MaterialPageRoute(builder: (_) => SettingsScreen(settings: _settings, updates: _updates)),
    );
    _clipboardOnHome = true;
    if (WidgetsBinding.instance.lifecycleState == AppLifecycleState.resumed) {
      _clipboard.start();
    }
    _focus.requestFocus();
    if (result == null) {
      return;
    }

    await _settingsStore.save(result);
    await _loadSettings();
  }

  Future<void> _openCamera() async {
    _clipboardOnHome = false;
    _clipboard.stop();
    final code = await Navigator.of(context).push<String>(
      MaterialPageRoute(builder: (_) => const CameraScanPage()),
    );
    _clipboardOnHome = true;
    if (WidgetsBinding.instance.lifecycleState == AppLifecycleState.resumed) {
      _clipboard.start();
    }
    _focus.requestFocus();
    if (code == null || code.isEmpty) {
      return;
    }

    _onReadyCode(code);
  }

  KeyEventResult _onKey(FocusNode node, KeyEvent event) {
    if (event is! KeyDownEvent) {
      return KeyEventResult.ignored;
    }

    final control = HardwareKeyboard.instance.isControlPressed;
    if (control && event.logicalKey == LogicalKeyboardKey.keyV) {
      _paste();
      return KeyEventResult.handled;
    }

    if (control &&
        (event.logicalKey == LogicalKeyboardKey.bracketRight || event.character == ']')) {
      _accept(_buffer.pushChar('\x1d'));
      return KeyEventResult.handled;
    }

    final isEnter = event.logicalKey == LogicalKeyboardKey.enter ||
        event.logicalKey == LogicalKeyboardKey.numpadEnter ||
        event.character == '\r' ||
        event.character == '\n';
    if (isEnter) {
      _accept(_buffer.pushChar('\r'));
      return KeyEventResult.handled;
    }

    final character = event.character;
    if (character != null && character.isNotEmpty) {
      _accept(_buffer.pushChar(character));
      return KeyEventResult.handled;
    }

    return KeyEventResult.ignored;
  }

  Future<void> _paste() async {
    final data = await Clipboard.getData(Clipboard.kTextPlain);
    final text = data?.text ?? '';
    _onReadyCode(text);
    await Clipboard.setData(const ClipboardData(text: ''));
  }

  void _onReadyCode(String code) {
    _clipboard.remember(code);
    _accept(_buffer.completeRaw(code));
  }

  void _accept(ScanPushResult result) {
    final code = result.code;
    if (code == null) {
      return;
    }

    if (!MarkCode.looksLikeMark(code)) {
      _rejectNotMark();
      return;
    }

    if (_settings.continuousScanEnabled) {
      _enqueue(code);
      return;
    }

    if (_checking) {
      return;
    }

    _check(code);
  }

  void _rejectNotMark() {
    ScaffoldMessenger.of(context).hideCurrentSnackBar();
    ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Это не марка')));
    setState(() {
      if (!_settings.continuousScanEnabled) {
        _rows = const [];
      }
      _placeholder = 'Это не марка';
    });
  }

  void _enqueue(String code) {
    if (_queue.add(code) == null) {
      return;
    }

    setState(() {});
    _pump();
  }

  void _pump() {
    for (final item in _queue.takeForRequest()) {
      unawaited(_checkQueued(item));
    }
  }

  Future<void> _checkQueued(ScanQueueItem item) async {
    try {
      final result = await _requestQueued(item);
      if (!mounted || result == null) {
        return;
      }

      _queue.complete(item, result);
    } catch (error) {
      if (!mounted) {
        return;
      }

      _queue.fail(item, error.toString());
    } finally {
      if (mounted) {
        setState(() {});
        _pump();
      }
    }
  }

  Future<MarkCheckResult?> _requestQueued(ScanQueueItem item) async {
    if (_settings.apiBaseUrl.isEmpty) {
      _queue.fail(item, 'Укажите адрес Fmu-Api в настройках');
      return null;
    }

    var inn = _inn;
    if (inn == null || inn.isEmpty) {
      inn = await FmuApiClient(baseUrl: _settings.apiBaseUrl).organisationInn();
      _inn = inn;
    }

    if (inn.isEmpty) {
      _queue.fail(item, 'В Fmu-Api не задан ИНН организации');
      return null;
    }

    return FmuApiClient(baseUrl: _settings.apiBaseUrl).checkMarkResult(inn: inn, mark: item.mark);
  }

  Future<void> _openDetail(ScanQueueItem item) async {
    if (!item.canOpen) {
      return;
    }

    _clipboardOnHome = false;
    _clipboard.stop();
    await Navigator.of(context).push(
      MaterialPageRoute(builder: (_) => MarkDetailPage(rows: item.rows)),
    );
    if (!mounted) {
      return;
    }

    _clipboardOnHome = true;
    if (WidgetsBinding.instance.lifecycleState == AppLifecycleState.resumed) {
      _clipboard.start();
    }
    _focus.requestFocus();
  }

  Future<void> _check(String mark) async {
    if (_settings.apiBaseUrl.isEmpty) {
      setState(() {
        _rows = const [];
        _placeholder = 'Укажите адрес Fmu-Api в настройках';
      });
      return;
    }

    setState(() {
      _checking = true;
      _rows = const [];
      _placeholder = 'Проверка марки…';
    });

    try {
      var inn = _inn;
      if (inn == null || inn.isEmpty) {
        inn = await FmuApiClient(baseUrl: _settings.apiBaseUrl).organisationInn();
        _inn = inn;
      }

      if (inn.isEmpty) {
        setState(() {
          _rows = const [];
          _placeholder = 'В Fmu-Api не задан ИНН организации';
        });
        return;
      }

      final rows = await FmuApiClient(baseUrl: _settings.apiBaseUrl).checkMark(inn: inn, mark: mark);
      if (!mounted) {
        return;
      }

      setState(() {
        _rows = rows;
        _placeholder = rows.isEmpty ? 'Нет данных' : 'Сканируйте марку';
      });
    } catch (error) {
      if (!mounted) {
        return;
      }

      setState(() {
        _rows = [
          MarkCardRow(label: '', value: error.toString(), tone: MarkCardTone.error),
        ];
      });
    } finally {
      if (mounted) {
        setState(() => _checking = false);
        _focus.requestFocus();
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        leadingWidth: 48,
        leading: const Padding(
          padding: EdgeInsets.all(10),
          child: Image(image: AssetImage('assets/logo_fmuapi.png')),
        ),
        title: const Text('Fmu-Api: Проверка марки'),
        actions: [
          if (_settings.continuousScanEnabled && _queue.items.isNotEmpty)
            IconButton(
              tooltip: 'Очистить',
              icon: const Icon(Icons.delete_outline),
              onPressed: () => setState(_queue.clear),
            ),
          IconButton(
            icon: const Icon(Icons.settings),
            onPressed: _openSettings,
          ),
        ],
      ),
      body: Column(
        children: [
          if (_offer == null && _updateCheckFailed)
            const UpdateCheckFailure(),
          if (_offer != null)
            UpdateOffer(
              versionName: _offer!.versionName,
              busy: _updating,
              onUpdate: _installUpdate,
            ),
          Expanded(
            child: Focus(
              focusNode: _focus,
              autofocus: true,
              onKeyEvent: _onKey,
              child: ColoredBox(
                color: WebixDarkTheme.background,
                child: _settings.continuousScanEnabled
                    ? ContinuousScanTable(
                        items: _queue.items,
                        placeholder: _placeholder,
                        onOpen: _openDetail,
                      )
                    : GestureDetector(
                        behavior: HitTestBehavior.opaque,
                        onTap: () => _focus.requestFocus(),
                        child: MarkCardsView(rows: _rows, placeholder: _placeholder),
                      ),
              ),
            ),
          ),
        ],
      ),
      floatingActionButton: _settings.cameraScanEnabled
          ? FloatingActionButton(
              onPressed: _openCamera,
              backgroundColor: WebixDarkTheme.accent,
              child: const Icon(Icons.qr_code_scanner),
            )
          : null,
    );
  }
}
