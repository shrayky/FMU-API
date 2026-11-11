import 'dart:async';

import 'package:flutter/material.dart';

import '../../application/journal/work_log.dart';
import '../../domain/journal/work_log_entry.dart';
import '../../theme/webix_dark_theme.dart';
import '../shell/screen_commands.dart';

/// Журнал работы: сообщения с уровнем, временем и подробностью для диагностики.
class WorkLogScreen extends StatefulWidget {
  const WorkLogScreen({super.key, this.log});

  final WorkLog? log;

  @override
  State<WorkLogScreen> createState() => _WorkLogScreenState();
}

class _WorkLogScreenState extends State<WorkLogScreen> {
  late final WorkLog _log;

  @override
  void initState() {
    super.initState();
    _log = widget.log ?? WorkLog.instance;
    _log.addListener(_onChanged);
    unawaited(_log.load());
  }

  @override
  void dispose() {
    _log.removeListener(_onChanged);
    super.dispose();
  }

  void _onChanged() {
    if (mounted) {
      setState(() {});
    }
  }

  @override
  Widget build(BuildContext context) {
    final entries = _log.entries;
    return Scaffold(
      appBar: AppBar(
        automaticallyImplyLeading: false,
        title: const Text('Логи работы'),
      ),
      body: ColoredBox(
        color: WebixDarkTheme.background,
        child: entries.isEmpty
            ? const Center(child: Text('Нет данных для отображения'))
            : ListView.separated(
                itemCount: entries.length,
                separatorBuilder: (_, _) => const Divider(height: 1, color: WebixDarkTheme.border),
                itemBuilder: (context, index) => _LogRow(entry: entries[index]),
              ),
      ),
      bottomNavigationBar: ScreenCommands(
        commands: [
          if (entries.isNotEmpty)
            ScreenCommand(label: 'Очистить', onPressed: () => unawaited(_log.clear())),
          ScreenCommand(label: 'Закрыть', onPressed: () => Navigator.of(context).pop()),
        ],
      ),
    );
  }
}

class _LogRow extends StatelessWidget {
  const _LogRow({required this.entry});

  final WorkLogEntry entry;

  @override
  Widget build(BuildContext context) {
    final details = _details(entry.data);
    return ListTile(
      dense: true,
      leading: Icon(_icon(entry.level), color: _color(entry.level), size: 20),
      title: Text(entry.text),
      subtitle: details.isEmpty
          ? null
          : Text(
              details,
              maxLines: 3,
              overflow: TextOverflow.ellipsis,
              style: const TextStyle(color: WebixDarkTheme.muted, fontSize: 12),
            ),
      trailing: Text(
        _time(entry.at),
        style: const TextStyle(color: WebixDarkTheme.muted, fontSize: 12),
      ),
    );
  }
}

String _details(Map<String, Object?> data) {
  final lines = <String>[];
  for (final item in data.entries) {
    final value = '${item.value}'.trim();
    if (value.isNotEmpty) {
      lines.add('${item.key}: $value');
    }
  }
  return lines.join('\n');
}

IconData _icon(WorkLogLevel level) {
  switch (level) {
    case WorkLogLevel.info:
      return Icons.info_outline;
    case WorkLogLevel.warning:
      return Icons.warning_amber_rounded;
    case WorkLogLevel.error:
      return Icons.error_outline;
  }
}

Color _color(WorkLogLevel level) {
  switch (level) {
    case WorkLogLevel.info:
      return WebixDarkTheme.accent;
    case WorkLogLevel.warning:
      return WebixDarkTheme.warning;
    case WorkLogLevel.error:
      return WebixDarkTheme.warn;
  }
}

String _time(DateTime at) {
  return '${_two(at.day)}.${_two(at.month)} ${_two(at.hour)}:${_two(at.minute)}:${_two(at.second)}';
}

String _two(int value) => value.toString().padLeft(2, '0');
