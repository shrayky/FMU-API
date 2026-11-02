import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:tsd_app/presentation/update/update_offer.dart';

void main() {
  testWidgets('сбой проверки показывает строку на главном экране', (tester) async {
    await tester.pumpWidget(
      const MaterialApp(
        home: Scaffold(body: UpdateCheckFailure()),
      ),
    );

    expect(find.text('Не удалось проверить обновление'), findsOneWidget);
  });
}
