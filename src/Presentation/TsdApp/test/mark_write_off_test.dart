import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:tsd_app/domain/documents/scanned_document.dart';
import 'package:tsd_app/domain/documents/stored_mark.dart';
import 'package:tsd_app/domain/documents/write_off_api.dart';
import 'package:tsd_app/infrastructure/scan/clipboard_scan_watcher.dart';
import 'package:tsd_app/infrastructure/settings/app_settings.dart';
import 'package:tsd_app/main.dart';
import 'package:tsd_app/presentation/mark_write_off/mark_write_off_screen.dart';

import 'documents_support.dart';

const _mark = '0104603334003509215EQ26tdHwAvn\x1d93ee1c';
const _sameCis = '0104603334003509215EQ26tdHwAvn\x1d93ffff';
const _otherMark = '0104607022743036215hJZ5k';
const _productName = 'Молоко пастеризованное 3,2%';

void main() {
  setUp(() {
    SharedPreferences.setMockInitialValues({});
  });

  testWidgets('фасовка показывает товар, берёт количество из ЧЗ и цену из Frontol', (tester) async {
    final store = FakeDocumentStore();
    final api = FakeWriteOffApi();
    final document = await store.createWriteOff(scannedAt: DateTime(2026, 11, 5, 11));

    await _open(tester, store, api, document);
    await _scan(tester, _mark);

    expect(api.infoMarks, [markStorageText(_mark)]);
    expect(api.infoInns, ['2464122182']);
    expect(api.priceBarcodes, ['04603334003509']);
    expect(find.text(_productName), findsOneWidget);
    expect(find.text('0.5'), findsOneWidget);
    expect(find.text('250'), findsOneWidget);

    final saved = store.marksByDocument[document.id]!.single;
    expect(saved.quantity, 0.5);
    expect(saved.price, 250);
    expect(saved.name, _productName);
  });

  testWidgets('сохранённое название не запрашивается при открытии', (tester) async {
    final store = FakeDocumentStore();
    final api = FakeWriteOffApi();
    final document = await store.createWriteOff(scannedAt: DateTime(2026, 11, 5, 11));

    await _open(tester, store, api, document);
    await _scan(tester, _mark);
    expect(api.infoMarks, hasLength(1));

    await tester.pumpWidget(const SizedBox());
    await tester.pumpAndSettle();
    await _open(tester, store, api, document);

    expect(api.infoMarks, hasLength(1));
    expect(find.text(_productName), findsOneWidget);
  });

  testWidgets('одна организация выбирается по умолчанию и сохраняется в документе', (tester) async {
    final store = FakeDocumentStore();
    final api = FakeWriteOffApi();
    final document = await store.createWriteOff(scannedAt: DateTime(2026, 11, 5, 11));

    await _open(tester, store, api, document);

    expect(find.text('ООО Ромашка (2464122182)'), findsOneWidget);
    expect(store.savedOrganisations, contains((document.id, '2464122182', 'ООО Ромашка')));

    final saved = await store.document(document.id);
    expect(saved?.inn, '2464122182');
    expect(saved?.organisationName, 'ООО Ромашка');
    expect(saved?.printGroupId, 1);
  });

  testWidgets('организации с одним ИНН различаются печатной группой и КПП', (tester) async {
    final store = FakeDocumentStore();
    final api = FakeWriteOffApi()
      ..organisationsList = [
        const WriteOffOrganisation(
          id: 1,
          inn: '2464122182',
          name: 'Магазин 1',
          kpp: '246401001',
        ),
        const WriteOffOrganisation(
          id: 2,
          inn: '2464122182',
          name: 'Магазин 2',
          kpp: '246402001',
        ),
      ];
    final document = await store.createWriteOff(scannedAt: DateTime(2026, 11, 5, 11));

    await _open(tester, store, api, document);

    expect(find.text(writeOffNoOrganisation), findsOneWidget);

    await tester.tap(find.byKey(const Key('write-off-organisation')));
    await tester.pumpAndSettle();

    expect(find.text('Магазин 1 (2464122182), КПП 246401001'), findsOneWidget);
    expect(find.text('Магазин 2 (2464122182), КПП 246402001'), findsOneWidget);

    await tester.tap(find.text('Магазин 2 (2464122182), КПП 246402001').last);
    await tester.pumpAndSettle();

    await _scan(tester, _mark);
    await tester.tap(find.text('Выгрузить'));
    await tester.pumpAndSettle();

    expect(api.receipts.single.inn, '2464122182');
    expect(api.receipts.single.printGroupId, 2);

    final saved = await store.document(document.id);
    expect(saved?.inn, '2464122182');
    expect(saved?.organisationName, 'Магазин 2');
    expect(saved?.printGroupId, 2);
  });

  testWidgets('при нескольких организациях выбор обязателен и уходит в акт', (tester) async {
    final store = FakeDocumentStore();
    final api = FakeWriteOffApi()
      ..organisationsList = [
        const WriteOffOrganisation(id: 1, inn: '1111111111', name: 'Фирма Один'),
        const WriteOffOrganisation(id: 2, inn: '2222222222', name: 'Фирма Два'),
      ];
    final document = await store.createWriteOff(scannedAt: DateTime(2026, 11, 5, 11));

    await _open(tester, store, api, document);

    expect(find.text(writeOffNoOrganisation), findsOneWidget);
    expect(store.savedOrganisations, isEmpty);

    await _scan(tester, _mark);

    expect(store.marksByDocument[document.id] ?? [], isEmpty);
    expect(api.infoMarks, isEmpty);
    expect(api.priceBarcodes, isEmpty);

    await tester.tap(find.byKey(const Key('write-off-organisation')));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Фирма Два (2222222222)').last);
    await tester.pumpAndSettle();

    expect(store.savedOrganisations, contains((document.id, '2222222222', 'Фирма Два')));

    await _scan(tester, _mark);

    expect(store.marksByDocument[document.id], hasLength(1));
    expect(api.infoMarks, hasLength(1));
    expect(api.infoInns, ['2222222222']);

    await tester.tap(find.text('Выгрузить'));
    await tester.pumpAndSettle();

    expect(api.receipts.single.inn, '2222222222');
    expect(api.receipts.single.printGroupId, 2);
    expect(api.receipts.single.uid, document.id);
    expect(api.receipts.single.terminalId, isNotEmpty);
  });

  testWidgets('без организаций в Fmu-Api марка не принимается', (tester) async {
    final store = FakeDocumentStore();
    final api = FakeWriteOffApi()..organisationsList = [];
    final document = await store.createWriteOff(scannedAt: DateTime(2026, 11, 5, 11));

    await _open(tester, store, api, document);
    await _scan(tester, _mark);

    expect(store.marksByDocument[document.id] ?? [], isEmpty);
    expect(find.text(writeOffNoOrganisation), findsWidgets);
  });

  testWidgets('удалённая печатная группа документ не закрывает и марки не принимает', (tester) async {
    final store = FakeDocumentStore();
    final api = FakeWriteOffApi()
      ..organisationsList = [
        const WriteOffOrganisation(id: 1, inn: '1111111111', name: 'Фирма Один'),
      ];
    final created = await store.createWriteOff(scannedAt: DateTime(2026, 11, 5, 11));
    await store.setOrganisation(
      created.id,
      inn: '2222222222',
      name: 'Старый магазин',
      printGroupId: 9,
    );
    final document = (await store.document(created.id))!;

    await _open(tester, store, api, document);
    await _scan(tester, _mark);

    expect(find.text('Была: Старый магазин'), findsOneWidget);
    expect(find.text(writeOffNoOrganisation), findsWidgets);
    expect(store.marksByDocument[document.id] ?? [], isEmpty);
  });

  testWidgets('во время выгрузки виден спиннер', (tester) async {
    final store = FakeDocumentStore();
    final api = FakeWriteOffApi();
    final gate = Completer<void>();
    api.receiptGate = gate;
    final document = await store.createWriteOff(scannedAt: DateTime(2026, 11, 5, 11));

    await _open(tester, store, api, document);
    await _scan(tester, _mark);

    await tester.tap(find.text('Выгрузить'));
    await tester.pump();

    expect(find.byType(CircularProgressIndicator), findsOneWidget);

    gate.complete();
    await tester.pumpAndSettle();

    expect(find.byType(CircularProgressIndicator), findsNothing);
    expect(store.uploads, [(document.id, true)]);
  });

  testWidgets('сохранённая организация выбирается при открытии', (tester) async {
    final store = FakeDocumentStore();
    final api = FakeWriteOffApi()
      ..organisationsList = [
        const WriteOffOrganisation(id: 1, inn: '1111111111', name: 'Фирма Один'),
        const WriteOffOrganisation(id: 2, inn: '2222222222', name: 'Фирма Два'),
      ];
    final document = await store.createWriteOff(scannedAt: DateTime(2026, 11, 5, 11));

    await _open(tester, store, api, document);
    await tester.tap(find.byKey(const Key('write-off-organisation')));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Фирма Два (2222222222)').last);
    await tester.pumpAndSettle();

    await tester.pumpWidget(const SizedBox());
    await tester.pumpAndSettle();

    final reopened = (await store.document(document.id))!;
    await _open(tester, store, api, reopened);

    expect(find.text('Фирма Два (2222222222)'), findsOneWidget);
    expect(find.text(writeOffNoOrganisation), findsNothing);
    expect(api.organisationsCalls, 2);
  });

  testWidgets('штучный товар получает количество 1', (tester) async {
    final store = FakeDocumentStore();
    final api = FakeWriteOffApi()
      ..info = const WriteOffMarkInfo(name: 'Вода питьевая', gtin: '04603334003509');
    final document = await store.createWriteOff(scannedAt: DateTime(2026, 11, 5, 11));

    await _open(tester, store, api, document);
    await _scan(tester, _mark);

    expect(find.text('Вода питьевая'), findsOneWidget);
    expect(find.text('1'), findsOneWidget);
    expect(store.marksByDocument[document.id]!.single.quantity, 1);
  });

  testWidgets('истечение срока не спрашивает цену и не показывает колонку', (tester) async {
    final store = FakeDocumentStore();
    final api = FakeWriteOffApi();
    final document = await _expirationDocument(store);

    await _open(tester, store, api, document);
    await _scan(tester, _mark);

    expect(api.priceBarcodes, isEmpty);
    expect(api.infoMarks, hasLength(1));
    expect(find.text(_productName), findsOneWidget);
    expect(find.text('Цена'), findsNothing);
    expect(store.marksByDocument[document.id]!.single.price, isNull);
  });

  testWidgets('смена на фасовку спрашивает цену уже лежащих марок', (tester) async {
    final store = FakeDocumentStore();
    final api = FakeWriteOffApi();
    final document = await _expirationDocument(store);

    await _open(tester, store, api, document);
    await _scan(tester, _mark);
    expect(api.priceBarcodes, isEmpty);

    await tester.tap(find.byKey(const Key('write-off-reason')));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Фасовка').last);
    await tester.pumpAndSettle();

    expect(api.priceBarcodes, ['04603334003509']);
    expect(find.text('Цена'), findsOneWidget);
    expect(find.text('250'), findsOneWidget);
    expect(store.reasons, contains((document.id, packingReason)));

    await tester.tap(find.byKey(const Key('write-off-reason')));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Истечение срока').last);
    await tester.pumpAndSettle();

    expect(find.text('Цена'), findsNothing);
  });

  testWidgets('правка количества и цены сохраняется в строке', (tester) async {
    final store = FakeDocumentStore();
    final api = FakeWriteOffApi();
    final document = await store.createWriteOff(scannedAt: DateTime(2026, 11, 5, 11));

    await _open(tester, store, api, document);
    await _scan(tester, _mark);

    await tester.tap(find.text(_productName));
    await tester.pumpAndSettle();

    await tester.enterText(find.byKey(const Key('write-off-quantity')), '0,75');
    await tester.enterText(find.byKey(const Key('write-off-price')), '300');
    await tester.tap(find.text('Сохранить'));
    await tester.pumpAndSettle();

    final saved = store.marksByDocument[document.id]!.single;
    expect(saved.quantity, 0.75);
    expect(saved.price, 300);
    expect(find.text('0.75'), findsOneWidget);
    expect(find.text('300'), findsOneWidget);
  });

  testWidgets('строку можно удалить', (tester) async {
    final store = FakeDocumentStore();
    final api = FakeWriteOffApi();
    final document = await store.createWriteOff(scannedAt: DateTime(2026, 11, 5, 11));

    await _open(tester, store, api, document);
    await _scan(tester, _mark);
    expect(store.marksByDocument[document.id], hasLength(1));

    await tester.tap(find.byIcon(Icons.delete_outline));
    await tester.pumpAndSettle();

    expect(store.marksByDocument[document.id], isEmpty);
    expect(find.text(_productName), findsNothing);
    expect(find.text('Отсканируйте марку'), findsOneWidget);
  });

  testWidgets('выгрузка шлёт причину, марки, количество и цену', (tester) async {
    final store = FakeDocumentStore();
    final api = FakeWriteOffApi();
    final document = await store.createWriteOff(scannedAt: DateTime(2026, 11, 5, 11));

    await _open(tester, store, api, document);
    await _scan(tester, _mark);

    await tester.tap(find.text('Выгрузить'));
    await tester.pumpAndSettle();

    expect(api.receipts, hasLength(1));
    final receipt = api.receipts.single;
    expect(receipt.inn, '2464122182');
    expect(receipt.reason, packingReason);
    expect(receipt.lines, hasLength(1));
    expect(receipt.lines.single.mark, markStorageText(_mark));
    expect(receipt.lines.single.quantity, 0.5);
    expect(receipt.lines.single.price, 250);
    expect(receipt.printGroupId, 1);
    expect(store.uploads, [(document.id, true)]);
    expect(find.text(writeOffUploadedMark), findsOneWidget);

    await _scan(tester, _otherMark);

    expect(store.marksByDocument[document.id], hasLength(1));
    expect(find.text(writeOffUploadedLock), findsOneWidget);
  });

  testWidgets('ошибка выгрузки оставляет отметку выгрузки пустой', (tester) async {
    final store = FakeDocumentStore();
    final api = FakeWriteOffApi()..receiptError = Exception('Нет связи с сервером');
    final document = await store.createWriteOff(scannedAt: DateTime(2026, 11, 5, 11));

    await _open(tester, store, api, document);
    await _scan(tester, _mark);

    await tester.tap(find.text('Выгрузить'));
    await tester.pumpAndSettle();

    expect(store.uploads, isEmpty);
    expect(find.text(writeOffUploadedMark), findsNothing);
    expect(find.textContaining('Нет связи с сервером'), findsOneWidget);
  });

  testWidgets('старый сервер без строк подсказывает обновить Fmu-Api', (tester) async {
    final store = FakeDocumentStore();
    final api = FakeWriteOffApi()..receiptError = Exception('В документе нет ни одной марки');
    final document = await store.createWriteOff(scannedAt: DateTime(2026, 11, 5, 11));

    await _open(tester, store, api, document);
    await _scan(tester, _mark);

    await tester.tap(find.text('Выгрузить'));
    await tester.pumpAndSettle();

    expect(find.textContaining('Обновите Fmu-Api'), findsOneWidget);
    expect(find.textContaining('Exception:'), findsNothing);
    expect(store.uploads, isEmpty);
  });

  testWidgets('пустой документ выгрузку не начинает', (tester) async {
    final store = FakeDocumentStore();
    final api = FakeWriteOffApi();
    final document = await store.createWriteOff(scannedAt: DateTime(2026, 11, 5, 11));

    await _open(tester, store, api, document);
    await tester.tap(find.text('Выгрузить'));
    await tester.pumpAndSettle();

    expect(api.receipts, isEmpty);
    expect(store.uploads, isEmpty);
    expect(find.text(writeOffNoMarks), findsOneWidget);
  });

  testWidgets('камера видна когда включена в настройках', (tester) async {
    final store = FakeDocumentStore();
    final api = FakeWriteOffApi();
    final document = await store.createWriteOff(scannedAt: DateTime(2026, 11, 5, 11));

    await _open(
      tester,
      store,
      api,
      document,
      settings: const AppSettings(apiBaseUrl: 'http://10.0.0.5:5000', cameraScanEnabled: true),
    );

    expect(find.byIcon(Icons.qr_code_scanner), findsOneWidget);
  });

  testWidgets('повтор по КИ и не марка марку не пишут', (tester) async {
    final store = FakeDocumentStore();
    final api = FakeWriteOffApi();
    final document = await store.createWriteOff(scannedAt: DateTime(2026, 11, 5, 11));

    await _open(tester, store, api, document);
    await _scan(tester, _mark);
    await _scan(tester, _sameCis);

    expect(store.marksByDocument[document.id], hasLength(1));
    expect(find.text(writeOffDuplicateMark), findsOneWidget);

    await _scan(tester, 'привет');

    expect(store.marksByDocument[document.id], hasLength(1));
    expect(find.text(writeOffNotMark), findsOneWidget);
  });
}

Future<ScannedDocument> _expirationDocument(FakeDocumentStore store) async {
  final created = await store.createWriteOff(scannedAt: DateTime(2026, 11, 5, 11));
  await store.setReason(created.id, reason: expirationReason);
  return (await store.document(created.id))!;
}

Future<FakeWriteOffApi> _open(
  WidgetTester tester,
  FakeDocumentStore store,
  FakeWriteOffApi api,
  ScannedDocument document, {
  AppSettings? settings,
}) async {
  _clipboard = '';
  _watcher = ClipboardScanWatcher(
    interval: const Duration(milliseconds: 20),
    requireMark: false,
    clearAfterEmit: false,
    readClipboard: () async => _clipboard,
    clearClipboard: () async {},
  );
  await tester.pumpWidget(
    TsdApp(
      home: MarkWriteOffScreen(
        settings: settings ?? const AppSettings(apiBaseUrl: 'http://10.0.0.5:5000'),
        document: document,
        store: store,
        api: api,
        clipboard: _watcher,
      ),
    ),
  );
  await tester.pumpAndSettle();
  return api;
}

var _clipboard = '';
late ClipboardScanWatcher _watcher;

Future<void> _scan(WidgetTester tester, String code) async {
  _watcher.remember('');
  _clipboard = code;
  await tester.pump(const Duration(milliseconds: 60));
  await tester.pumpAndSettle();
}

class FakeWriteOffApi implements WriteOffApi {
  final priceBarcodes = <String>[];
  final infoMarks = <String>[];
  final infoInns = <String>[];
  final receipts = <FakeReceipt>[];
  var organisationsCalls = 0;
  var organisationsList = <WriteOffOrganisation>[
    const WriteOffOrganisation(id: 1, inn: '2464122182', name: 'ООО Ромашка'),
  ];
  num? price = 250;
  WriteOffMarkInfo? info = const WriteOffMarkInfo(
    name: _productName,
    gtin: '04603334003509',
    quantity: 0.5,
  );
  Object? receiptError;
  Completer<void>? receiptGate;

  @override
  Future<List<WriteOffOrganisation>> organisations() async {
    organisationsCalls += 1;
    return organisationsList;
  }

  @override
  Future<WriteOffMarkInfo?> markInfo({required String inn, required String mark}) async {
    infoInns.add(inn);
    infoMarks.add(mark);
    return info;
  }

  @override
  Future<num?> retailPriceByBarcode(String barcode) async {
    priceBarcodes.add(barcode);
    return price;
  }

  @override
  Future<void> createReceipt({
    required String inn,
    required String reason,
    required List<WriteOffReceiptLine> lines,
    int? printGroupId,
    String uid = '',
    String number = '',
    String terminalId = '',
  }) async {
    receipts.add(
      FakeReceipt(
        inn: inn,
        reason: reason,
        lines: lines,
        printGroupId: printGroupId,
        uid: uid,
        number: number,
        terminalId: terminalId,
      ),
    );
    final gate = receiptGate;
    if (gate != null) {
      await gate.future;
    }
    final error = receiptError;
    if (error != null) {
      throw error;
    }
  }
}

class FakeReceipt {
  const FakeReceipt({
    required this.inn,
    required this.reason,
    required this.lines,
    this.printGroupId,
    this.uid = '',
    this.number = '',
    this.terminalId = '',
  });

  final String inn;
  final String reason;
  final List<WriteOffReceiptLine> lines;
  final int? printGroupId;
  final String uid;
  final String number;
  final String terminalId;
}
