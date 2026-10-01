import 'package:flutter_test/flutter_test.dart';
import 'package:tsd_app/infrastructure/scan/clipboard_scan_watcher.dart';

void main() {
  const mark = '010460071404010521AB\x1d93abcd';

  test('после марки очищает буфер и позволяет тот же код снова', () async {
    var clipboard = mark;
    final emitted = <String>[];
    final watcher = ClipboardScanWatcher(
      readClipboard: () async => clipboard,
      clearClipboard: () async => clipboard = '',
    );
    watcher.onCode = emitted.add;

    await watcher.poll();
    expect(emitted, [mark]);
    expect(clipboard, isEmpty);

    clipboard = mark;
    await watcher.poll();
    expect(emitted, [mark, mark]);
  });

  test('в режиме монитора отдаёт любой новый текст и не чистит буфер', () async {
    var clipboard = 'привет';
    final emitted = <String>[];
    final watcher = ClipboardScanWatcher(
      requireMark: false,
      clearAfterEmit: false,
      readClipboard: () async => clipboard,
      clearClipboard: () async => clipboard = '',
    );
    watcher.onCode = emitted.add;

    await watcher.poll();
    expect(emitted, ['привет']);
    expect(clipboard, 'привет');
  });

  test('обычный текст не очищает буфер и не даёт код', () async {
    var clipboard = 'привет';
    final emitted = <String>[];
    final watcher = ClipboardScanWatcher(
      readClipboard: () async => clipboard,
      clearClipboard: () async => clipboard = '',
    );
    watcher.onCode = emitted.add;

    await watcher.poll();
    expect(emitted, isEmpty);
    expect(clipboard, 'привет');
  });

  test('stop отменяет периодический опрос буфера', () async {
    var reads = 0;
    final watcher = ClipboardScanWatcher(
      interval: const Duration(milliseconds: 20),
      readClipboard: () async {
        reads++;
        return '';
      },
      clearClipboard: () async {},
    );

    watcher.start();
    await Future<void>.delayed(const Duration(milliseconds: 50));
    final afterStart = reads;
    expect(afterStart, greaterThan(0));

    watcher.stop();
    await Future<void>.delayed(const Duration(milliseconds: 50));
    expect(reads, afterStart);
  });
}
