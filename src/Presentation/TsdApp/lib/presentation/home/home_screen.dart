import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

import '../../domain/mark/mark_card_row.dart';
import '../../domain/scan/scan_buffer.dart';
import '../../infrastructure/api/fmu_api_client.dart';
import '../../infrastructure/discovery/lan_fmu_scanner.dart';
import '../../infrastructure/scan/clipboard_scan_watcher.dart';
import '../../infrastructure/scan/intent_scan_channel.dart';
import '../../infrastructure/settings/app_settings.dart';
import '../../infrastructure/settings/settings_store.dart';
import '../../theme/webix_dark_theme.dart';
import '../cards/mark_cards_view.dart';
import '../scan/camera_scan_page.dart';
import '../settings/settings_screen.dart';

/// Главный экран ТСД: ожидает скан и рисует карточку марки.
class HomeScreen extends StatefulWidget {
  const HomeScreen({super.key, this.lanScanner});

  final LanFmuScanner? lanScanner;

  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> {
  final _focus = FocusNode();
  final _buffer = ScanBuffer();
  final _settingsStore = SettingsStore();
  final _intent = IntentScanChannel();
  final _clipboard = ClipboardScanWatcher();
  late final LanFmuScanner _lan;

  AppSettings _settings = const AppSettings();
  List<MarkCardRow> _rows = const [];
  String _placeholder = 'Сканируйте марку';
  bool _checking = false;
  String? _inn;

  @override
  void initState() {
    super.initState();
    _lan = widget.lanScanner ?? LanFmuScanner();
    _intent.onScan = _onReadyCode;
    _clipboard.onCode = (code) => _accept(_buffer.completeRaw(code));
    _intent.start();
    _clipboard.start();
    _loadSettings();
    WidgetsBinding.instance.addPostFrameCallback((_) => _focus.requestFocus());
  }

  @override
  void dispose() {
    _lan.cancel();
    _intent.stop();
    _clipboard.stop();
    _focus.dispose();
    super.dispose();
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
  }

  Future<void> _configureIntent(AppSettings settings) async {
    try {
      await _intent.configure(action: settings.intentAction, extra: settings.intentExtra);
    } catch (_) {}
  }

  Future<void> _discoverApi() async {
    setState(() => _placeholder = 'Поиск Fmu-Api в сети…');
    final found = await _lan.findFirst();
    if (!mounted) {
      return;
    }

    if (found == null) {
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
    _clipboard.stop();
    final result = await Navigator.of(context).push<AppSettings>(
      MaterialPageRoute(builder: (_) => SettingsScreen(settings: _settings)),
    );
    _clipboard.start();
    _focus.requestFocus();
    if (result == null) {
      return;
    }

    await _settingsStore.save(result);
    await _loadSettings();
  }

  Future<void> _openCamera() async {
    _clipboard.stop();
    final code = await Navigator.of(context).push<String>(
      MaterialPageRoute(builder: (_) => const CameraScanPage()),
    );
    _clipboard.start();
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
    if (code == null || _checking) {
      return;
    }

    _check(code);
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
          IconButton(
            icon: const Icon(Icons.settings),
            onPressed: _openSettings,
          ),
        ],
      ),
      body: Focus(
        focusNode: _focus,
        autofocus: true,
        onKeyEvent: _onKey,
        child: GestureDetector(
          behavior: HitTestBehavior.opaque,
          onTap: () => _focus.requestFocus(),
          child: ColoredBox(
            color: WebixDarkTheme.background,
            child: MarkCardsView(rows: _rows, placeholder: _placeholder),
          ),
        ),
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
