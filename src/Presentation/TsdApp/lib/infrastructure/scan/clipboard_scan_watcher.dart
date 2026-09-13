import 'dart:async';

import 'package:flutter/services.dart';

import '../../domain/scan/mark_code.dart';

/// Читает буфер обмена и отдаёт новую марку, затем очищает буфер.
class ClipboardScanWatcher {
  ClipboardScanWatcher({
    this.interval = const Duration(milliseconds: 350),
    this.requireMark = true,
    this.clearAfterEmit = true,
    Future<String> Function()? readClipboard,
    Future<void> Function()? clearClipboard,
  })  : _readClipboard = readClipboard ?? _defaultRead,
        _clearClipboard = clearClipboard ?? _defaultClear;

  final Duration interval;
  final bool requireMark;
  final bool clearAfterEmit;
  final Future<String> Function() _readClipboard;
  final Future<void> Function() _clearClipboard;
  void Function(String code)? onCode;

  Timer? _timer;
  String _last = '';
  bool _busy = false;

  void remember(String code) {
    _last = MarkCode.normalize(code);
  }

  void start() {
    stop();
    _timer = Timer.periodic(interval, (_) => poll());
    poll();
  }

  void stop() {
    _timer?.cancel();
    _timer = null;
  }

  void emit(String raw) {
    unawaited(_consume(raw));
  }

  Future<void> poll() async {
    if (_busy) {
      return;
    }

    _busy = true;
    try {
      await _consume(await _readClipboard());
    } catch (_) {
    } finally {
      _busy = false;
    }
  }

  Future<void> _consume(String raw) async {
    final code = MarkCode.normalize(raw);
    if (code.isEmpty || code == _last) {
      return;
    }

    if (requireMark && !MarkCode.looksLikeMark(code)) {
      return;
    }

    _last = code;
    onCode?.call(code);
    if (!clearAfterEmit) {
      return;
    }

    await _clearClipboard();
    _last = '';
  }

  static Future<String> _defaultRead() async {
    final data = await Clipboard.getData(Clipboard.kTextPlain);
    return data?.text ?? '';
  }

  static Future<void> _defaultClear() {
    return Clipboard.setData(const ClipboardData(text: ''));
  }
}
