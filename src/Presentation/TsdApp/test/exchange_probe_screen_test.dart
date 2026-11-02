import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:tsd_app/domain/exchange/exchange_call_result.dart';
import 'package:tsd_app/domain/exchange/exchange_cancellation.dart';
import 'package:tsd_app/domain/exchange/exchange_protocol.dart';
import 'package:tsd_app/domain/exchange/exchange_server.dart';
import 'package:tsd_app/presentation/settings/exchange_server_form_screen.dart';
import 'package:tsd_app/theme/webix_dark_theme.dart';

void main() {
  testWidgets('пустой адрес показывает ошибку без экрана ожидания', (tester) async {
    await _open(tester);

    await tester.tap(find.text('Проверить'));
    await tester.pumpAndSettle();

    expect(find.text('Ожидание...'), findsNothing);
    expect(find.text('$exchangeProbeFailedPrefix$exchangeAddressMissing'), findsOneWidget);
  });

  testWidgets('таймаут 0 показывает ошибку без экрана ожидания', (tester) async {
    await _open(tester);
    await _fill(tester, timeout: '0');

    await tester.tap(find.text('Проверить'));
    await tester.pumpAndSettle();

    expect(find.text('Ожидание...'), findsNothing);
    expect(find.text('$exchangeProbeFailedPrefix$exchangeTimeoutMissing'), findsOneWidget);
  });

  testWidgets('неизвестный протокол в сеть не уходит', (tester) async {
    var lookups = 0;
    await _open(
      tester,
      initialProtocol: 'Старый',
      protocols: (name) {
        lookups += 1;
        return ScriptedProtocol((server, cancellation) async => const ExchangeCallResult(success: true));
      },
    );
    await _fill(tester);

    await tester.tap(find.text('Проверить'));
    await tester.pumpAndSettle();

    expect(find.text('Ожидание...'), findsNothing);
    expect(find.text('$exchangeProbeFailedPrefix$exchangeProtocolUnknown'), findsOneWidget);
    expect(lookups, 0);
  });

  testWidgets('успех показывает адрес и диалог', (tester) async {
    final gate = Completer<ExchangeCallResult>();
    await _open(
      tester,
      protocols: (name) => ScriptedProtocol((server, cancellation) => gate.future),
    );
    await _fill(tester, publication: 'erp');

    await tester.tap(find.text('Проверить'));
    await tester.pumpAndSettle();
    expect(find.text('10.0.0.5:80/erp'), findsOneWidget);

    gate.complete(const ExchangeCallResult(success: true, body: 'pong'));
    await tester.pumpAndSettle();
    expect(find.text(exchangeProbeSuccess), findsOneWidget);
    expect(find.text('Ок'), findsOneWidget);
  });

  testWidgets('ошибка проверки показывается диалогом карточки', (tester) async {
    await _open(
      tester,
      protocols: (name) => ScriptedProtocol(
        (server, cancellation) async => const ExchangeCallResult(success: false, error: 'нет связи'),
      ),
    );
    await _fill(tester);

    await tester.tap(find.text('Проверить'));
    await tester.pumpAndSettle();
    expect(find.text('${exchangeProbeFailedPrefix}нет связи'), findsOneWidget);
  });

  testWidgets('отмена закрывает ожидание без сообщения', (tester) async {
    final gate = Completer<ExchangeCallResult>();
    await _open(
      tester,
      protocols: (name) => ScriptedProtocol((server, cancellation) {
        cancellation.onCancel(() {
          if (!gate.isCompleted) {
            gate.complete(const ExchangeCallResult(success: false, error: 'closed'));
          }
        });
        return gate.future;
      }),
    );
    await _fill(tester);

    await tester.tap(find.text('Проверить'));
    await tester.pumpAndSettle();
    expect(find.text('Ожидание...'), findsOneWidget);

    await tester.tap(find.text('Отмена'));
    await tester.pumpAndSettle();

    expect(find.text('Ожидание...'), findsNothing);
    expect(find.textContaining('Проверка связи'), findsNothing);
    expect(find.text('Сервер обмена (создание)'), findsOneWidget);
  });

  testWidgets('в списке оба протокола, Data Mobile возвращает бронь', (tester) async {
    await _open(tester);
    final dropdown = find.byType(DropdownButtonFormField<String>);
    await tester.ensureVisible(dropdown);
    await tester.pumpAndSettle();
    await tester.tap(dropdown);
    await tester.pumpAndSettle();

    expect(find.text(dataMobileProtocol).hitTestable(), findsOneWidget);
    await tester.tap(find.text(dataMobileProtocol).hitTestable());
    await tester.pumpAndSettle();
    await _fill(tester);

    await tester.tap(find.text('Проверить'));
    await tester.pumpAndSettle();

    expect(find.text('$exchangeProbeFailedPrefix$dataMobileReserved'), findsOneWidget);
  });
}

Future<void> _open(
  WidgetTester tester, {
  String initialProtocol = defaultExchangeProtocol,
  ExchangeProtocol? Function(String name)? protocols,
}) async {
  await tester.pumpWidget(
    MaterialApp(
      theme: WebixDarkTheme.data(),
      home: ExchangeServerFormScreen(initialProtocol: initialProtocol, protocols: protocols),
    ),
  );
}

Future<void> _fill(WidgetTester tester, {String timeout = '5', String publication = ''}) async {
  await tester.enterText(_field('IP-адрес'), '10.0.0.5');
  await tester.enterText(_field('Порт'), '80');
  if (publication.isNotEmpty) {
    await tester.enterText(_field('Имя публикации базы'), publication);
  }
  final timeoutField = _field('Таймаут запроса (сек)');
  await tester.ensureVisible(timeoutField);
  await tester.enterText(timeoutField, timeout);
  await tester.pump();
}

Finder _field(String label) {
  return find.byWidgetPredicate((widget) => widget is TextField && (widget.decoration?.labelText == label));
}

class ScriptedProtocol implements ExchangeProtocol {
  ScriptedProtocol(this.onProbe);

  final Future<ExchangeCallResult> Function(ExchangeServer server, ExchangeCancellation cancellation) onProbe;

  @override
  Future<ExchangeCallResult> probe(ExchangeServer server, ExchangeCancellation cancellation) {
    return onProbe(server, cancellation);
  }

  @override
  Future<ExchangeCallResult> documentByBarcode(
    ExchangeServer server,
    ExchangeCancellation cancellation, {
    required String command,
    required String barcode,
  }) async {
    return const ExchangeCallResult(success: false, error: exchangeCommandPending);
  }

  @override
  Future<ExchangeCallResult> markStatus(
    ExchangeServer server,
    ExchangeCancellation cancellation, {
    required String mark,
  }) async {
    return const ExchangeCallResult(success: false, error: exchangeCommandPending);
  }

  @override
  Future<ExchangeCallResult> trueApiToken(ExchangeServer server, ExchangeCancellation cancellation) async {
    return const ExchangeCallResult(success: false, error: exchangeCommandPending);
  }

  @override
  Future<ExchangeCallResult> loadGoods(ExchangeServer server, ExchangeCancellation cancellation) async {
    return const ExchangeCallResult(success: false, error: exchangeCommandPending);
  }

  @override
  Future<ExchangeCallResult> uploadDocument(
    ExchangeServer server,
    ExchangeCancellation cancellation, {
    required Map<String, Object?> packet,
  }) async {
    return const ExchangeCallResult(success: false, error: exchangeCommandPending);
  }

  @override
  Future<ExchangeCallResult> setPrice(
    ExchangeServer server,
    ExchangeCancellation cancellation, {
    required String nomenclature,
    required num price,
  }) async {
    return const ExchangeCallResult(success: false, error: exchangeCommandPending);
  }
}
