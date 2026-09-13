import 'package:flutter/material.dart';

import '../../infrastructure/scan/broadcast_monitor_channel.dart';
import '../../infrastructure/scan/clipboard_scan_watcher.dart';
import '../../infrastructure/scan/scanner_broadcast.dart';
import '../../theme/webix_dark_theme.dart';

/// Результат выбора action/extra из пойманного broadcast.
class BroadcastPick {
  const BroadcastPick({required this.action, required this.extra});

  final String action;
  final String extra;
}

/// Окно просмотра сообщений сканера: broadcast, буфер и клавиатура.
class BroadcastMonitorScreen extends StatefulWidget {
  const BroadcastMonitorScreen({super.key, this.intentAction = ''});

  final String intentAction;

  @override
  State<BroadcastMonitorScreen> createState() => _BroadcastMonitorScreenState();
}

class _BroadcastMonitorScreenState extends State<BroadcastMonitorScreen> {
  final _channel = BroadcastMonitorChannel();
  final _clipboard = ClipboardScanWatcher(requireMark: false, clearAfterEmit: false);
  final _events = <BroadcastEvent>[];
  final _keys = FocusNode();
  final _keysCtrl = TextEditingController();
  bool _listening = false;
  int _actionCount = 0;
  String? _error;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      _keys.requestFocus();
      _start();
    });
  }

  @override
  void dispose() {
    _clipboard.stop();
    if (_listening) {
      _channel.stop();
    }
    _keys.dispose();
    _keysCtrl.dispose();
    super.dispose();
  }

  Future<void> _toggle() async {
    if (_listening) {
      await _stop();
      return;
    }

    await _start();
  }

  Future<void> _start() async {
    final actions = <String>{
      ...ScannerBroadcast.knownActions,
      if (widget.intentAction.trim().isNotEmpty) widget.intentAction.trim(),
    }.toList();

    _channel.onEvent = (event) => _add(event);
    _clipboard.onCode = (text) {
      _add(
        BroadcastEvent(
          action: 'clipboard',
          extras: {'data': text},
          at: DateTime.now(),
        ),
      );
    };

    _clipboard.start();
    try {
      final count = await _channel.start(actions);
      if (!mounted) {
        return;
      }
      setState(() {
        _listening = true;
        _actionCount = count;
        _error = null;
      });
    } catch (error) {
      if (!mounted) {
        return;
      }
      setState(() {
        _listening = true;
        _actionCount = 0;
        _error = error.toString();
      });
    }
  }

  Future<void> _stop() async {
    _clipboard.stop();
    await _channel.stop();
    if (!mounted) {
      return;
    }
    setState(() => _listening = false);
  }

  void _add(BroadcastEvent event) {
    if (!mounted) {
      return;
    }

    if (_events.isNotEmpty &&
        _events.first.action == event.action &&
        _mapEquals(_events.first.extras, event.extras)) {
      return;
    }

    setState(() => _events.insert(0, event));
  }

  bool _mapEquals(Map<String, String> left, Map<String, String> right) {
    if (left.length != right.length) {
      return false;
    }

    return left.entries.every((entry) => right[entry.key] == entry.value);
  }

  void _onKeyboard(String value) {
    final code = value.replaceAll('\r', '').replaceAll('\n', '').trim();
    if (code.isEmpty) {
      return;
    }

    _add(
      BroadcastEvent(
        action: 'keyboard',
        extras: {'data': code},
        at: DateTime.now(),
      ),
    );
    _keysCtrl.clear();
    _keys.requestFocus();
  }

  void _pick(BroadcastEvent event) {
    if (event.action == 'clipboard' || event.action == 'keyboard') {
      return;
    }

    Navigator.of(context).pop(
      BroadcastPick(
        action: event.action,
        extra: ScannerBroadcast.guessExtraKey(event.extras),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final status = _error ??
        (_listening
            ? 'Слушаю $_actionCount action. Сканируйте. Тап по broadcast подставит настройки.'
            : 'Нажмите «Мониторинг»');

    return Scaffold(
      appBar: AppBar(
        title: const Text('Broadcast сканера'),
        actions: [
          TextButton(
            onPressed: _toggle,
            child: Text(
              _listening ? 'Стоп' : 'Мониторинг',
              style: const TextStyle(color: WebixDarkTheme.accent),
            ),
          ),
        ],
      ),
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 12, 16, 8),
            child: Text(status, style: const TextStyle(color: WebixDarkTheme.muted, fontSize: 13)),
          ),
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 16),
            child: TextField(
              focusNode: _keys,
              controller: _keysCtrl,
              autofocus: true,
              textInputAction: TextInputAction.done,
              decoration: const InputDecoration(
                labelText: 'Поле для режима «клавиатура»',
              ),
              onSubmitted: _onKeyboard,
            ),
          ),
          const SizedBox(height: 8),
          Expanded(
            child: _events.isEmpty
                ? const Center(
                    child: Text(
                      'Пока пусто: broadcast, буфер и ввод появятся здесь',
                      style: TextStyle(color: WebixDarkTheme.text, fontSize: 16),
                      textAlign: TextAlign.center,
                    ),
                  )
                : ListView.separated(
                    itemCount: _events.length,
                    separatorBuilder: (_, _) => const Divider(height: 1, color: WebixDarkTheme.border),
                    itemBuilder: (context, index) {
                      final event = _events[index];
                      final extras = event.extras.entries
                          .map((entry) => '${entry.key}=${entry.value}')
                          .join('\n');
                      final title = event.action.isEmpty ? '(без action)' : event.action;
                      return ListTile(
                        title: Text(title, style: const TextStyle(color: WebixDarkTheme.text)),
                        subtitle: Text(
                          extras.isEmpty ? '(нет extra)' : extras,
                          style: const TextStyle(color: WebixDarkTheme.muted),
                        ),
                        onTap: () => _pick(event),
                      );
                    },
                  ),
          ),
        ],
      ),
    );
  }
}
