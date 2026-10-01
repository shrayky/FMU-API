import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:tsd_app/infrastructure/discovery/lan_fmu_scanner.dart';
import 'package:tsd_app/infrastructure/scan/clipboard_scan_watcher.dart';
import 'package:tsd_app/main.dart';
import 'package:tsd_app/presentation/home/home_screen.dart';

void main() {
  testWidgets('главный экран без адреса ищет Fmu-Api', (tester) async {
    SharedPreferences.setMockInitialValues({});
    await tester.pumpWidget(
      TsdApp(
        home: HomeScreen(
          lanScanner: LanFmuScanner(listAddresses: () async => []),
        ),
      ),
    );
    await tester.pump();
    await tester.idle();
    await tester.pump();
    expect(find.text('Укажите адрес Fmu-Api в настройках'), findsOneWidget);
    expect(find.text('Fmu-Api: Проверка марки'), findsOneWidget);
    expect(find.byIcon(Icons.qr_code_scanner), findsNothing);
  });

  testWidgets('кнопка камеры видна если сканирование включено', (tester) async {
    SharedPreferences.setMockInitialValues({'cameraScanEnabled': true});
    await tester.pumpWidget(
      TsdApp(
        home: HomeScreen(
          lanScanner: LanFmuScanner(listAddresses: () async => []),
        ),
      ),
    );
    await tester.pump();
    await tester.idle();
    await tester.pump();
    expect(find.byIcon(Icons.qr_code_scanner), findsOneWidget);
  });

  testWidgets('в фоне не читает буфер обмена', (tester) async {
    var reads = 0;
    SharedPreferences.setMockInitialValues({});
    await tester.pumpWidget(
      TsdApp(
        home: HomeScreen(
          lanScanner: LanFmuScanner(listAddresses: () async => []),
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
}
