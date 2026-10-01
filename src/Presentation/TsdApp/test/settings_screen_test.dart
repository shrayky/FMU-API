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
  });
}
