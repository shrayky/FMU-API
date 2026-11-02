import 'package:flutter/material.dart';

import '../../domain/exchange/exchange_call_result.dart';
import '../../domain/exchange/exchange_cancellation.dart';
import '../../theme/webix_dark_theme.dart';
import '../shell/screen_commands.dart';

/// Ожидание проверки связи. Ответ возвращает результат, отмена — ничего.
class ExchangeProbeScreen extends StatefulWidget {
  const ExchangeProbeScreen({
    super.key,
    required this.addressLine,
    required this.probe,
  });

  final String addressLine;
  final Future<ExchangeCallResult> Function(ExchangeCancellation cancellation) probe;

  @override
  State<ExchangeProbeScreen> createState() => _ExchangeProbeScreenState();
}

class _ExchangeProbeScreenState extends State<ExchangeProbeScreen> {
  final _cancellation = ExchangeCancellation();
  var _left = false;

  @override
  void initState() {
    super.initState();
    _run();
  }

  Future<void> _run() async {
    final result = await widget.probe(_cancellation);
    if (!mounted || _left) {
      return;
    }
    Navigator.of(context).pop(result);
  }

  void _cancel() {
    _left = true;
    _cancellation.cancel();
    if (mounted) {
      Navigator.of(context).pop();
    }
  }

  @override
  Widget build(BuildContext context) {
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
              const Text('Проверка связи с сервером'),
              const SizedBox(height: 8),
              Text(widget.addressLine),
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
