import 'dart:async';

import 'package:flutter/services.dart';
import 'package:flutter/widgets.dart';

import '../../domain/scan/scan_buffer.dart';
import '../../infrastructure/scan/clipboard_scan_watcher.dart';
import '../../infrastructure/scan/intent_scan_channel.dart';
import '../../infrastructure/settings/app_settings.dart';

/// Приём скана с клавиатуры, из интента и из буфера обмена.
///
/// Тип штрихкода модуль не проверяет: он отдаёт код экрану, а экран решает,
/// тот ли это тип и что с кодом делать.
class ScanSession {
  ScanSession({
    required this.settings,
    required this.onCode,
    ClipboardScanWatcher? clipboard,
    IntentScanChannel? intent,
  })  : _clipboard = clipboard ?? ClipboardScanWatcher(requireMark: false),
        _intent = intent ?? IntentScanChannel();

  final AppSettings settings;
  final ClipboardScanWatcher _clipboard;
  final IntentScanChannel _intent;
  final void Function(String code) onCode;
  final ScanBuffer _buffer = ScanBuffer();
  var _enabled = false;

  void start() {
    _enabled = true;
    _intent.onScan = (code) {
      _clipboard.remember(code);
      _emit(_buffer.completeRaw(code));
    };
    _clipboard.onCode = (code) => _emit(_buffer.completeRaw(code));
    _intent.start();
    _clipboard.start();
    unawaited(_configure());
  }

  void pause() {
    _enabled = false;
    _clipboard.stop();
  }

  void resume() {
    _enabled = true;
    final state = WidgetsBinding.instance.lifecycleState;
    if (state == null || state == AppLifecycleState.resumed) {
      _clipboard.start();
    }
  }

  void onLifecycle(AppLifecycleState state) {
    if (state == AppLifecycleState.resumed) {
      if (_enabled) {
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

  void dispose() {
    _enabled = false;
    _intent.stop();
    _clipboard.stop();
  }

  KeyEventResult onKey(FocusNode node, KeyEvent event) {
    if (event is! KeyDownEvent) {
      return KeyEventResult.ignored;
    }

    final control = HardwareKeyboard.instance.isControlPressed;
    if (control && event.logicalKey == LogicalKeyboardKey.keyV) {
      unawaited(_paste());
      return KeyEventResult.handled;
    }

    if (control &&
        (event.logicalKey == LogicalKeyboardKey.bracketRight || event.character == ']')) {
      _emit(_buffer.pushChar('\x1d'));
      return KeyEventResult.handled;
    }

    final isEnter = event.logicalKey == LogicalKeyboardKey.enter ||
        event.logicalKey == LogicalKeyboardKey.numpadEnter ||
        event.character == '\r' ||
        event.character == '\n';
    if (isEnter) {
      _emit(_buffer.pushChar('\r'));
      return KeyEventResult.handled;
    }

    final character = event.character;
    if (character != null && character.isNotEmpty) {
      _emit(_buffer.pushChar(character));
      return KeyEventResult.handled;
    }

    return KeyEventResult.ignored;
  }

  /// Готовый код мимо источника ввода: камера отдаёт его сюда, как и остальные сканеры.
  void submit(String code) {
    if (code.isEmpty) {
      return;
    }
    onCode(code);
  }

  Future<void> _configure() async {
    try {
      await _intent.configure(action: settings.intentAction, extra: settings.intentExtra);
    } catch (_) {}
  }

  Future<void> _paste() async {
    final data = await Clipboard.getData(Clipboard.kTextPlain);
    final text = data?.text ?? '';
    _clipboard.remember(text);
    _emit(_buffer.completeRaw(text));
    await Clipboard.setData(const ClipboardData(text: ''));
  }

  void _emit(ScanPushResult result) {
    final code = result.code;
    if (code == null) {
      return;
    }
    submit(code);
  }
}
