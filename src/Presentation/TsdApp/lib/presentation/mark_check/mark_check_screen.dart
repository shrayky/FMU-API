import 'dart:async';

import 'package:flutter/material.dart';

import '../../application/mark/scan_queue.dart';
import '../../application/message/message_center.dart';
import '../../domain/mark/mark_card_row.dart';
import '../../domain/mark/mark_check_result.dart';
import '../../domain/message/app_message.dart';
import '../../domain/scan/barcode_kind.dart';
import '../../infrastructure/api/fmu_api_client.dart';
import '../../infrastructure/scan/clipboard_scan_watcher.dart';
import '../../infrastructure/settings/app_settings.dart';
import '../../theme/webix_dark_theme.dart';
import '../cards/mark_cards_view.dart';
import '../cards/mark_detail_page.dart';
import '../scan/camera_scan_page.dart';
import '../scan/continuous_scan_table.dart';
import '../scan/scan_session.dart';
import '../shell/camera_scan_button.dart';
import '../shell/screen_commands.dart';

/// Проверка марки: карточка, непрерывный режим, камера и интент сканера.
class MarkCheckScreen extends StatefulWidget {
  const MarkCheckScreen({super.key, required this.settings, this.clipboard});

  final AppSettings settings;
  final ClipboardScanWatcher? clipboard;

  @override
  State<MarkCheckScreen> createState() => _MarkCheckScreenState();
}

class _MarkCheckScreenState extends State<MarkCheckScreen> with WidgetsBindingObserver {
  final _focus = FocusNode();
  final _queue = ScanQueue();
  final _messages = MessageCenter();
  late final ScanSession _session;

  late String _placeholder;
  List<MarkCardRow> _rows = const [];
  bool _checking = false;
  String? _inn;

  AppSettings get _settings => widget.settings;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addObserver(this);
    _placeholder = _settings.apiBaseUrl.trim().isEmpty
        ? 'Укажите адрес Fmu-Api в настройках'
        : 'Сканируйте марку';
    _session = ScanSession(
      settings: _settings,
      clipboard: widget.clipboard,
      onCode: _onCode,
    );
    _session.start();
    if (_settings.apiBaseUrl.trim().isNotEmpty) {
      unawaited(_refreshInn());
    }
    WidgetsBinding.instance.addPostFrameCallback((_) => _focus.requestFocus());
  }

  @override
  void dispose() {
    WidgetsBinding.instance.removeObserver(this);
    _session.dispose();
    _focus.dispose();
    super.dispose();
  }

  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    _session.onLifecycle(state);
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

  Future<void> _openCamera() async {
    _session.pause();
    final code = await Navigator.of(context).push<String>(
      MaterialPageRoute(builder: (_) => CameraScanPage(kinds: const [BarcodeKind.mark])),
    );
    if (!mounted) {
      return;
    }

    _session.resume();
    _focus.requestFocus();
    if (code == null || code.isEmpty) {
      return;
    }

    _session.submit(code);
  }

  void _onCode(String code) {
    if (!mounted) {
      return;
    }

    if (barcodeKindOf(code) != BarcodeKind.mark) {
      _rejectNotMark(code);
      return;
    }

    if (_settings.continuousScanEnabled) {
      _enqueue(code);
      return;
    }

    if (_checking) {
      return;
    }

    unawaited(_check(code));
  }

  void _rejectNotMark(String code) {
    if (!mounted) {
      return;
    }

    _messages.showSnack(
      context,
      AppMessage(kind: MessageKind.warning, text: 'Это не марка', data: {'barcode': code}),
    );
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

    _session.pause();
    await Navigator.of(context).push(
      MaterialPageRoute(builder: (_) => MarkDetailPage(rows: item.rows)),
    );
    if (!mounted) {
      return;
    }

    _session.resume();
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

  void _clear() {
    setState(() {
      _queue.clear();
      _rows = const [];
      _placeholder = 'Сканируйте марку';
    });
    _focus.requestFocus();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Проверка марки', style: TextStyle(color: WebixDarkTheme.accent)),
      ),
      body: Focus(
        focusNode: _focus,
        autofocus: true,
        onKeyEvent: _session.onKey,
        child: GestureDetector(
          behavior: HitTestBehavior.opaque,
          onTap: () => _focus.requestFocus(),
          child: ColoredBox(
            color: WebixDarkTheme.background,
            child: _settings.continuousScanEnabled
                ? ContinuousScanTable(
                    items: _queue.items,
                    placeholder: _placeholder,
                    onOpen: _openDetail,
                  )
                : MarkCardsView(rows: _rows, placeholder: _placeholder),
          ),
        ),
      ),
      floatingActionButton: _settings.cameraScanEnabled
          ? CameraScanButton(heroTag: 'mark-check-camera', onPressed: _openCamera)
          : null,
      bottomNavigationBar: ScreenCommands(
        commands: [
          ScreenCommand(label: 'Очистить', onPressed: _clear),
          ScreenCommand(label: 'Выход', onPressed: () => Navigator.of(context).pop()),
        ],
      ),
    );
  }
}
