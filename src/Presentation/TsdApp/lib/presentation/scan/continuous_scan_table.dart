import 'package:flutter/material.dart';

import '../../application/mark/scan_queue.dart';
import '../../domain/mark/mark_check_result.dart';
import '../../theme/webix_dark_theme.dart';

/// Таблица непрерывного сканирования.
class ContinuousScanTable extends StatelessWidget {
  const ContinuousScanTable({
    super.key,
    required this.items,
    required this.placeholder,
    required this.onOpen,
  });

  final List<ScanQueueItem> items;
  final String placeholder;
  final ValueChanged<ScanQueueItem> onOpen;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        const _Header(),
        Expanded(
          child: items.isEmpty
              ? Center(
                  child: Text(
                    placeholder,
                    style: const TextStyle(color: WebixDarkTheme.text, fontSize: 16),
                    textAlign: TextAlign.center,
                  ),
                )
              : ListView.separated(
                  padding: const EdgeInsets.only(bottom: 88),
                  itemCount: items.length,
                  separatorBuilder: (_, _) => const Divider(height: 1, color: WebixDarkTheme.border),
                  itemBuilder: (context, index) => _Row(item: items[index], onOpen: onOpen),
                ),
        ),
      ],
    );
  }
}

/// Подпись индикаторов у правого края.
class _Header extends StatelessWidget {
  const _Header();

  @override
  Widget build(BuildContext context) {
    return const Padding(
      padding: EdgeInsets.fromLTRB(16, 10, 16, 8),
      child: Align(
        alignment: Alignment.centerRight,
        child: Text(
          'Статус, владелец, срок годности',
          textAlign: TextAlign.right,
          style: TextStyle(color: WebixDarkTheme.muted, fontSize: 12),
        ),
      ),
    );
  }
}

/// Строка: код марки пока идёт запрос, название после ответа.
class _Row extends StatelessWidget {
  const _Row({required this.item, required this.onOpen});

  final ScanQueueItem item;
  final ValueChanged<ScanQueueItem> onOpen;

  @override
  Widget build(BuildContext context) {
    final waiting = item.phase == ScanItemPhase.queued || item.phase == ScanItemPhase.running;
    final title = waiting || item.name.isEmpty ? item.shortMark : item.name;
    final titleColor = item.phase == ScanItemPhase.failed ? WebixDarkTheme.warn : WebixDarkTheme.text;

    return GestureDetector(
      behavior: HitTestBehavior.opaque,
      onTap: item.canOpen ? () => onOpen(item) : null,
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
        child: Row(
          children: [
            Expanded(
              child: Text(
                title,
                maxLines: 2,
                overflow: TextOverflow.ellipsis,
                style: TextStyle(color: titleColor, fontSize: 14, height: 1.3),
              ),
            ),
            const SizedBox(width: 8),
            SizedBox(
              width: 64,
              child: waiting
                  ? const Align(
                      alignment: Alignment.centerRight,
                      child: SizedBox(
                        width: 14,
                        height: 14,
                        child: CircularProgressIndicator(
                          strokeWidth: 2,
                          color: WebixDarkTheme.accent,
                        ),
                      ),
                    )
                  : Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        _Lamp(item.status),
                        _Lamp(item.owner),
                        _Lamp(item.expiry),
                      ],
                    ),
            ),
          ],
        ),
      ),
    );
  }
}

/// Один индикатор светофора.
class _Lamp extends StatelessWidget {
  const _Lamp(this.tone);

  final MarkSignalTone tone;

  @override
  Widget build(BuildContext context) {
    final color = switch (tone) {
      MarkSignalTone.pending => WebixDarkTheme.muted,
      MarkSignalTone.ok => WebixDarkTheme.ok,
      MarkSignalTone.bad => WebixDarkTheme.warn,
    };

    return Container(
      width: 12,
      height: 12,
      decoration: BoxDecoration(color: color, shape: BoxShape.circle),
    );
  }
}
