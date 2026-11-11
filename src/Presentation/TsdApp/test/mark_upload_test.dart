import 'package:flutter_test/flutter_test.dart';
import 'package:tsd_app/application/mark/mark_uploader.dart';
import 'package:tsd_app/domain/documents/scanned_document.dart';
import 'package:tsd_app/domain/documents/stored_mark.dart';
import 'package:tsd_app/domain/documents/upload_packet.dart';
import 'package:tsd_app/domain/exchange/exchange_call_result.dart';
import 'package:tsd_app/domain/exchange/exchange_cancellation.dart';
import 'package:tsd_app/domain/exchange/exchange_protocol.dart';
import 'package:tsd_app/domain/exchange/exchange_server.dart';

import 'documents_support.dart';

/// Ответ выгрузки: пакет с успехом.
const uploadSuccessResponse = '{"Пакет": {"Результат": true}}';

/// Ответ выгрузки: пакет с ошибкой.
const uploadErrorResponse = '{"Пакет": {"Результат": false, "Ошибка": "нет товара"}}';

/// Ответ выгрузки: пакета нет.
const uploadEmptyResponse = '{}';

/// Ответ выгрузки: пакет без успеха и без текста ошибки.
const uploadSilentResponse = '{"Пакет": {"Результат": false}}';

void main() {
  test('пакет выгрузки собирает команду, склад, УИДИС и состав марок', () async {
    final document = _document();
    final packet = markUploadPacket(document, const [
      StoredMark(mark: '010460000000001521a', nomenclatureUid: 'uid-1', units: 1),
      StoredMark(mark: '0012345678905', nomenclatureUid: 'uid-1', units: 6),
    ]);

    expect(packet['Команда'], markUploadCommand);
    expect(packet['Склад'], markUploadWarehouse);

    final data = packet['Данные'] as Map<String, Object?>;
    expect(data['УИДИС'], sampleErpUid);
    expect(data['Тип'], markUploadCommand);
    expect(data['ТипДокументаErp'], '');

    final composition = data['Состав'] as List<Map<String, Object?>>;
    expect(composition, hasLength(2));
    expect(composition.first['Марка'], '010460000000001521a');
    expect(composition.first['Номенклатура'], 'uid-1');
    expect(composition.first['ГрупповойКод'], isFalse);
    expect(composition.first['КоличествоЕдиниц'], 1);
    expect(composition.last['ГрупповойКод'], isTrue);
    expect(composition.last['КоличествоЕдиниц'], 6);
  });

  test('Пакет.Результат истина ставит отметку выгрузки', () async {
    final store = FakeDocumentStore();
    final protocol = DocumentProtocol(
      (_) async => const ExchangeCallResult(success: false, error: ''),
      onUpload: (_) async => const ExchangeCallResult(success: true, body: uploadSuccessResponse),
    );

    final result = await _upload(store, protocol);

    expect(result.uploaded, isTrue);
    expect(store.uploads, [('doc-1', true)]);
    expect(protocol.packets, hasLength(1));
  });

  test('Пакет.Результат ложь отметку не ставит и отдаёт ошибку пакета', () async {
    final store = FakeDocumentStore();
    final protocol = DocumentProtocol(
      (_) async => const ExchangeCallResult(success: false, error: ''),
      onUpload: (_) async => const ExchangeCallResult(success: true, body: uploadErrorResponse),
    );

    final result = await _upload(store, protocol);

    expect(result.uploaded, isFalse);
    expect(result.error, '$uploadFailedPrefixнет товара');
    expect(store.uploads, isEmpty);
  });

  test('пустой пакет и пакет без ошибки дают свои тексты', () async {
    final store = FakeDocumentStore();

    final empty = await _upload(
      store,
      DocumentProtocol(
        (_) async => const ExchangeCallResult(success: false, error: ''),
        onUpload: (_) async => const ExchangeCallResult(success: true, body: uploadEmptyResponse),
      ),
    );
    final silent = await _upload(
      store,
      DocumentProtocol(
        (_) async => const ExchangeCallResult(success: false, error: ''),
        onUpload: (_) async => const ExchangeCallResult(success: true, body: uploadSilentResponse),
      ),
    );

    expect(empty.error, uploadNoPacket);
    expect(silent.error, uploadFailed);
    expect(store.uploads, isEmpty);
  });

  test('код ответа не 200 отдаёт текст протокола с началом неудачи', () async {
    final store = FakeDocumentStore();
    final protocol = DocumentProtocol(
      (_) async => const ExchangeCallResult(success: false, error: ''),
      onUpload: (_) async => const ExchangeCallResult(
        success: false,
        error: 'Сервер ответил кодом 503: нет',
      ),
    );

    final result = await _upload(store, protocol);

    expect(result.error, '$uploadFailedPrefixСервер ответил кодом 503: нет');
    expect(store.uploads, isEmpty);
  });

  test('сбой отметки после принятого пакета повторяет только отметку', () async {
    final store = FakeDocumentStore()..setUploadedFailures = 1;
    final protocol = DocumentProtocol(
      (_) async => const ExchangeCallResult(success: false, error: ''),
      onUpload: (_) async => const ExchangeCallResult(success: true, body: uploadSuccessResponse),
    );

    final result = await _upload(store, protocol);

    expect(result.uploaded, isTrue);
    expect(result.error, isEmpty);
    expect(protocol.packets, hasLength(1));
    expect(store.uploads, [('doc-1', true), ('doc-1', true)]);
  });

  test('отметка не записалась совсем — выгрузка не считается ошибкой', () async {
    final store = FakeDocumentStore()..setUploadedFailures = 99;
    final protocol = DocumentProtocol(
      (_) async => const ExchangeCallResult(success: false, error: ''),
      onUpload: (_) async => const ExchangeCallResult(success: true, body: uploadSuccessResponse),
    );

    final result = await _upload(store, protocol);

    expect(result.uploaded, isTrue);
    expect(protocol.packets, hasLength(1));
    expect(store.uploads, hasLength(3));
  });

  test('Data Mobile пакет не отправляет и отдаёт бронь', () async {
    final store = FakeDocumentStore();

    final result = await MarkUploader(store: store).upload(
      settingsWithServer(protocol: dataMobileProtocol),
      _document(),
      ExchangeCancellation(),
    );

    expect(result.uploaded, isFalse);
    expect(result.error, contains(dataMobileReserved));
    expect(store.uploads, isEmpty);
  });

  test('без выбранного сервера выгрузка в сеть не идёт', () async {
    final store = FakeDocumentStore();

    final result = await _upload(store, null, withServer: false);

    expect(result.blocked, isTrue);
    expect(result.error, exchangeServerMissing);
    expect(store.uploads, isEmpty);
  });
}

Future<MarkUploadResult> _upload(
  FakeDocumentStore store,
  DocumentProtocol? protocol, {
  String protocolName = defaultExchangeProtocol,
  bool withServer = true,
}) {
  return MarkUploader(
    store: store,
    protocol: (_) => protocol,
  ).upload(
    settingsWithServer(protocol: protocolName, withServer: withServer),
    _document(),
    ExchangeCancellation(),
  );
}

ScannedDocument _document() {
  return ScannedDocument(
    id: 'doc-1',
    scannedAt: DateTime(2026, 11, 5, 10, 30),
    number: '123',
    counterparty: 'ООО Ромашка',
    presentation: samplePresentation,
    erpUid: sampleErpUid,
    body: const {'УИД': sampleErpUid},
  );
}
