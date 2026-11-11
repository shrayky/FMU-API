import 'package:flutter_test/flutter_test.dart';
import 'package:tsd_app/application/mark/mark_pick.dart';
import 'package:tsd_app/domain/documents/scanned_document.dart';
import 'package:tsd_app/domain/documents/stored_mark.dart';
import 'package:tsd_app/domain/mark/mark_online.dart';
import 'package:tsd_app/infrastructure/settings/app_settings.dart';

import 'documents_support.dart';

/// GTIN товара `sampleBarcode`: четырнадцать цифр с ведущим нулём.
const waterGtin = '04600000000001';

/// Марка товара `sampleBarcode`; длина не 29, поэтому штрихкод берётся из кода.
const waterMark = '01${waterGtin}21ABC123';

/// Вторая марка того же товара: серия другая, товар тот же.
const waterMarkOther = '01${waterGtin}21ZZZ999';

/// Марка второго товара документа.
const lemonadeMark = '010460703110108721AAAA';

void main() {
  test('марка с 01 без открытой позиции открывает товар и пишет строку marks', () async {
    final store = FakeDocumentStore()
      ..barcodeToUid = {sampleBarcode: 'uid-1'}
      ..names = {'uid-1': sampleGoodsName};
    final pick = await _pick(store);

    final result = await pick.scan(waterMark);

    expect(result.changed, isTrue);
    expect(result.messages, isEmpty);
    expect(pick.productOpen, isTrue);
    expect(pick.selected, 0);
    final marks = await store.marks('doc-1');
    expect(marks, hasLength(1));
    expect(marks.single.nomenclatureUid, 'uid-1');
    expect(pick.lines.single.picked, 1);
  });

  test('упаковка 00 без открытой позиции не пишет марку и просит штрихкод товара', () async {
    final store = FakeDocumentStore();
    final pick = await _pick(store);

    final result = await pick.scan('00123456789012345678');

    expect(result.changed, isFalse);
    expect(result.messages, [markNeedsBarcode]);
    expect(store.marksByDocument, isEmpty);
    expect(pick.productOpen, isFalse);
  });

  test('повтор марки в документе не пишется', () async {
    final store = FakeDocumentStore()..barcodeToUid = {sampleBarcode: 'uid-1'};
    final pick = await _pick(store);
    await pick.scan(waterMark);

    final again = await pick.scan(waterMark);

    expect(again.changed, isFalse);
    expect(again.messages, [markAlreadyScanned]);
    expect(await store.marks('doc-1'), hasLength(1));
  });

  test('марка другого товара переключает позицию на его строку', () async {
    final store = FakeDocumentStore()
      ..barcodeToUid = {sampleBarcode: 'uid-1', '4607031101087': 'uid-2'};
    final pick = await _pick(
      store,
      lines: [
        ('uid-1', 'Вода негазированная', 12),
        ('uid-2', 'Лимонад', 6),
      ],
    );
    await pick.scan(waterMark);
    expect(pick.selected, 0);

    final other = await pick.scan(lemonadeMark);

    expect(other.changed, isTrue);
    expect(other.messages, isEmpty);
    expect(pick.selected, 1);
    expect(pick.lines[1].picked, 1);
    final marks = await store.marks('doc-1');
    expect(marks, hasLength(2));
    expect(marks.last.nomenclatureUid, 'uid-2');
  });

  test('марка с незнакомым штрихкодом при открытой позиции не пишется', () async {
    final store = FakeDocumentStore()..barcodeToUid = {sampleBarcode: 'uid-1'};
    final pick = await _pick(store);
    await pick.scan(waterMark);

    final other = await pick.scan(lemonadeMark);

    expect(other.changed, isFalse);
    expect(other.messages, [markForOtherProduct(name: sampleGoodsName, barcode: '4607031101087')]);
    expect(await store.marks('doc-1'), hasLength(1));
  });

  test('товар по штрихкоду марки не из состава документа не пишется', () async {
    final store = FakeDocumentStore()
      ..barcodeToUid = {'4607031101087': 'uid-2'}
      ..names = {'uid-2': 'Лимонад'};
    final pick = await _pick(store);

    final result = await pick.scan(lemonadeMark);

    expect(result.changed, isFalse);
    expect(result.messages, [markNotInComposition(name: 'Лимонад', barcode: '4607031101087')]);
    expect(store.marksByDocument, isEmpty);
  });

  test('товар находится и по полному GTIN из марки', () async {
    final store = FakeDocumentStore()..barcodeToUid = {'04600000000001': 'uid-1'};
    final pick = await _pick(store);

    final result = await pick.scan(waterMark);

    expect(result.changed, isTrue);
    expect(pick.productOpen, isTrue);
  });

  test('индивидуальная марка в режиме «Упаковки» не пишется', () async {
    final store = FakeDocumentStore()..barcodeToUid = {sampleBarcode: 'uid-1'};
    final pick = await _pick(store, settings: const AppSettings(registerPackageBarcode: true));

    final result = await pick.scan(waterMark);

    expect(pick.modeTitle, packagesMode);
    expect(result.changed, isFalse);
    expect(result.messages, [markIndividualForbidden]);
    expect(store.marksByDocument, isEmpty);

    pick.packages = false;
    expect(pick.modeTitle, unitsAndPackagesMode);
  });

  test('превышение Количество не пишется', () async {
    final store = FakeDocumentStore()..barcodeToUid = {sampleBarcode: 'uid-1'};
    final pick = await _pick(store, lines: [('uid-1', sampleGoodsName, 1)]);
    await pick.scan(waterMark);

    final overflow = await pick.scan('01${waterGtin}21ZZZ999');

    expect(overflow.changed, isFalse);
    expect(overflow.messages, [markOverflow]);
    expect(await store.marks('doc-1'), hasLength(1));
  });

  test('последняя марка документа закрывает все строки', () async {
    final store = FakeDocumentStore()..barcodeToUid = {sampleBarcode: 'uid-1'};
    final pick = await _pick(store, lines: [('uid-1', sampleGoodsName, 2)]);

    final first = await pick.scan(waterMark);
    final second = await pick.scan('01${waterGtin}21ZZZ999');

    expect(first.allPicked, isFalse);
    expect(second.allPicked, isTrue);
    expect(second.changed, isTrue);
    expect(pick.productOpen, isFalse);
    expect(await store.marks('doc-1'), hasLength(2));
  });

  test('выгруженный документ марку не пишет и не удаляет', () async {
    final store = FakeDocumentStore()
      ..barcodeToUid = {sampleBarcode: 'uid-1'}
      ..marksByDocument['doc-1'] = [
        const StoredMark(mark: waterMark, nomenclatureUid: 'uid-1'),
      ];
    final pick = await _pick(store, uploaded: true);

    final scanned = await pick.scan('01${waterGtin}21ZZZ999');
    final deleted = await pick.delete(waterMark);

    expect(scanned.messages, [markDocumentUploaded]);
    expect(deleted.changed, isFalse);
    expect(deleted.messages, [markDocumentUploaded]);
    expect(await store.marks('doc-1'), hasLength(1));
  });

  test('удаление марки находит её в документе и закрывает позицию', () async {
    final store = FakeDocumentStore()
      ..barcodeToUid = {sampleBarcode: 'uid-1'}
      ..marksByDocument['doc-1'] = [
        const StoredMark(mark: waterMark, nomenclatureUid: 'uid-1', units: 2),
      ];
    final pick = await _pick(store);
    pick.productOpen = true;
    expect(pick.lines.single.picked, 2);

    final result = await pick.delete(waterMark);

    expect(result.changed, isTrue);
    expect(pick.productOpen, isFalse);
    expect(pick.lines.single.picked, 0);
    expect(await store.marks('doc-1'), isEmpty);
  });

  test('чужой марки в документе нет — удалять нечего', () async {
    final store = FakeDocumentStore();
    final pick = await _pick(store);

    final result = await pick.delete(waterMark);

    expect(result.changed, isFalse);
    expect(result.messages, [markNotInDocument]);
  });

  test('проверка владельца действует, пока документ её не отключил', () async {
    final store = FakeDocumentStore()..barcodeToUid = {sampleBarcode: 'uid-1'};
    final online = FakeMarkOnline()..ownerAnswer = const MarkCheckAnswer.rejected(markOwnerForeign);
    final pick = await _pick(
      store,
      settings: const AppSettings(checkMarkOwner: true),
      online: online,
    );

    final result = await pick.scan(waterMark);

    expect(result.messages, [markOwnerForeign]);
    expect(online.owners, [waterMark]);
    expect(store.marksByDocument, isEmpty);
  });

  test('БезПроверкиВладельца отключает проверку владельца документа', () async {
    final store = FakeDocumentStore()..barcodeToUid = {sampleBarcode: 'uid-1'};
    final online = FakeMarkOnline();
    final pick = await _pick(
      store,
      settings: const AppSettings(checkMarkOwner: true),
      online: online,
      withoutOwnerCheck: true,
    );

    final result = await pick.scan(waterMark);

    expect(result.changed, isTrue);
    expect(online.owners, isEmpty);
  });

  test('статус подбора проверяется и отказ не пишет марку', () async {
    final store = FakeDocumentStore()..barcodeToUid = {sampleBarcode: 'uid-1'};
    final online = FakeMarkOnline()..statusAnswer = const MarkStatusAnswer.rejected('Марка не найдена');
    final pick = await _pick(
      store,
      settings: const AppSettings(requestSelectionStatus: true),
      online: online,
    );

    final result = await pick.scan(waterMark);

    expect(result.messages, ['Марка не найдена']);
    expect(online.statuses, [waterMark]);
    expect(store.marksByDocument, isEmpty);
  });

  test('упаковка True API проходит разбор ещё раз без проверки владельца', () async {
    final store = FakeDocumentStore()
      ..barcodeToUid = {sampleBarcode: 'uid-1'}
      ..packSizes[FakeDocumentStore.sizeKey('uid-1', pallet: false)] = 6;
    final online = FakeMarkOnline()
      ..packageByMark[waterMark] = const MarkPackageAnswer(package: '0012345678905');
    final pick = await _pick(
      store,
      settings: const AppSettings(
        registerPackageBarcode: true,
        requestPackageViaTrueApi: true,
        checkMarkOwner: true,
      ),
      online: online,
    );

    final result = await pick.scan(waterMark);

    expect(result.changed, isTrue);
    expect(online.tokens, 1);
    expect(online.owners, hasLength(1));
    final marks = await store.marks('doc-1');
    expect(marks, hasLength(1));
    expect(marks.single.mark, '0012345678905');
    expect(marks.single.units, 6);
  });

  test('токен упаковки запрашивается по ИНН организации документа', () async {
    final store = FakeDocumentStore()..barcodeToUid = {sampleBarcode: 'uid-1'};
    final online = FakeMarkOnline();
    final pick = await _pick(
      store,
      settings: const AppSettings(
        registerPackageBarcode: true,
        requestPackageViaTrueApi: true,
      ),
      online: online,
      organization: const {'Название': 'ООО Ромашка', 'ИНН': '246412218294', 'КПП': '246401001'},
    );

    await pick.scan(waterMark);

    expect(online.tokenInns, ['246412218294']);
  });

  test('без организации документа токен берётся общий', () async {
    final store = FakeDocumentStore()..barcodeToUid = {sampleBarcode: 'uid-1'};
    final online = FakeMarkOnline();
    final pick = await _pick(
      store,
      settings: const AppSettings(
        registerPackageBarcode: true,
        requestPackageViaTrueApi: true,
      ),
      online: online,
    );

    await pick.scan(waterMark);

    expect(online.tokenInns, ['']);
  });

  test('просроченный токен не пускает упаковку True API', () async {
    final store = FakeDocumentStore()..barcodeToUid = {sampleBarcode: 'uid-1'};
    final online = FakeMarkOnline()
      ..tokenAnswer = const TrueApiTokenAnswer(token: '', error: markTokenExpired);
    final pick = await _pick(
      store,
      settings: const AppSettings(
        registerPackageBarcode: true,
        requestPackageViaTrueApi: true,
      ),
      online: online,
    );

    final result = await pick.scan(waterMark);

    expect(result.messages, [markTokenExpired]);
    expect(online.statuses, isEmpty);
    expect(store.marksByDocument, isEmpty);
  });

  test('символ GS в марке записывается текстом', () async {
    final store = FakeDocumentStore()..barcodeToUid = {sampleBarcode: 'uid-1'};
    final pick = await _pick(store);

    await pick.scan('01${waterGtin}21AB\x1d93xyz');

    final marks = await store.marks('doc-1');
    expect(marks.single.mark, r'010460000000000121AB\u001d93xyz');
  });

  test('палета берёт единицы из строки с Паллета истина', () async {
    final store = FakeDocumentStore()
      ..barcodeToUid = {sampleBarcode: 'uid-1'}
      ..packSizes[FakeDocumentStore.sizeKey('uid-1', pallet: true)] = 120;
    final pick = await _pick(store, lines: [('uid-1', sampleGoodsName, 120)]);
    pick.productOpen = true;

    final result = await pick.scan('00046000000000152');

    expect(result.changed, isTrue);
    expect((await store.marks('doc-1')).single.units, 120);
  });

  test('диагностика скана пишется в журнал', () async {
    final store = FakeDocumentStore()..barcodeToUid = {sampleBarcode: 'uid-1'};
    final traced = <String, Map<String, Object?>>{};
    final pick = await _pick(store, trace: (text, data) => traced[text] = data);

    await pick.scan(waterMark);

    expect(traced['Скан марки']?['штрихкоды'], '$sampleBarcode, 04600000000001');
    expect(traced['Поиск товара по штрихкоду']?['товар'], 'uid-1');
    expect(traced['Позиция открыта']?['товар'], sampleGoodsName);
  });

  test('УИД товара сравнивается без учёта регистра', () async {
    final store = FakeDocumentStore()..barcodeToUid = {sampleBarcode: 'UID-1'};
    final pick = await _pick(store);

    final result = await pick.scan(waterMark);

    expect(result.changed, isTrue);
    expect(pick.productOpen, isTrue);
    expect((await store.marks('doc-1')).single.nomenclatureUid, 'uid-1');
  });

  test('имя API True api сравнивается без учёта регистра', () async {
    final store = FakeDocumentStore()
      ..barcodeToUid = {sampleBarcode: 'uid-1'}
      ..packSizes[FakeDocumentStore.sizeKey('uid-1', pallet: false)] = 6;
    final online = FakeMarkOnline()
      ..packageByMark[waterMark] = const MarkPackageAnswer(package: '0012345678905');
    final pick = await _pick(
      store,
      settings: const AppSettings(
        registerPackageBarcode: true,
        requestPackageViaTrueApi: true,
        markCheckApi: 'true API',
      ),
      online: online,
    );

    final result = await pick.scan(waterMark);

    expect(result.changed, isTrue);
    expect((await store.marks('doc-1')).single.mark, '0012345678905');
  });

  test('режим позиции берётся из настройки регистрации упаковок', () async {
    final store = FakeDocumentStore();
    final on = await _pick(store, settings: const AppSettings(registerPackageBarcode: true));
    final off = await _pick(store, settings: const AppSettings());

    expect(on.modeTitle, packagesMode);
    expect(off.modeTitle, unitsAndPackagesMode);
  });

  test('окно ожидания нужно только при включённых сетевых проверках', () async {
    final store = FakeDocumentStore();

    expect((await _pick(store)).needsServer, isFalse);
    expect((await _pick(store, settings: const AppSettings(checkMarkOwner: true))).needsServer, isTrue);
    expect(
      (await _pick(store, settings: const AppSettings(requestSelectionStatus: true))).needsServer,
      isTrue,
    );
    expect(
      (await _pick(
        store,
        settings: const AppSettings(
          registerPackageBarcode: true,
          requestPackageViaTrueApi: true,
        ),
      ))
          .needsServer,
      isTrue,
    );
  });

  test('марки упаковки из истории агрегации добавляются все', () async {
    final store = FakeDocumentStore()..barcodeToUid = {sampleBarcode: 'uid-1'};
    final online = FakeMarkOnline()
      ..packageByMark[waterMark] = const MarkPackageAnswer(
        package: '0012345678905',
        marks: ['010460000000000121A1', '010460000000000121A2', '010460000000000121A3'],
      );
    final pick = await _pick(
      store,
      lines: [('uid-1', sampleGoodsName, 3)],
      settings: const AppSettings(
        registerPackageBarcode: true,
        requestPackageViaTrueApi: true,
      ),
      online: online,
    );

    final result = await pick.scan(waterMark);

    expect(result.changed, isTrue);
    expect(result.allPicked, isTrue);
    final marks = await store.marks('doc-1');
    expect(marks, hasLength(3));
    expect(marks.every((mark) => mark.units == 1), isTrue);
    expect(marks.every((mark) => mark.nomenclatureUid == 'uid-1'), isTrue);
    expect(pick.productOpen, isFalse);
  });

  test('марки упаковки сверх Количество пишутся до предела', () async {
    final store = FakeDocumentStore()..barcodeToUid = {sampleBarcode: 'uid-1'};
    final online = FakeMarkOnline()
      ..packageByMark[waterMark] = const MarkPackageAnswer(
        package: '0012345678905',
        marks: ['010460000000000121A1', '010460000000000121A2', '010460000000000121A3'],
      );
    final pick = await _pick(
      store,
      lines: [('uid-1', sampleGoodsName, 2)],
      settings: const AppSettings(
        registerPackageBarcode: true,
        requestPackageViaTrueApi: true,
      ),
      online: online,
    );

    final result = await pick.scan(waterMark);

    expect(result.changed, isTrue);
    expect(result.messages, [markOverflow]);
    expect(await store.marks('doc-1'), hasLength(2));
  });

  test('ведущий GS от сканера не мешает найти товар', () async {
    final store = FakeDocumentStore()..barcodeToUid = {sampleBarcode: 'uid-1'};
    final pick = await _pick(store);

    final result = await pick.scan('\x1d$waterMark\x1d93qmRP');

    expect(result.changed, isTrue);
    expect(pick.productOpen, isTrue);
    final marks = await store.marks('doc-1');
    expect(marks.single.mark, startsWith('01$waterGtin'));
    expect(marks.single.mark, isNot(startsWith(r'\u001d')));
  });

  test('реальная марка с ведущим GS открывает товар', () async {
    final store = FakeDocumentStore()..barcodeToUid = {'4607031101087': 'uid-1'};
    final pick = await _pick(store);

    final result = await pick.scan('\x1d0104607031101087215TXuEOoHzP)fL\x1d93qmRP');

    expect(result.changed, isTrue);
    expect(pick.productOpen, isTrue);
    expect(
      (await store.marks('doc-1')).single.mark,
      r'0104607031101087215TXuEOoHzP)fL\u001d93qmRP',
    );
  });

  test('марка без серийного номера находит товар по GTIN', () async {
    final store = FakeDocumentStore()..barcodeToUid = {sampleBarcode: 'uid-1'};
    final pick = await _pick(store);

    final result = await pick.scan('0104600000000001');

    expect(result.changed, isTrue);
    expect(pick.productOpen, isTrue);
  });

  test('код из четырнадцати цифр ищется как штрихкод товара', () async {
    final store = FakeDocumentStore()..barcodeToUid = {'04600000000001': 'uid-1'};
    final pick = await _pick(store);

    final result = await pick.scan('04600000000001');

    expect(result.changed, isTrue);
    expect(pick.productOpen, isTrue);
  });

  test('штучная марка длиной 29 находит товар по GTIN', () async {
    final store = FakeDocumentStore()..barcodeToUid = {sampleBarcode: 'uid-1'};
    final pick = await _pick(store);

    final result = await pick.scan('04600000000001ABCDEFGHIJKLMNO');

    expect(result.changed, isTrue);
    expect(pick.productOpen, isTrue);
    expect((await store.marks('doc-1')).single.nomenclatureUid, 'uid-1');
  });

  test('штучная марка без штрихкода в справочнике сообщает, что товара нет', () async {
    final store = FakeDocumentStore();
    final pick = await _pick(store);

    final result = await pick.scan('04600000000001ABCDEFGHIJKLMNO');

    expect(result.messages, [markBarcodeUnknown(sampleBarcode)]);
    expect(store.marksByDocument, isEmpty);
  });

  test('марки документа не прочитались — подбор не открывается', () async {
    final store = FakeDocumentStore()
      ..barcodeToUid = {sampleBarcode: 'uid-1'}
      ..marksError = StateError('database is closed');

    await expectLater(_pick(store), throwsA(isA<MarkPickUnavailable>()));
  });

  test('строка состава находится по УИД без учёта регистра и пробелов', () async {
    final store = FakeDocumentStore()..barcodeToUid = {sampleBarcode: 'uid-1'};
    final pick = await _pick(store, lines: [(' UID-1 ', sampleGoodsName, 12)]);

    expect(pick.lineOf('uid-1'), 0);
    expect(pick.lineOf('uid-2'), -1);

    final result = await pick.scan(waterMark);

    expect(result.changed, isTrue);
    expect(pick.selected, 0);
  });
}

/// Подбор открытого документа с заданными строками состава.
Future<MarkPick> _pick(
  FakeDocumentStore store, {
  List<(String, String, num)> lines = const [('uid-1', sampleGoodsName, 12)],
  AppSettings settings = const AppSettings(),
  FakeMarkOnline? online,
  bool uploaded = false,
  bool withoutOwnerCheck = false,
  Map<String, Object?>? organization,
  void Function(String text, Map<String, Object?> data)? trace,
}) {
  return MarkPick.open(
    document: ScannedDocument(
      id: 'doc-1',
      scannedAt: DateTime(2026, 11, 5, 10, 30),
      presentation: samplePresentation,
      erpUid: sampleErpUid,
      uploaded: uploaded,
      body: {
        'УИД': sampleErpUid,
        'БезПроверкиВладельца': withoutOwnerCheck,
        'Организация': ?organization,
        'Состав': [
          for (final line in lines)
            {'Номенклатура': line.$1, 'Количество': line.$3},
        ],
      },
    ),
    composition: [
      for (final line in lines)
        CompositionLine(
          nomenclatureUid: line.$1,
          name: line.$2,
          quantity: line.$3,
          marks: 0,
        ),
    ],
    settings: settings,
    store: store,
    online: online ?? FakeMarkOnline(),
    trace: trace,
  );
}
