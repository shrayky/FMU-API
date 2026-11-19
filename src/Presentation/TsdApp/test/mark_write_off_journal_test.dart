import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:tsd_app/domain/documents/scanned_document.dart';
import 'package:tsd_app/infrastructure/settings/app_settings.dart';
import 'package:tsd_app/main.dart';
import 'package:tsd_app/presentation/mark_write_off/mark_write_off_journal_screen.dart';
import 'package:tsd_app/presentation/mark_write_off/mark_write_off_screen.dart';

import 'documents_support.dart';

void main() {
  testWidgets('журнал показывает только документы списания и режет старые', (tester) async {
    final store = FakeDocumentStore()
      ..rows.addAll([
        _document(presentation: 'Сбор марок № 1', kind: markCollectionKind),
        _document(presentation: 'Списание № 2', kind: writeOffKind),
      ]);

    await _open(tester, store);

    expect(find.text('Списание № 2'), findsOneWidget);
    expect(find.text('Сбор марок № 1'), findsNothing);
    expect(store.trims, [30]);
  });

  testWidgets('«Выгружен» стоит только у строки с отметкой выгрузки', (tester) async {
    final store = FakeDocumentStore()
      ..rows.addAll([
        _document(presentation: 'Списание № 1', kind: writeOffKind, uploaded: true),
        _document(presentation: 'Списание № 2', kind: writeOffKind),
      ]);

    await _open(tester, store);

    expect(find.text(writeOffUploadedMark), findsOneWidget);
    expect(find.text('Списание № 1'), findsOneWidget);
    expect(find.text('Списание № 2'), findsOneWidget);
  });

  testWidgets('поиск фильтрует документы списания по представлению и номеру', (tester) async {
    final store = FakeDocumentStore()
      ..rows.addAll([
        _document(presentation: 'Списание № 123', number: '123', kind: writeOffKind),
        _document(presentation: 'Списание № 456', number: '456', kind: writeOffKind),
      ]);

    await _open(tester, store);

    await tester.enterText(find.byType(TextField), '456');
    await tester.pumpAndSettle();

    expect(find.text('Списание № 456'), findsOneWidget);
    expect(find.text('Списание № 123'), findsNothing);
  });

  testWidgets('«+» при пустом адресе список не удлиняет', (tester) async {
    final store = FakeDocumentStore();

    await _open(tester, store, settings: const AppSettings());

    await tester.tap(find.byIcon(Icons.add));
    await tester.pumpAndSettle();

    expect(find.text(writeOffNoAddress), findsOneWidget);
    expect(store.writeOffs, isEmpty);

    await tester.tap(find.text('Выход'));
    await tester.pumpAndSettle();

    expect(find.text('Нет данных для отображения'), findsOneWidget);
    expect(store.writeOffs, isEmpty);
  });

  testWidgets('строка журнала открывает сохранённый документ без создания нового', (tester) async {
    final store = FakeDocumentStore()
      ..rows.add(_document(presentation: 'Списание № 7', kind: writeOffKind));

    await _open(tester, store);

    await tester.tap(find.text('Списание № 7'));
    await tester.pumpAndSettle();

    expect(find.byType(MarkWriteOffScreen), findsOneWidget);
    expect(find.text('Тип:'), findsOneWidget);
    expect(find.text('05.11.2026'), findsOneWidget);
    expect(store.writeOffs, isEmpty);
  });
}

Future<void> _open(WidgetTester tester, FakeDocumentStore store, {AppSettings? settings}) async {
  await tester.pumpWidget(
    TsdApp(
      home: MarkWriteOffJournalScreen(
        settings: settings ?? const AppSettings(apiBaseUrl: 'http://10.0.0.5:5000'),
        store: store,
      ),
    ),
  );
  await tester.pumpAndSettle();
}

ScannedDocument _document({
  required String presentation,
  required String kind,
  String number = '1',
  bool uploaded = false,
}) {
  return ScannedDocument(
    id: '$presentation-$number',
    scannedAt: DateTime(2026, 11, 5, 10, 30),
    number: number,
    counterparty: 'ООО Ромашка',
    presentation: presentation,
    uploaded: uploaded,
    kind: kind,
    body: const {'reason': packingReason},
  );
}
