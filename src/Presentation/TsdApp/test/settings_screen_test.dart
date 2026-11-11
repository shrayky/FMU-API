import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:tsd_app/infrastructure/settings/app_settings.dart';
import 'package:tsd_app/presentation/settings/settings_pin_screen.dart';
import 'package:tsd_app/presentation/settings/settings_screen.dart';
import 'package:tsd_app/theme/webix_dark_theme.dart';

void main() {
  testWidgets('показывает заголовок настройки и камеру на оборудовании', (tester) async {
    await tester.pumpWidget(
      const MaterialApp(
        home: SettingsScreen(settings: AppSettings()),
      ),
    );

    expect(find.text('Настройка Fmu-Api'), findsOneWidget);
    expect(find.text('Сохранить'), findsNothing);
    expect(find.text('Сколько дней хранить документы'), findsOneWidget);
    await tester.drag(find.byType(ListView), const Offset(0, -400));
    await tester.pump();
    expect(find.text('Настройка проверки марки'), findsOneWidget);
    expect(find.text('Непрерывное сканирование'), findsOneWidget);
    await tester.tap(find.text('Оборудование'));
    await tester.pump();
    expect(find.text('Сканирование камерой мобильного устройства'), findsOneWidget);
  });

  testWidgets('в основных есть копия базы документов', (tester) async {
    SharedPreferences.setMockInitialValues({});
    await tester.pumpWidget(
      const MaterialApp(
        home: SettingsScreen(settings: AppSettings()),
      ),
    );

    await tester.scrollUntilVisible(
      find.text('Скопировать базу документов'),
      300,
      scrollable: find.byType(Scrollable).first,
    );

    expect(find.text('Скопировать базу документов'), findsOneWidget);
  });

  testWidgets('выход возвращает включённую камеру', (tester) async {
    SharedPreferences.setMockInitialValues({});
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
                    builder: (_) => const SettingsScreen(settings: AppSettings()),
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
    await tester.tap(find.text('Оборудование'));
    await tester.pump();
    await tester.tap(find.text('Сканирование камерой мобильного устройства'));
    await tester.pump();
    await tester.tap(find.text('Выход'));
    await tester.pumpAndSettle();

    expect(saved?.cameraScanEnabled, isTrue);
  });

  testWidgets('верный пин открывает вход, неверный остаётся на запросе', (tester) async {
    bool? allowed;
    await tester.pumpWidget(
      MaterialApp(
        home: Builder(
          builder: (context) => Scaffold(
            body: TextButton(
              onPressed: () async {
                allowed = await Navigator.of(context).push<bool>(
                  MaterialPageRoute(builder: (_) => const SettingsPinScreen(pin: '2468')),
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
    await tester.enterText(find.byType(TextField), '0000');
    await tester.tap(find.text('Ок'));
    await tester.pump();
    expect(find.text('Неверный пин код'), findsOneWidget);
    expect(allowed, isNull);

    await tester.enterText(find.byType(TextField), '2468');
    await tester.tap(find.text('Ок'));
    await tester.pumpAndSettle();
    expect(allowed, isTrue);
  });
}
