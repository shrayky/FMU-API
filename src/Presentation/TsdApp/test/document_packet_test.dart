import 'package:flutter_test/flutter_test.dart';
import 'package:tsd_app/domain/documents/document_packet.dart';
import 'package:tsd_app/domain/documents/scanned_document.dart';

import 'documents_support.dart';

void main() {
  test('берёт первый документ и справочники пакета', () {
    final packet = parseDocumentPacket(sampleDocumentResponse);

    expect(packet, isNotNull);
    expect(packet!.document['УИД'], sampleErpUid);
    expect(packet.document['Представление'], samplePresentation);
    expect(packet.document['Тип'], 'СборМарок');
    expect(packet.nomenclature.single['УИД'], sampleGoodsUid);
    expect(packet.barcodes.single['Штрихкод'], sampleBarcode);
    expect(packet.barcodes.single['Номенклатура'], sampleGoodsUid);
    expect(packet.packSizes.single['ЕдиницВУпаковке'], 6);
  });

  test('флаги Результат не читаются: пустой массив Документы даёт отсутствие документа', () {
    expect(parseDocumentPacket(emptyDocumentsResponse), isNull);
    expect(parseDocumentPacket('{"Пакет": {"Данные": {}}}'), isNull);
    expect(parseDocumentPacket('{"Пакет": {}}'), isNull);
  });

  test('битое тело и не объект документа дают отсутствие документа', () {
    expect(parseDocumentPacket('{'), isNull);
    expect(parseDocumentPacket('[]'), isNull);
    expect(parseDocumentPacket('{"Пакет": {"Данные": {"Документы": ["строка"]}}}'), isNull);
  });

  test('Пакет строкой JSON разбирается как объект', () {
    const body = '{"Пакет": "{\\"Данные\\": {\\"Документы\\": [{\\"УИД\\": \\"erp\\"}]}}"}';

    final packet = parseDocumentPacket(body);

    expect(packet?.document['УИД'], 'erp');
    expect(packet?.nomenclature, isEmpty);
  });

  test('строки состава остаются в теле, Весовой не приводится к логическому типу', () {
    final packet = parseDocumentPacket(sampleDocumentResponse)!;
    final document = ScannedDocument(
      id: '1',
      scannedAt: DateTime(2026, 11, 5),
      body: packet.document,
    );

    expect(document.body['Весовой'], 'false');
    expect(document.composition.single['Номенклатура'], sampleGoodsUid);
    expect(document.composition.single['Количество'], 12);
    expect(document.composition.single['КоличествоМарок'], 1);
    expect(document.nomenclatureUids, [sampleGoodsUid]);
  });

  test('справочники массивом Ключ/Значение тоже читаются', () {
    const body = '''
    {"Пакет": {"Данные": {
      "Документы": [{"УИД": "erp", "Состав": []}],
      "Справочники": [
        {"Ключ": "Номенклатура", "Значение": [{"УИД": "uid-1", "Наименование": "Вода"}]},
        {"Ключ": "Штрихкоды", "Значение": [{"Штрихкод": "4600000000001", "Номенклатура": "uid-1"}]},
        {"Ключ": "РазмерыУпаковок", "Значение": [{"Номенклатура": "uid-1", "ЕдиницВУпаковке": 6}]}
      ]
    }}}
    ''';

    final packet = parseDocumentPacket(body);

    expect(packet, isNotNull);
    expect(packet!.nomenclature.single['УИД'], 'uid-1');
    expect(packet.barcodes.single['Штрихкод'], '4600000000001');
    expect(packet.packSizes.single['ЕдиницВУпаковке'], 6);
  });

  test('организация документа читается объектом и полями', () {
    final document = ScannedDocument(
      id: '1',
      scannedAt: DateTime(2026, 11, 5),
      body: const {
        'Организация': {'Название': 'ООО Ромашка', 'ИНН': '246412218294', 'КПП': '246401001'},
      },
    );
    final flat = ScannedDocument(
      id: '2',
      scannedAt: DateTime(2026, 11, 5),
      body: const {'Организация': 'ООО Ромашка', 'ИНН': '246412218294'},
    );

    expect(document.organization.name, 'ООО Ромашка');
    expect(document.organization.inn, '246412218294');
    expect(document.organization.kpp, '246401001');
    expect(flat.organization.name, 'ООО Ромашка');
    expect(flat.organization.inn, '246412218294');
    expect(flat.organization.kpp, isEmpty);
  });

  test('организация из пакета переезжает в документ', () {
    const body = '''
    {"Пакет": {"Данные": {
      "Документы": [{"УИД": "erp"}],
      "Организация": {"Название": "ООО Ромашка", "ИНН": "246412218294", "КПП": "246401001"}
    }}}
    ''';

    final packet = parseDocumentPacket(body)!;
    final organization = packet.document['Организация'] as Map;

    expect(organization['ИНН'], '246412218294');
    expect(organization['КПП'], '246401001');
  });

  test('справочник строкой JSON тоже читается', () {
    const body = '{"Пакет": {"Данные": {"Документы": [{"УИД": "erp"}], '
        '"Справочники": {"Штрихкоды": "[{\\"Штрихкод\\": \\"4600000000001\\", '
        '\\"Номенклатура\\": \\"uid-1\\"}]"}}}}';

    final packet = parseDocumentPacket(body);

    expect(packet?.barcodes.single['Штрихкод'], '4600000000001');
    expect(packet?.barcodes.single['Номенклатура'], 'uid-1');
  });

  test('состав берёт наименование из справочника, без карточки имя пустое', () {
    final document = ScannedDocument(
      id: '1',
      scannedAt: DateTime(2026, 11, 5),
      body: parseDocumentPacket(sampleDocumentResponse)!.document,
    );

    final withName = documentComposition(document, {sampleGoodsUid: sampleGoodsName}).single;
    final withoutName = documentComposition(document, const {}).single;

    expect(withName.name, sampleGoodsName);
    expect(withName.quantity, 12);
    expect(withName.marks, 1);
    expect(withoutName.name, isEmpty);
    expect(withoutName.quantity, 12);
  });
}
