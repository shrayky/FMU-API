import 'dart:async';

import 'package:flutter/material.dart';

import '../../domain/exchange/exchange_cancellation.dart';
import '../../theme/webix_dark_theme.dart';
import '../shell/screen_commands.dart';

/// Ожидание запроса к серверу обмена. Ответ возвращает результат, отмена — ничего.
class ExchangeProbeScreen<T> extends StatefulWidget {
  const ExchangeProbeScreen({
    super.key,
    required this.addressLine,
    required this.run,
    this.caption = 'Проверка связи с сервером',
    this.timeout,
  });

  final String addressLine;
  final String caption;

  /// Таймаут запроса: по нему идёт обратный отсчёт в окне ожидания.
  final Duration? timeout;

  final Future<T> Function(ExchangeCancellation cancellation) run;

  @override
  State<ExchangeProbeScreen<T>> createState() => _ExchangeProbeScreenState<T>();
}

class _ExchangeProbeScreenState<T> extends State<ExchangeProbeScreen<T>> {
  final _cancellation = ExchangeCancellation();
  var _left = 0;
  Timer? _ticker;
  var _leftAt = false;

  @override
  void initState() {
    super.initState();
    final timeout = widget.timeout;
    _left = timeout == null ? 0 : timeout.inSeconds;
    if (timeout != null && _left > 0) {
      _ticker = Timer.periodic(const Duration(seconds: 1), (timer) {
        if (!mounted || _left <= 1) {
          timer.cancel();
          if (mounted) {
            setState(() => _left = 0);
          }
          return;
        }
        setState(() => _left -= 1);
      });
    }
    _run();
  }

  @override
  void dispose() {
    _ticker?.cancel();
    super.dispose();
  }

  Future<void> _run() async {
    final result = await widget.run(_cancellation);
    if (!mounted || _leftAt) {
      return;
    }
    Navigator.of(context).pop(result);
  }

  void _cancel() {
    _leftAt = true;
    _ticker?.cancel();
    _cancellation.cancel();
    if (mounted) {
      Navigator.of(context).pop();
    }
  }

  @override
  Widget build(BuildContext context) {
    final timeout = widget.timeout;
    return Scaffold(
      appBar: AppBar(
        automaticallyImplyLeading: false,
        title: const Text('Ожидание...'),
      ),
      body: ColoredBox(
        color: WebixDarkTheme.background,
        child: SizedBox.expand(
          child: Column(
            children: [
              const SizedBox(height: 32),
              Text(widget.caption),
              const SizedBox(height: 8),
              Text(widget.addressLine),
              const SizedBox(height: 32),
              const SizedBox(
                width: 48,
                height: 48,
                child: CircularProgressIndicator(strokeWidth: 4),
              ),
              if (timeout != null) ...[
                const SizedBox(height: 16),
                Text(
                  'Осталось: $_left сек',
                  style: const TextStyle(color: WebixDarkTheme.muted),
                ),
              ],
            ],
          ),
        ),
      ),
      bottomNavigationBar: ScreenCommands(
        commands: [
          ScreenCommand(label: 'Отмена', onPressed: _cancel),
        ],
      ),
    );
  }
}
