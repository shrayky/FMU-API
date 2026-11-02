import '../../domain/mark/mark_card_row.dart';
import '../../domain/mark/mark_check_result.dart';
import '../../domain/scan/mark_code.dart';

/// Состояние строки непрерывного сканирования.
enum ScanItemPhase { queued, running, ready, failed }

/// Строка таблицы непрерывного сканирования.
class ScanQueueItem {
  ScanQueueItem(this.mark, this.shortMark);

  final String mark;
  final String shortMark;
  ScanItemPhase phase = ScanItemPhase.queued;
  String name = '';
  MarkSignalTone status = MarkSignalTone.pending;
  MarkSignalTone owner = MarkSignalTone.pending;
  MarkSignalTone expiry = MarkSignalTone.pending;
  List<MarkCardRow> rows = const [];

  bool get canOpen => phase == ScanItemPhase.ready || phase == ScanItemPhase.failed;
}

/// Очередь непрерывного сканирования: без повторов и не больше трёх запросов сразу.
class ScanQueue {
  static const maxInFlight = 3;

  final List<ScanQueueItem> items = [];

  ScanQueueItem? add(String raw) {
    final mark = MarkCode.normalize(raw);
    if (mark.isEmpty || items.any((item) => item.mark == mark)) {
      return null;
    }

    final item = ScanQueueItem(mark, MarkCode.shortMark(mark));
    items.insert(0, item);
    return item;
  }

  List<ScanQueueItem> takeForRequest() {
    var slots = maxInFlight - items.where((item) => item.phase == ScanItemPhase.running).length;
    if (slots <= 0) {
      return const [];
    }

    final started = <ScanQueueItem>[];
    for (final item in items) {
      if (slots == 0) {
        break;
      }

      if (item.phase != ScanItemPhase.queued) {
        continue;
      }

      item.phase = ScanItemPhase.running;
      started.add(item);
      slots--;
    }

    return started;
  }

  void complete(ScanQueueItem item, MarkCheckResult result) {
    item
      ..phase = ScanItemPhase.ready
      ..name = result.name
      ..status = result.status
      ..owner = result.owner
      ..expiry = result.expiry
      ..rows = result.rows;
  }

  void fail(ScanQueueItem item, String message) {
    item
      ..phase = ScanItemPhase.failed
      ..name = message
      ..status = MarkSignalTone.bad
      ..owner = MarkSignalTone.bad
      ..expiry = MarkSignalTone.bad
      ..rows = [
        MarkCardRow(label: '', value: message, tone: MarkCardTone.error),
      ];
  }

  void clear() {
    items.clear();
  }
}
