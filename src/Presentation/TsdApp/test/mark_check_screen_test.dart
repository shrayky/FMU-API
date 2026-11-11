import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:tsd_app/infrastructure/scan/clipboard_scan_watcher.dart';
import 'package:tsd_app/infrastructure/settings/app_settings.dart';
import 'package:tsd_app/main.dart';
import 'package:tsd_app/presentation/mark_check/mark_check_screen.dart';
import 'package:tsd_app/presentation/scan/continuous_scan_table.dart';

void main() {
  testWidgets('кнопка камеры видна если сканирование включено', (tester) async {
    await tester.pumpWidget(
      const TsdApp(
        home: MarkCheckScreen(settings: AppSettings(cameraScanEnabled: true)),
      ),
    );
    await tester.pump();

    expect(find.byIcon(Icons.qr_code_scanner), findsOneWidget);
  });

  testWidgets('в фоне не читает буфер обмена', (tester) async {
    var reads = 0;
    await tester.pumpWidget(
      TsdApp(
        home: MarkCheckScreen(
          settings: const AppSettings(),
          clipboard: ClipboardScanWatcher(
            interval: const Duration(milliseconds: 20),
            readClipboard: () async {
              reads++;
              return '';
            },
            clearClipboard: () async {},
          ),
        ),
      ),
    );
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 50));
    expect(reads, greaterThan(0));

    final beforePause = reads;
    tester.binding.handleAppLifecycleStateChanged(AppLifecycleState.paused);
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 80));
    expect(reads, beforePause);
  });

  testWidgets('не марка в режиме марки показывает предупреждение', (tester) async {
    await tester.pumpWidget(
      TsdApp(
        home: MarkCheckScreen(
          settings: const AppSettings(),
          clipboard: ClipboardScanWatcher(
            interval: const Duration(seconds: 5),
            requireMark: false,
            clearAfterEmit: false,
            readClipboard: () async => 'привет',
            clearClipboard: () async {},
          ),
        ),
      ),
    );
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 400));

    expect(find.text('Это не марка'), findsWidgets);
  });

  testWidgets('непрерывное сканирование показывает таблицу', (tester) async {
    await tester.pumpWidget(
      const TsdApp(
        home: MarkCheckScreen(settings: AppSettings(continuousScanEnabled: true)),
      ),
    );
    await tester.pump();

    expect(find.byType(ContinuousScanTable), findsOneWidget);
    expect(find.text('Статус, владелец, срок годности'), findsOneWidget);
    expect(find.byIcon(Icons.delete_outline), findsNothing);
  });
}
