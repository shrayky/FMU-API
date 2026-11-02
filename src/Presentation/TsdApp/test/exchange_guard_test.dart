import 'package:flutter_test/flutter_test.dart';
import 'package:tsd_app/domain/exchange/exchange_cancellation.dart';
import 'package:tsd_app/domain/exchange/exchange_protocol.dart';
import 'package:tsd_app/domain/exchange/exchange_server.dart';

void main() {
  test('имена протоколов и команда документа сбора', () {
    expect(defaultExchangeProtocol, 'DS T\u0421Д');
    expect(dataMobileProtocol, 'Data Mobile');
    expect(exchangeProtocolNames, [defaultExchangeProtocol, dataMobileProtocol]);
    expect(markCollectionDocumentCommand, 'СборМарокПоДокументу');
  });

  test('пустой адрес и порт 0 блокируют проверку одним текстом', () {
    final emptyHost = probeTransportBlocked(_server(host: '  '));
    final zeroPort = probeTransportBlocked(_server(port: 0));

    expect(emptyHost?.success, isFalse);
    expect(emptyHost?.error, exchangeAddressMissing);
    expect(zeroPort?.error, exchangeAddressMissing);
  });

  test('таймаут 0 блокирует проверку после адреса', () {
    final blocked = probeBlocked(_server(timeoutSeconds: 0));

    expect(blocked?.error, exchangeTimeoutMissing);
  });

  test('неизвестное имя блокирует проверку и в сеть его не закладывает домен', () {
    final blocked = probeBlocked(_server(protocol: 'Старый'));

    expect(blocked?.error, exchangeProtocolUnknown);
    expect(probeTransportBlocked(_server(protocol: 'Старый')), isNull);
  });

  test('готовые параметры проверку не блокируют', () {
    expect(probeBlocked(_server()), isNull);
    expect(probeBlocked(_server(protocol: dataMobileProtocol)), isNull);
  });

  test('отмена вызывает слушателя один раз и сразу, если уже отменено', () {
    final cancellation = ExchangeCancellation();
    var calls = 0;
    cancellation.onCancel(() => calls += 1);
    cancellation.cancel();
    cancellation.cancel();
    expect(calls, 1);

    cancellation.onCancel(() => calls += 1);
    expect(calls, 2);
    expect(cancellation.isCancelled, isTrue);
  });
}

ExchangeServer _server({
  String host = '10.0.0.5',
  int port = 80,
  int timeoutSeconds = 5,
  String protocol = defaultExchangeProtocol,
}) {
  return ExchangeServer(
    id: '1',
    host: host,
    port: port,
    timeoutSeconds: timeoutSeconds,
    protocol: protocol,
  );
}
