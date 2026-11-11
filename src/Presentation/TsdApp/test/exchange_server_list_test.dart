import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:tsd_app/domain/exchange/exchange_server.dart';
import 'package:tsd_app/presentation/settings/exchange_server_list_screen.dart';
import 'package:tsd_app/theme/webix_dark_theme.dart';

void main() {
  testWidgets('правка меняет наименование, нажатие строки возвращает id', (tester) async {
    final opened = await _open(tester);

    await tester.tap(find.byTooltip('Изменить'));
    await tester.pumpAndSettle();
    expect(find.text('Сервер обмена'), findsOneWidget);
    expect(find.text('Удалить'), findsOneWidget);

    await tester.enterText(_field('Наименование'), 'север');
    await tester.tap(find.text('Закрыть').hitTestable());
    await tester.pumpAndSettle();

    expect(find.text('север'), findsOneWidget);
    expect(opened.servers.single.name, 'север');
    expect(opened.servers.single.id, '1');

    await tester.tap(find.text('север'));
    await tester.pumpAndSettle();

    expect(opened.selected, '1');
  });

  testWidgets('удаление убирает строку', (tester) async {
    final opened = await _open(tester);

    await tester.tap(find.byTooltip('Изменить'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Удалить'));
    await tester.pumpAndSettle();

    expect(find.text('vostok'), findsNothing);
    expect(opened.servers, isEmpty);
  });

  testWidgets('пустое наименование не меняет строку', (tester) async {
    final opened = await _open(tester);

    await tester.tap(find.byTooltip('Изменить'));
    await tester.pumpAndSettle();
    await tester.enterText(_field('Наименование'), '');
    await tester.tap(find.text('Закрыть').hitTestable());
    await tester.pumpAndSettle();

    expect(find.text('vostok'), findsOneWidget);
    expect(opened.servers.single.name, 'vostok');
  });
}

Finder _field(String label) {
  return find.byWidgetPredicate((widget) => widget is TextField && widget.decoration?.labelText == label);
}

Future<_Opened> _open(WidgetTester tester) async {
  final opened = _Opened([
    const ExchangeServer(id: '1', name: 'vostok', host: '10.0.0.5', port: 80, publication: 'erp'),
  ]);
  await tester.pumpWidget(
    MaterialApp(
      theme: WebixDarkTheme.data(),
      home: Builder(
        builder: (context) {
          return Scaffold(
            body: TextButton(
              onPressed: () async {
                opened.selected = await Navigator.of(context).push<String>(
                  MaterialPageRoute(
                    builder: (_) => ExchangeServerListScreen(
                      servers: opened.servers,
                      onChanged: (servers) => opened.servers = servers,
                    ),
                  ),
                );
              },
              child: const Text('Открыть'),
            ),
          );
        },
      ),
    ),
  );
  await tester.tap(find.text('Открыть'));
  await tester.pumpAndSettle();
  return opened;
}

class _Opened {
  _Opened(this.servers);

  List<ExchangeServer> servers;
  String? selected;
}
