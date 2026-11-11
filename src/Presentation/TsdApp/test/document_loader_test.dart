import 'package:flutter_test/flutter_test.dart';
import 'package:tsd_app/application/documents/document_loader.dart';
import 'package:tsd_app/domain/documents/document_packet.dart';
import 'package:tsd_app/domain/exchange/exchange_call_result.dart';
import 'package:tsd_app/domain/exchange/exchange_cancellation.dart';
import 'package:tsd_app/domain/exchange/exchange_protocol.dart';
import 'package:tsd_app/domain/exchange/exchange_server.dart';
import 'package:tsd_app/infrastructure/exchange/data_mobile_protocol.dart';

import 'documents_support.dart';

void main() {
  test('без выбранного сервера запрос не уходит в сеть', () async {
    final store = FakeDocumentStore();
    final protocol = DocumentProtocol((barcode) async => const ExchangeCallResult(success: true));

    final result = await DocumentLoader(store: store, protocol: (_) => protocol).load(
      settingsWithServer(withServer: false),
      sampleBarcode,
      ExchangeCancellation(),
    );

    expect(result.error, exchangeServerMissing);
    expect(result.blocked, isTrue);
    expect(result.document, isNull);
    expect(protocol.barcodes, isEmpty);
    expect(store.saved, isEmpty);
  });

  test('Data Mobile в сеть не идёт и возвращает бронь', () async {
    final store = FakeDocumentStore();
    final network = DocumentProtocol((barcode) async => const ExchangeCallResult(success: true));
    final loader = DocumentLoader(
      store: store,
      protocol: (name) => switch (name) {
        dataMobileProtocol => const DataMobileProtocol(),
        _ => network,
      },
    );

    final result = await loader.load(
      settingsWithServer(protocol: dataMobileProtocol),
      sampleBarcode,
      ExchangeCancellation(),
    );

    expect(result.error, dataMobileReserved);
    expect(result.blocked, isFalse);
    expect(network.barcodes, isEmpty);
    expect(store.saved, isEmpty);
  });

  test('пустой Документы не пишет в базу', () async {
    final store = FakeDocumentStore();

    final result = await DocumentLoader(
      store: store,
      protocol: (_) => DocumentProtocol((barcode) async => const ExchangeCallResult(success: true, body: emptyDocumentsResponse)),
    ).load(settingsWithServer(), sampleBarcode, ExchangeCancellation());

    expect(result.error, documentMissing);
    expect(result.document, isNull);
    expect(store.saved, isEmpty);
    expect(store.trims, isEmpty);
  });

  test('успешный пакет сохраняется и срезает старые документы', () async {
    final store = FakeDocumentStore();

    final result = await DocumentLoader(
      store: store,
      protocol: (_) => DocumentProtocol(
        (barcode) async => const ExchangeCallResult(success: true, body: sampleDocumentResponse),
      ),
    ).load(settingsWithServer(retentionDays: 30), sampleBarcode, ExchangeCancellation());

    expect(result.document, isNotNull);
    expect(result.error, isEmpty);
    expect(store.saved.single.document['УИД'], sampleErpUid);
    expect(store.trims, [30]);
  });

  test('сбой очистки срока хранения не отменяет сохранённый документ', () async {
    final store = FakeDocumentStore()..trimError = StateError('database is locked');

    final result = await DocumentLoader(
      store: store,
      protocol: (_) => DocumentProtocol(
        (barcode) async => const ExchangeCallResult(success: true, body: sampleDocumentResponse),
      ),
    ).load(settingsWithServer(retentionDays: 30), sampleBarcode, ExchangeCancellation());

    expect(result.document, isNotNull);
    expect(result.error, isEmpty);
    expect(store.saved, hasLength(1));
    expect(store.trims, [30]);
  });

  test('сбой протокола не пишет в базу и отдаёт подробности', () async {
    final store = FakeDocumentStore();

    final result = await DocumentLoader(
      store: store,
      protocol: (_) => DocumentProtocol(
        (barcode) async => const ExchangeCallResult(
          success: false,
          error: 'Сервер ответил кодом 503: нет',
          details: 'нет',
        ),
      ),
    ).load(settingsWithServer(), sampleBarcode, ExchangeCancellation());

    expect(result.error, 'Сервер ответил кодом 503: нет');
    expect(result.details, 'нет');
    expect(store.saved, isEmpty);
  });

  test('пустой адрес, порт 0 и таймаут 0 блокируют загрузку', () async {
    final protocol = DocumentProtocol((barcode) async => const ExchangeCallResult(success: true));

    for (final settings in [
      settingsWithServer().copyWith(exchangeServers: [
        const ExchangeServer(id: 'srv-1', host: '', port: 80, timeoutSeconds: 10),
      ]),
      settingsWithServer().copyWith(exchangeServers: [
        const ExchangeServer(id: 'srv-1', host: '10.0.0.5', port: 0, timeoutSeconds: 10),
      ]),
      settingsWithServer().copyWith(exchangeServers: [
        const ExchangeServer(id: 'srv-1', host: '10.0.0.5', port: 80, timeoutSeconds: 0),
      ]),
    ]) {
      final result = await DocumentLoader(store: FakeDocumentStore(), protocol: (_) => protocol)
          .load(settings, sampleBarcode, ExchangeCancellation());
      expect(result.blocked, isTrue);
    }

    expect(protocol.barcodes, isEmpty);
  });

  test('неизвестный протокол в сеть не уходит', () async {
    var lookups = 0;
    final result = await DocumentLoader(
      store: FakeDocumentStore(),
      protocol: (name) {
        lookups += 1;
        return null;
      },
    ).load(settingsWithServer(protocol: 'Старый'), sampleBarcode, ExchangeCancellation());

    expect(result.error, exchangeProtocolUnknown);
    expect(result.blocked, isTrue);
    expect(lookups, 0);
  });
}
