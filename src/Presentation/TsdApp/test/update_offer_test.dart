import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:tsd_app/presentation/update/update_offer.dart';

void main() {
  testWidgets('предложение обновления показывает версию и зовёт установку', (tester) async {
    var pressed = 0;
    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: UpdateOffer(
            versionName: '1.2.0',
            busy: false,
            onUpdate: () => pressed++,
          ),
        ),
      ),
    );

    expect(find.text('Доступна версия 1.2.0'), findsOneWidget);
    await tester.tap(find.text('Обновить'));
    expect(pressed, 1);
  });

  testWidgets('во время установки кнопка недоступна', (tester) async {
    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: UpdateOffer(
            versionName: '1.2.0',
            busy: true,
            onUpdate: () {},
          ),
        ),
      ),
    );

    expect(find.text('Загрузка…'), findsOneWidget);
    expect(tester.widget<TextButton>(find.byType(TextButton)).onPressed, isNull);
  });
}
