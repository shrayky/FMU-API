import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:tsd_app/infrastructure/settings/app_settings.dart';
import 'package:tsd_app/presentation/settings/settings_screen.dart';
import 'package:tsd_app/theme/webix_dark_theme.dart';

void main() {
  testWidgets('показывает версию приложения', (tester) async {
    await tester.pumpWidget(
      MaterialApp(
        theme: WebixDarkTheme.data(),
        home: const SettingsScreen(
          settings: AppSettings(),
          appVersion: '1.0.1 (2)',
        ),
      ),
    );

    expect(find.text('Версия 1.0.1 (2)'), findsOneWidget);
    expect(find.text('Непрерывное сканирование'), findsOneWidget);
  });

  testWidgets('сохраняет непрерывное сканирование', (tester) async {
    AppSettings? saved;
    await tester.pumpWidget(
      MaterialApp(
        theme: WebixDarkTheme.data(),
        home: Builder(
          builder: (context) => Scaffold(
            body: TextButton(
              onPressed: () async {
                saved = await Navigator.of(context).push<AppSettings>(
                  MaterialPageRoute(
                    builder: (_) => const SettingsScreen(
                      settings: AppSettings(),
                      appVersion: '1.0.1 (2)',
                    ),
                  ),
                );
              },
              child: const Text('Открыть'),
            ),
          ),
        ),
      ),
    );

    await tester.tap(find.text('Открыть'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Непрерывное сканирование'));
    await tester.pump();
    await tester.tap(find.text('Сохранить'));
    await tester.pumpAndSettle();

    expect(saved?.continuousScanEnabled, isTrue);
  });
}
