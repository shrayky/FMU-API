import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:tsd_app/domain/documents/document_packet.dart';
import 'package:tsd_app/domain/documents/scanned_document.dart';
import 'package:tsd_app/infrastructure/settings/app_settings.dart';
import 'package:tsd_app/main.dart';
import 'package:tsd_app/presentation/mark_collection/mark_collection_screen.dart';
import 'package:tsd_app/presentation/mark_collection/mark_journal_screen.dart';

import 'documents_support.dart';

void main() {
  testWidgets('пустой журнал показывает заглушку и срезает старые документы', (tester) async {
    final store = FakeDocumentStore();

    await _open(tester, store);

    expect(find.text('Нет данных для отображения'), findsOneWidget);
    expect(store.trims, [30]);
  });

  testWidgets('строка журнала показывает документ, номер и дату скана', (tester) async {
    final store = FakeDocumentStore()
      ..rows.add(_document(presentation: samplePresentation, number: '123'));

    await _open(tester, store);

    expect(find.text(samplePresentation), findsOneWidget);
    expect(find.text('123'), findsOneWidget);
    expect(find.text('05.11.2026'), findsOneWidget);
  });

  testWidgets('поиск фильтрует по документу, номеру и контрагенту без учёта регистра', (tester) async {
    final store = FakeDocumentStore()
      ..rows.addAll([
        _document(presentation: 'Сбор марок № 123', number: '123', counterparty: 'ООО Ромашка'),
        _document(presentation: 'Сбор марок № 456', number: '456', counterparty: 'ИП Петров'),
      ]);

    await _open(tester, store);

    await tester.enterText(find.byType(TextField), 'петров');
    await tester.pumpAndSettle();

    expect(find.text('Сбор марок № 456'), findsOneWidget);
    expect(find.text('Сбор марок № 123'), findsNothing);

    await tester.enterText(find.byType(TextField), '123');
    await tester.pumpAndSettle();

    expect(find.text('Сбор марок № 123'), findsOneWidget);
    expect(find.text('Сбор марок № 456'), findsNothing);

    await tester.enterText(find.byType(TextField), 'марка');
    await tester.pumpAndSettle();

    expect(find.text('Сбор марок № 123'), findsNothing);
    expect(find.text('Нет данных для отображения'), findsOneWidget);
  });

  testWidgets('поиск находит документ, которого нет в начале списка', (tester) async {
    final store = FakeDocumentStore();
    for (var index = 0; index < 600; index += 1) {
      store.rows.add(_document(presentation: 'Сбор марок № $index', number: '$index'));
    }
    store.rows.add(
      _document(presentation: 'Старый сбор', number: '1', counterparty: 'ИП Петров'),
    );

    await _open(tester, store);

    await tester.enterText(find.byType(TextField), 'петров');
    await tester.pumpAndSettle();

    expect(store.queries.last, 'петров');
    expect(find.text('Старый сбор'), findsOneWidget);
  });

  testWidgets('строка открывает сохранённый документ', (tester) async {
    final store = FakeDocumentStore()
      ..names = {sampleGoodsUid: sampleGoodsName}
      ..rows.add(_document(presentation: samplePresentation, number: '123'));

    await _open(tester, store);

    await tester.tap(find.text(samplePresentation));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Состав'));
    await tester.pumpAndSettle();

    expect(find.byType(MarkCollectionScreen), findsOneWidget);
    expect(find.text(sampleGoodsName), findsOneWidget);
    expect(find.text('12'), findsOneWidget);
    expect(store.saved, isEmpty);
  });

  testWidgets('плюс открывает пустой сбор', (tester) async {
    final store = FakeDocumentStore()
      ..rows.add(_document(presentation: samplePresentation, number: '123'));

    await _open(tester, store);

    await tester.tap(find.byIcon(Icons.add));
    await tester.pumpAndSettle();

    expect(find.byType(MarkCollectionScreen), findsOneWidget);
    expect(find.text(markCollectionHint), findsOneWidget);
  });

  testWidgets('срок хранения из настроек уходит в срез при открытии', (tester) async {
    final store = FakeDocumentStore();

    await _open(tester, store, settings: settingsWithServer(retentionDays: 0));

    expect(store.trims, [0]);
  });

  testWidgets('выгруженный документ подписан, остальные строки пустые', (tester) async {
    final store = FakeDocumentStore()
      ..rows.addAll([
        _document(presentation: 'Сбор марок № 1', number: '1', uploaded: true),
        _document(presentation: 'Сбор марок № 2', number: '2'),
      ]);

    await _open(tester, store);

    expect(find.text(uploadMark), findsOneWidget);
    expect(find.text('Сбор марок № 1'), findsOneWidget);
    expect(find.text('Сбор марок № 2'), findsOneWidget);
  });
}

Future<void> _open(WidgetTester tester, FakeDocumentStore store, {AppSettings? settings}) async {
  await tester.pumpWidget(
    TsdApp(
      home: MarkJournalScreen(
        settings: settings ?? settingsWithServer(),
        store: store,
      ),
    ),
  );
  await tester.pumpAndSettle();
}

ScannedDocument _document({
  required String presentation,
  required String number,
  String counterparty = 'ООО Ромашка',
  bool uploaded = false,
}) {
  return ScannedDocument(
    id: '$presentation-$number',
    scannedAt: DateTime(2026, 11, 5, 10, 30),
    number: number,
    counterparty: counterparty,
    presentation: presentation,
    erpUid: sampleErpUid,
    uploaded: uploaded,
    body: parseDocumentPacket(sampleDocumentResponse)!.document,
  );
}
