import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:tsd_app/application/documents/document_loader.dart';
import 'package:tsd_app/application/mark/mark_pick.dart';
import 'package:tsd_app/application/mark/mark_uploader.dart';
import 'package:tsd_app/domain/documents/document_packet.dart';
import 'package:tsd_app/domain/documents/scanned_document.dart';
import 'package:tsd_app/domain/documents/stored_mark.dart';
import 'package:tsd_app/domain/exchange/exchange_call_result.dart';
import 'package:tsd_app/domain/exchange/exchange_protocol.dart';
import 'package:tsd_app/domain/mark/mark_online.dart';
import 'package:tsd_app/infrastructure/scan/clipboard_scan_watcher.dart';
import 'package:tsd_app/main.dart';
import 'package:tsd_app/presentation/mark_collection/mark_collection_screen.dart';
import 'package:tsd_app/theme/webix_dark_theme.dart';

import 'documents_support.dart';

void main() {
  testWidgets('открытый документ сразу показывает состав из справочника', (tester) async {
    final store = FakeDocumentStore()..names = {sampleGoodsUid: sampleGoodsName};

    await tester.pumpWidget(
      TsdApp(
        home: MarkCollectionScreen(
          settings: settingsWithServer(),
          store: store,
          document: _document(),
        ),
      ),
    );
    await tester.pumpAndSettle();

    await tester.tap(find.text('Состав'));
    await tester.pumpAndSettle();

    expect(find.text(sampleGoodsName), findsOneWidget);
    expect(find.text('12'), findsOneWidget);
    expect(find.text('0'), findsOneWidget);
    expect(store.requestedUids.single, {sampleGoodsUid});
  });

  testWidgets('состав показывает наименование отдельно, под ним количество и марки', (tester) async {
    final store = FakeDocumentStore()..names = {sampleGoodsUid: sampleGoodsName};

    await tester.pumpWidget(
      TsdApp(
        home: MarkCollectionScreen(
          settings: settingsWithServer(),
          store: store,
          document: _document(),
        ),
      ),
    );
    await tester.pumpAndSettle();
    await tester.tap(find.text('Состав'));
    await tester.pumpAndSettle();

    expect(find.text('Наименование'), findsOneWidget);
    expect(find.text('Количество'), findsOneWidget);
    expect(find.text('Подобрано'), findsOneWidget);

    final name = tester.getTopLeft(find.text(sampleGoodsName));
    final quantity = tester.getTopLeft(find.text('12'));
    final marks = tester.getTopLeft(find.text('0'));

    expect(quantity.dy, greaterThan(name.dy));
    expect((quantity.dy - marks.dy).abs(), lessThan(6));
    expect(marks.dx, greaterThan(quantity.dx));
  });

  testWidgets('штрихкод на шапке грузит документ и открывает состав', (tester) async {
    final store = FakeDocumentStore()..names = {sampleGoodsUid: sampleGoodsName};
    final protocol = DocumentProtocol(
      (barcode) async => const ExchangeCallResult(success: true, body: sampleDocumentResponse),
    );

    await tester.pumpWidget(
      TsdApp(
        home: MarkCollectionScreen(
          settings: settingsWithServer(),
          store: store,
          loader: DocumentLoader(store: store, protocol: (_) => protocol),
          clipboard: _clipboard(sampleBarcode),
        ),
      ),
    );
    await tester.pumpAndSettle();

    expect(protocol.barcodes, [sampleBarcode]);
    expect(store.saved.single.document['УИД'], sampleErpUid);
    expect(find.text(sampleGoodsName), findsOneWidget);
    expect(find.text('0'), findsOneWidget);
  });

  testWidgets('пустой пакет показывает ошибку и в базу не пишет', (tester) async {
    final store = FakeDocumentStore();
    final protocol = DocumentProtocol(
      (barcode) async => const ExchangeCallResult(success: true, body: emptyDocumentsResponse),
    );

    await tester.pumpWidget(
      TsdApp(
        home: MarkCollectionScreen(
          settings: settingsWithServer(),
          store: store,
          loader: DocumentLoader(store: store, protocol: (_) => protocol),
          clipboard: _clipboard(sampleBarcode),
        ),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text('Загрузка документа завершилась с ошибкой: $documentMissing'), findsOneWidget);
    expect(store.saved, isEmpty);
  });

  testWidgets('отмена ожидания не создаёт документ', (tester) async {
    final store = FakeDocumentStore();
    final gate = Completer<ExchangeCallResult>();
    final protocol = DocumentProtocol((barcode) => gate.future);

    await tester.pumpWidget(
      TsdApp(
        home: MarkCollectionScreen(
          settings: settingsWithServer(),
          store: store,
          loader: DocumentLoader(store: store, protocol: (_) => protocol),
          clipboard: _clipboard(sampleBarcode),
        ),
      ),
    );
    await tester.pump();
    await tester.pump();
    expect(find.text('Ожидание...'), findsOneWidget);

    await tester.tap(find.text('Отмена'));
    await tester.pumpAndSettle();

    expect(find.text('Ожидание...'), findsNothing);
    expect(find.text(markCollectionHint), findsOneWidget);
    expect(store.saved, isEmpty);
  });

  testWidgets('в открытом документе штрихкод загрузку не запускает', (tester) async {
    final store = FakeDocumentStore()..names = {sampleGoodsUid: sampleGoodsName};
    final protocol = DocumentProtocol(
      (barcode) async => const ExchangeCallResult(success: true, body: sampleDocumentResponse),
    );

    await tester.pumpWidget(
      TsdApp(
        home: MarkCollectionScreen(
          settings: settingsWithServer(),
          store: store,
          document: _document(),
          loader: DocumentLoader(store: store, protocol: (_) => protocol),
          clipboard: _clipboard(sampleBarcode),
        ),
      ),
    );
    await tester.pumpAndSettle();

    expect(protocol.barcodes, isEmpty);
    expect(find.text('Ожидание...'), findsNothing);
    expect(store.saved, isEmpty);
  });

  testWidgets('марка на шапке не запускает загрузку документа', (tester) async {
    final store = FakeDocumentStore();
    final protocol = DocumentProtocol(
      (barcode) async => const ExchangeCallResult(success: true, body: sampleDocumentResponse),
    );

    await tester.pumpWidget(
      TsdApp(
        home: MarkCollectionScreen(
          settings: settingsWithServer(),
          store: store,
          loader: DocumentLoader(store: store, protocol: (_) => protocol),
          clipboard: _clipboard('010460071404010521AB\x1d93abcd'),
        ),
      ),
    );
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 400));

    expect(protocol.barcodes, isEmpty);
    expect(store.saved, isEmpty);
    expect(find.text('Ожидание...'), findsNothing);
    expect(find.text('Это не тот тип штрихкода: нужен QR код документа'), findsOneWidget);
  });

  testWidgets('в загруженном документе EAN-13 открывает позицию', (tester) async {
    final store = FakeDocumentStore()
      ..names = {sampleGoodsUid: sampleGoodsName}
      ..barcodeToUid = {'4607118000005': sampleGoodsUid};
    final clipboard = ClipboardScanWatcher(
      interval: const Duration(seconds: 5),
      requireMark: false,
      clearAfterEmit: false,
      readClipboard: () async => '',
      clearClipboard: () async {},
    );

    await tester.pumpWidget(
      TsdApp(
        home: MarkCollectionScreen(
          settings: settingsWithServer(),
          store: store,
          document: _document(),
          clipboard: clipboard,
        ),
      ),
    );
    await tester.pumpAndSettle();

    clipboard.emit('4607118000005');
    await tester.pumpAndSettle();

    expect(find.text('Всего марок'), findsOneWidget);
    expect(find.text(sampleGoodsName), findsOneWidget);

    expect(tester.widget<Text>(find.text('Всего марок')).textAlign, TextAlign.center);
    expect(tester.widget<Text>(find.text('12')).textAlign, TextAlign.center);
    expect(tester.widget<Text>(find.text('0')).textAlign, TextAlign.center);
    expect(tester.widget<Text>(find.text(sampleGoodsName)).style?.fontSize, 22);
  });

  testWidgets('штрихкод открывает свою строку состава, а не первую', (tester) async {
    final store = FakeDocumentStore()
      ..names = {'uid-1': 'Лимонад 1л', 'uid-2': 'Лель 0.65/8'}
      ..barcodeToUid = {'4607031101087': 'uid-2'};
    final clipboard = ClipboardScanWatcher(
      interval: const Duration(seconds: 5),
      requireMark: false,
      clearAfterEmit: false,
      readClipboard: () async => '',
      clearClipboard: () async {},
    );

    await tester.pumpWidget(
      TsdApp(
        home: MarkCollectionScreen(
          settings: settingsWithServer(),
          store: store,
          document: _documentWithLines([
            ('uid-1', 6, 0),
            ('uid-2', 16, 0),
          ]),
          clipboard: clipboard,
        ),
      ),
    );
    await tester.pumpAndSettle();

    clipboard.emit('4607031101087');
    await tester.pumpAndSettle();

    expect(find.text('Лель 0.65/8'), findsOneWidget);
    expect(find.text('Лимонад 1л'), findsNothing);
    expect(find.text('16'), findsOneWidget);
  });

  testWidgets('EAN-13 в марке находит строку по GTIN', (tester) async {
    final store = FakeDocumentStore()
      ..names = {'uid-1': 'Лимонад 1л', 'uid-2': 'Лель 0.65/8'}
      ..barcodeToUid = {'4607031101087': 'uid-2'};
    final clipboard = ClipboardScanWatcher(
      interval: const Duration(seconds: 5),
      requireMark: false,
      clearAfterEmit: false,
      readClipboard: () async => '',
      clearClipboard: () async {},
    );

    await tester.pumpWidget(
      TsdApp(
        home: MarkCollectionScreen(
          settings: settingsWithServer(),
          store: store,
          document: _documentWithLines([
            ('uid-1', 6, 0),
            ('uid-2', 16, 0),
          ]),
          clipboard: clipboard,
        ),
      ),
    );
    await tester.pumpAndSettle();

    clipboard.emit('010460703110108721ABC123\x1d93xyz');
    await tester.pumpAndSettle();

    expect(find.text('Лель 0.65/8'), findsOneWidget);
  });

  testWidgets('незнакомый штрихкод пишет, что такого нет в справочнике', (tester) async {
    final store = FakeDocumentStore()..names = {sampleGoodsUid: sampleGoodsName};
    final clipboard = _watcher();

    await tester.pumpWidget(
      TsdApp(
        home: MarkCollectionScreen(
          settings: settingsWithServer(),
          store: store,
          document: _document(),
          clipboard: clipboard,
        ),
      ),
    );
    await tester.pumpAndSettle();

    clipboard.emit('4607118000005');
    await tester.pumpAndSettle();

    expect(find.text('Такого штрихкода нет в справочнике товаров'), findsOneWidget);
  });

  testWidgets('товар по штрихкоду не из документа пишет, что его нет в документе', (tester) async {
    final store = FakeDocumentStore()
      ..barcodeToUid = {'4607118000005': 'uid-other'};
    final clipboard = _watcher();

    await tester.pumpWidget(
      TsdApp(
        home: MarkCollectionScreen(
          settings: settingsWithServer(),
          store: store,
          document: _document(),
          clipboard: clipboard,
        ),
      ),
    );
    await tester.pumpAndSettle();

    clipboard.emit('4607118000005');
    await tester.pumpAndSettle();

    expect(find.text('Товара с таким штрихкодом нет в документе'), findsOneWidget);
  });

  testWidgets('марка, отсканированная до чтения состава, находит товар', (tester) async {
    final gate = Completer<void>();
    final store = FakeDocumentStore()
      ..names = {sampleGoodsUid: sampleGoodsName}
      ..barcodeToUid = {'4607118000005': sampleGoodsUid}
      ..namesGate = gate;
    final clipboard = _watcher();

    await tester.pumpWidget(
      TsdApp(
        home: MarkCollectionScreen(
          settings: settingsWithServer(),
          store: store,
          document: _documentWithLines([(sampleGoodsUid, 12, 0)]),
          clipboard: clipboard,
        ),
      ),
    );
    await tester.pump();

    clipboard.emit('010460711800000521ABCDE');
    await tester.pump();

    expect(find.text(documentNotReady), findsNothing);

    gate.complete();
    await tester.pumpAndSettle();

    expect(find.text(sampleGoodsName), findsOneWidget);
    expect((await store.marks('doc-lines')).single.nomenclatureUid, sampleGoodsUid);
  });

  testWidgets('марка без товара в справочнике сообщает об этом', (tester) async {
    final store = FakeDocumentStore()..names = {sampleGoodsUid: sampleGoodsName};
    final clipboard = _watcher();

    await tester.pumpWidget(
      TsdApp(
        home: MarkCollectionScreen(
          settings: settingsWithServer(),
          store: store,
          document: _documentWithLines([(sampleGoodsUid, 12, 0)]),
          clipboard: clipboard,
        ),
      ),
    );
    await tester.pumpAndSettle();

    clipboard.emit('010460711800000521ABCDE');
    await tester.pumpAndSettle();

    expect(find.text(markBarcodeUnknown('4607118000005'), findRichText: true), findsOneWidget);
    expect(store.marksByDocument, isEmpty);
  });

  testWidgets('QR документа в загруженном документе загрузку не запускает', (tester) async {
    final store = FakeDocumentStore()..names = {sampleGoodsUid: sampleGoodsName};
    final clipboard = _watcher();

    await tester.pumpWidget(
      TsdApp(
        home: MarkCollectionScreen(
          settings: settingsWithServer(),
          store: store,
          document: _document(),
          clipboard: clipboard,
        ),
      ),
    );
    await tester.pumpAndSettle();

    clipboard.emit('DOC-2026-000123');
    await tester.pumpAndSettle();

    expect(find.text('Ожидание...'), findsNothing);
    expect(find.text(markNeedsBarcode), findsOneWidget);
    expect(store.saved, isEmpty);
  });

  testWidgets('марка без привычной структуры GS1 ищет товар по штрихкоду', (tester) async {
    final store = FakeDocumentStore()
      ..names = {sampleGoodsUid: sampleGoodsName}
      ..barcodeToUid = {sampleBarcode: sampleGoodsUid};
    final clipboard = _watcher();

    await tester.pumpWidget(
      TsdApp(
        home: MarkCollectionScreen(
          settings: settingsWithServer(),
          store: store,
          document: _documentWithLines([(sampleGoodsUid, 12, 0)]),
          clipboard: clipboard,
        ),
      ),
    );
    await tester.pumpAndSettle();

    clipboard.emit('0104600000000001');
    await tester.pumpAndSettle();

    expect(find.text(sampleGoodsName), findsOneWidget);
    expect((await store.marks('doc-lines')).single.nomenclatureUid, sampleGoodsUid);
  });

  testWidgets('без выбранного сервера показывается предупреждение и документ не пишется', (tester) async {
    final store = FakeDocumentStore();

    await tester.pumpWidget(
      TsdApp(
        home: MarkCollectionScreen(
          settings: settingsWithServer(withServer: false),
          store: store,
          loader: DocumentLoader(store: store, protocol: (_) => null),
          clipboard: _clipboard(sampleBarcode),
        ),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text('Загрузка документа завершилась с ошибкой: $exchangeServerMissing'), findsOneWidget);
    expect(store.saved, isEmpty);
  });

  testWidgets('EAN-13 открывает строку, если УИД отличается регистром и пробелами', (tester) async {
    final store = FakeDocumentStore()
      ..names = {'UID-1': sampleGoodsName}
      ..barcodeToUid = {'4607118000005': 'uid-1'};
    final clipboard = _watcher();

    await tester.pumpWidget(
      TsdApp(
        home: MarkCollectionScreen(
          settings: settingsWithServer(),
          store: store,
          document: _documentWithLines([('UID-1', 12, 0)]),
          clipboard: clipboard,
        ),
      ),
    );
    await tester.pumpAndSettle();

    clipboard.emit('4607118000005');
    await tester.pumpAndSettle();

    expect(find.text('Всего марок'), findsOneWidget);
    expect(find.text(sampleGoodsName), findsOneWidget);
    expect(find.text('Товара с таким штрихкодом нет в документе'), findsNothing);
  });

  testWidgets('второй скан не пишется, пока первый ждёт запись в базу', (tester) async {
    final gate = Completer<void>();
    final store = FakeDocumentStore()
      ..names = {sampleGoodsUid: sampleGoodsName}
      ..barcodeToUid = {'4607118000005': sampleGoodsUid}
      ..addMarkGate = gate;
    final clipboard = _watcher();

    await tester.pumpWidget(
      TsdApp(
        home: MarkCollectionScreen(
          settings: settingsWithServer(),
          store: store,
          document: _documentWithLines([(sampleGoodsUid, 12, 0)]),
          clipboard: clipboard,
        ),
      ),
    );
    await tester.pumpAndSettle();

    clipboard.emit('010460711800000521ABCDE');
    await tester.pump();
    clipboard.emit('010460711800000521ABCDZ');
    await tester.pump();

    gate.complete();
    await tester.pumpAndSettle();

    expect(await store.marks('doc-lines'), hasLength(1));
    expect(find.text(markOverflow), findsNothing);
  });

  testWidgets('марки не прочитались — подбор не открывается и скан сообщает причину', (tester) async {
    final store = FakeDocumentStore()
      ..names = {sampleGoodsUid: sampleGoodsName}
      ..barcodeToUid = {'4607118000005': sampleGoodsUid}
      ..marksError = StateError('database is closed');
    final clipboard = _watcher();

    await tester.pumpWidget(
      TsdApp(
        home: MarkCollectionScreen(
          settings: settingsWithServer(),
          store: store,
          document: _documentWithLines([(sampleGoodsUid, 12, 0)]),
          clipboard: clipboard,
        ),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text(MarkPickUnavailable.text), findsOneWidget);

    clipboard.emit('010460711800000521ABCDE');
    await tester.pumpAndSettle();

    expect(find.text(MarkPickUnavailable.text), findsWidgets);
    expect(store.marksByDocument, isEmpty);
  });

  testWidgets('состав обновляет «Подобрано» после скана', (tester) async {
    final store = FakeDocumentStore()
      ..names = {sampleGoodsUid: sampleGoodsName}
      ..barcodeToUid = {'4607118000005': sampleGoodsUid};
    final clipboard = _watcher();

    await tester.pumpWidget(
      TsdApp(
        home: MarkCollectionScreen(
          settings: settingsWithServer(),
          store: store,
          document: _documentWithLines([(sampleGoodsUid, 12, 0)]),
          clipboard: clipboard,
        ),
      ),
    );
    await tester.pumpAndSettle();

    await tester.tap(find.text('Состав'));
    await tester.pumpAndSettle();
    expect(find.text('0'), findsOneWidget);

    clipboard.emit('010460711800000521ABCDE');
    await tester.pumpAndSettle();

    await tester.tap(find.text('Состав'));
    await tester.pumpAndSettle();
    expect(find.text('1'), findsOneWidget);
    expect(find.text('12'), findsOneWidget);
  });

  testWidgets('настройка упаковок открывает позицию в режиме «Упаковки»', (tester) async {
    final store = FakeDocumentStore()
      ..names = {sampleGoodsUid: sampleGoodsName}
      ..barcodeToUid = {'4607118000005': sampleGoodsUid};

    Future<void> open({required bool packages}) async {
      final clipboard = _watcher();
      await tester.pumpWidget(
        TsdApp(
          home: MarkCollectionScreen(
            key: ValueKey(packages),
            settings: settingsWithServer(packages: packages),
            store: store,
            document: _documentWithLines([(sampleGoodsUid, 12, 0)]),
            clipboard: clipboard,
          ),
        ),
      );
      await tester.pumpAndSettle();

      clipboard.emit('4607118000005');
      await tester.pumpAndSettle();
    }

    await open(packages: true);
    expect(find.text(packagesMode), findsOneWidget);

    await open(packages: false);
    expect(find.text(unitsAndPackagesMode), findsOneWidget);
  });

  testWidgets('проверка марки идёт через окно ожидания с отсчётом таймаута', (tester) async {
    final store = FakeDocumentStore()
      ..names = {sampleGoodsUid: sampleGoodsName}
      ..barcodeToUid = {'4607118000005': sampleGoodsUid};
    final gate = Completer<MarkCheckAnswer>();
    final online = FakeMarkOnline()..onOwner = (_) => gate.future;
    final clipboard = _watcher();

    await tester.pumpWidget(
      TsdApp(
        home: MarkCollectionScreen(
          settings: settingsWithServer(checkOwner: true),
          store: store,
          online: online,
          document: _documentWithLines([(sampleGoodsUid, 12, 0)]),
          clipboard: clipboard,
        ),
      ),
    );
    await tester.pumpAndSettle();

    clipboard.emit('010460711800000521ABCDE');
    await tester.pump();
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 50));

    expect(find.text('Ожидание...'), findsOneWidget);
    expect(find.text(markCheckCaption), findsOneWidget);
    expect(find.text('Осталось: 10 сек'), findsOneWidget);
    expect(find.byType(CircularProgressIndicator), findsOneWidget);

    gate.complete(const MarkCheckAnswer.passed());
    await tester.pumpAndSettle();

    expect(find.text('Ожидание...'), findsNothing);
    expect((await store.marks('doc-lines')).single.nomenclatureUid, sampleGoodsUid);
  });

  testWidgets('марка пишется на позицию и показывает подобранное', (tester) async {
    final store = FakeDocumentStore()
      ..names = {sampleGoodsUid: sampleGoodsName}
      ..barcodeToUid = {'4607118000005': sampleGoodsUid};
    final clipboard = _watcher();

    await tester.pumpWidget(
      TsdApp(
        home: MarkCollectionScreen(
          settings: settingsWithServer(),
          store: store,
          document: _documentWithLines([(sampleGoodsUid, 12, 0)]),
          clipboard: clipboard,
        ),
      ),
    );
    await tester.pumpAndSettle();

    clipboard.emit('010460711800000521ABCDE');
    await tester.pumpAndSettle();

    expect(find.text(sampleGoodsName), findsOneWidget);
    expect(find.text('1'), findsOneWidget);
    expect((await store.marks('doc-lines')).single.units, 1);
  });

  testWidgets('последняя марка открывает меню с текстом «Все марки подобраны!»', (tester) async {
    final store = FakeDocumentStore()
      ..names = {sampleGoodsUid: sampleGoodsName}
      ..barcodeToUid = {'4607118000005': sampleGoodsUid};
    final clipboard = _watcher();

    await tester.pumpWidget(
      TsdApp(
        home: MarkCollectionScreen(
          settings: settingsWithServer(),
          store: store,
          document: _documentWithLines([(sampleGoodsUid, 1, 0)]),
          clipboard: clipboard,
        ),
      ),
    );
    await tester.pumpAndSettle();

    clipboard.emit('010460711800000521ABCDE');
    await tester.pumpAndSettle();

    expect(find.text(markAllPicked), findsOneWidget);
    // Карточка позиции остаётся на экране: видно, что строка подобрана полностью.
    expect(find.text(sampleGoodsName), findsOneWidget);
    expect(find.text('Подобрано полностью'), findsOneWidget);
    expect(find.text('Всего марок'), findsOneWidget);
  });

  testWidgets('первая марка на всю строку показывает карточку позиции', (tester) async {
    final store = FakeDocumentStore()
      ..names = {sampleGoodsUid: sampleGoodsName}
      ..barcodeToUid = {'4607118000005': sampleGoodsUid}
      ..coefficients['4607118000005'] = 12;
    final clipboard = _watcher();

    await tester.pumpWidget(
      TsdApp(
        home: MarkCollectionScreen(
          settings: settingsWithServer(),
          store: store,
          document: _documentWithLines([(sampleGoodsUid, 12, 0)]),
          clipboard: clipboard,
        ),
      ),
    );
    await tester.pumpAndSettle();

    clipboard.emit('010460711800000521ABCDE');
    await tester.pumpAndSettle();

    expect(find.text(sampleGoodsName), findsOneWidget);
    expect(find.text('Всего марок'), findsOneWidget);
    // Нужно и подобрано по 12: карточки «Всего марок» и «Подобрано» показывают одно число.
    expect(find.text('12'), findsNWidgets(2));
    expect(find.text('Подобрано полностью'), findsOneWidget);
    expect((await store.marks('doc-lines')).single.units, 12);
  });

  testWidgets('подобранная строка состава зелёная с галочкой', (tester) async {
    final store = FakeDocumentStore()
      ..names = {sampleGoodsUid: sampleGoodsName}
      ..barcodeToUid = {'4607118000005': sampleGoodsUid}
      ..coefficients['4607118000005'] = 12;
    final clipboard = _watcher();

    await tester.pumpWidget(
      TsdApp(
        home: MarkCollectionScreen(
          settings: settingsWithServer(),
          store: store,
          document: _documentWithLines([(sampleGoodsUid, 12, 0)]),
          clipboard: clipboard,
        ),
      ),
    );
    await tester.pumpAndSettle();

    await tester.tap(find.text('Состав'));
    await tester.pumpAndSettle();
    expect(tester.widget<Text>(find.text(sampleGoodsName)).style?.color, isNull);

    clipboard.emit('010460711800000521ABCDE');
    await tester.pumpAndSettle();
    await tester.tap(find.text('Состав'));
    await tester.pumpAndSettle();

    expect(tester.widget<Text>(find.text(sampleGoodsName)).style?.color, WebixDarkTheme.ok);
    expect(find.byIcon(Icons.check_circle_outline), findsWidgets);
  });

  testWidgets('выгруженный документ скан не пишет и показывает «Документ уже выгружен»', (tester) async {
    final store = FakeDocumentStore()
      ..names = {sampleGoodsUid: sampleGoodsName}
      ..barcodeToUid = {'4607118000005': sampleGoodsUid};
    final clipboard = _watcher();

    await tester.pumpWidget(
      TsdApp(
        home: MarkCollectionScreen(
          settings: settingsWithServer(),
          store: store,
          document: _uploadedDocument(),
          clipboard: clipboard,
        ),
      ),
    );
    await tester.pumpAndSettle();

    clipboard.emit('010460711800000521ABCDE');
    await tester.pumpAndSettle();

    expect(find.text(markDocumentUploaded), findsOneWidget);
    expect(store.marksByDocument, isEmpty);
  });

  testWidgets('кнопка выгрузки отправляет пакет документа', (tester) async {
    final store = FakeDocumentStore()
      ..names = {sampleGoodsUid: sampleGoodsName}
      ..marksByDocument['doc-1'] = [
        const StoredMark(mark: '010460711800000521ABCDE', nomenclatureUid: sampleGoodsUid),
      ];
    final protocol = DocumentProtocol(
      (barcode) async => const ExchangeCallResult(success: true, body: sampleDocumentResponse),
      onUpload: (_) async => const ExchangeCallResult(
        success: true,
        body: '{"Пакет": {"Результат": false, "Ошибка": "нет товара"}}',
      ),
    );

    await tester.pumpWidget(
      TsdApp(
        home: MarkCollectionScreen(
          settings: settingsWithServer(),
          store: store,
          uploader: MarkUploader(store: store, protocol: (_) => protocol),
          document: _document(),
        ),
      ),
    );
    await tester.pumpAndSettle();

    await tester.tap(find.text('Меню'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Выгрузить документ'));
    await tester.pumpAndSettle();

    expect(protocol.packets, hasLength(1));
    expect(find.text('Обмен закончился неудачей по причиние: нет товара'), findsOneWidget);
  });
}

ClipboardScanWatcher _watcher() {
  return ClipboardScanWatcher(
    interval: const Duration(seconds: 5),
    requireMark: false,
    clearAfterEmit: false,
    readClipboard: () async => '',
    clearClipboard: () async {},
  );
}

ClipboardScanWatcher _clipboard(String code) {
  return ClipboardScanWatcher(
    interval: const Duration(seconds: 5),
    requireMark: false,
    clearAfterEmit: false,
    readClipboard: () async => code,
    clearClipboard: () async {},
  );
}

/// Сохранённый документ из примера пакета.
ScannedDocument _document() {
  return ScannedDocument(
    id: 'doc-1',
    scannedAt: DateTime(2026, 11, 5, 10, 30),
    number: '123',
    counterparty: 'ООО Ромашка',
    presentation: samplePresentation,
    erpUid: sampleErpUid,
    body: parseDocumentPacket(sampleDocumentResponse)!.document,
  );
}

/// Документ с заданным составом: УИД товара, количество и количество марок.
ScannedDocument _documentWithLines(List<(String, num, num)> lines) {
  return ScannedDocument(
    id: 'doc-lines',
    scannedAt: DateTime(2026, 11, 5, 10, 30),
    number: '123',
    counterparty: 'ООО Ромашка',
    presentation: samplePresentation,
    erpUid: sampleErpUid,
    body: {
      'УИД': sampleErpUid,
      'Представление': samplePresentation,
      'Состав': [
        for (final line in lines)
          {'Номенклатура': line.$1, 'Количество': line.$2, 'КоличествоМарок': line.$3},
      ],
    },
  );
}

/// Документ, который уже ушёл на сервер обмена.
ScannedDocument _uploadedDocument() {
  return ScannedDocument(
    id: 'doc-1',
    scannedAt: DateTime(2026, 11, 5, 10, 30),
    number: '123',
    counterparty: 'ООО Ромашка',
    presentation: samplePresentation,
    erpUid: sampleErpUid,
    uploaded: true,
    body: parseDocumentPacket(sampleDocumentResponse)!.document,
  );
}
