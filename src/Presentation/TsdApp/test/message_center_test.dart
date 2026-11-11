import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:tsd_app/application/journal/work_log.dart';
import 'package:tsd_app/application/message/message_center.dart';
import 'package:tsd_app/application/message/message_effects.dart';
import 'package:tsd_app/domain/exchange/exchange_call_result.dart';
import 'package:tsd_app/domain/exchange/exchange_protocol.dart';
import 'package:tsd_app/domain/exchange/exchange_server.dart';
import 'package:tsd_app/domain/journal/work_log_entry.dart';
import 'package:tsd_app/domain/journal/work_log_storage.dart';
import 'package:tsd_app/domain/message/app_message.dart';

const _server = ExchangeServer(
  id: '1',
  host: '10.0.0.5',
  port: 80,
  timeoutSeconds: 10,
  protocol: defaultExchangeProtocol,
);

void main() {
  test('диагностика пишется в журнал без сообщения', () {
    final log = WorkLog(storage: MemoryWorkLogStorage());
    final center = MessageCenter(log: log, effects: RecordingEffects());

    center.trace('Скан марки', {'марка': '010460000000000121'});

    expect(log.entries.single.level, WorkLogLevel.info);
    expect(log.entries.single.text, 'Скан марки');
    expect(log.entries.single.data['марка'], '010460000000000121');
  });

  testWidgets('ошибка проверки пишется в журнал и показывается с иконкой', (tester) async {
    final log = WorkLog(storage: MemoryWorkLogStorage());
    final effects = RecordingEffects();
    final center = MessageCenter(log: log, effects: effects);
    const rawError = 'Сервер не ответил за 10 сек';
    const result = ExchangeCallResult(
      success: false,
      error: rawError,
      details: 'TimeoutException after 0:00:10.000000: Future not completed',
    );

    await _show(tester, (context) => center.show(context, center.exchangeProbe(_server, result)));

    expect(find.text('$exchangeProbeFailedPrefix$rawError'), findsOneWidget);
    expect(find.byIcon(Icons.error_outline), findsOneWidget);
    expect(effects.played, [MessageKind.failure]);
    expect(log.entries.single.level, WorkLogLevel.error);
    expect(log.entries.single.data['raw'], contains('TimeoutException'));
    expect(log.entries.single.data['endpoint'], '10.0.0.5:80/');
  });

  testWidgets('успех проверки показывается зелёной иконкой и пишется в журнал', (tester) async {
    final log = WorkLog(storage: MemoryWorkLogStorage());
    final center = MessageCenter(log: log, effects: RecordingEffects());

    await _show(
      tester,
      (context) => center.show(
        context,
        center.exchangeProbe(_server, const ExchangeCallResult(success: true, body: 'pong')),
      ),
    );

    expect(find.text(exchangeProbeSuccess), findsOneWidget);
    expect(find.byIcon(Icons.check_circle_outline), findsOneWidget);
    expect(log.entries.single.level, WorkLogLevel.info);
    expect(log.entries.single.data['endpoint'], '10.0.0.5:80/');
  });

  testWidgets('проверка без адреса идёт в журнал предупреждением', (tester) async {
    final log = WorkLog(storage: MemoryWorkLogStorage());
    final center = MessageCenter(log: log, effects: RecordingEffects());

    await _show(
      tester,
      (context) => center.show(
        context,
        center.exchangeBlocked(_server, const ExchangeCallResult(success: false, error: exchangeAddressMissing)),
      ),
    );

    expect(find.byIcon(Icons.warning_amber_rounded), findsOneWidget);
    expect(log.entries.single.level, WorkLogLevel.warning);
  });

  testWidgets('баннер ошибки пишется в журнал вместе с подробностью', (tester) async {
    final log = WorkLog(storage: MemoryWorkLogStorage());
    final center = MessageCenter(log: log, effects: RecordingEffects());

    await _show(
      tester,
      (context) async => center.showSnack(
        context,
        const AppMessage(
          kind: MessageKind.failure,
          text: 'Нет связи',
          data: {'error': 'SocketException'},
        ),
      ),
    );

    expect(find.text('Нет связи'), findsOneWidget);
    expect(find.byIcon(Icons.error_outline), findsOneWidget);
    expect(log.entries.single.level, WorkLogLevel.error);
    expect(log.entries.single.data['error'], 'SocketException');
  });
}

Future<void> _show(WidgetTester tester, Future<void> Function(BuildContext context) show) async {
  await tester.pumpWidget(
    MaterialApp(
      home: Builder(
        builder: (context) => Scaffold(
          body: Center(
            child: TextButton(
              onPressed: () => show(context),
              child: const Text('Показать'),
            ),
          ),
        ),
      ),
    ),
  );
  await tester.tap(find.text('Показать'));
  await tester.pumpAndSettle();
}

class RecordingEffects extends MessageEffects {
  final played = <MessageKind>[];

  @override
  void play(MessageKind kind) {
    played.add(kind);
  }
}

class MemoryWorkLogStorage implements WorkLogStorage {
  final entries = <WorkLogEntry>[];

  @override
  Future<List<WorkLogEntry>> recent({required int limit}) async {
    final sorted = [...entries]..sort((left, right) => right.at.compareTo(left.at));
    return sorted.take(limit).toList();
  }

  @override
  Future<void> add(WorkLogEntry entry) async {
    entries.add(entry);
  }

  @override
  Future<void> trim({required Duration age, required int limit}) async {}

  @override
  Future<void> clear() async {
    entries.clear();
  }
}
