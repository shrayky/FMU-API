import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:tsd_app/application/mark/scan_queue.dart';
import 'package:tsd_app/domain/mark/mark_card_row.dart';
import 'package:tsd_app/presentation/cards/mark_detail_page.dart';
import 'package:tsd_app/presentation/scan/continuous_scan_table.dart';
import 'package:tsd_app/theme/webix_dark_theme.dart';

void main() {
  testWidgets('нажатие на готовую строку открывает карточку', (tester) async {
    final item = ScanQueueItem('010460071404010521N4N57RSCBUZTQ', 'N4N57RSCBUZTQ')
      ..phase = ScanItemPhase.ready
      ..name = 'Молоко'
      ..rows = const [MarkCardRow(label: 'Название', value: 'Молоко')];

    await tester.pumpWidget(
      MaterialApp(
        theme: WebixDarkTheme.data(),
        home: Builder(
          builder: (context) => ContinuousScanTable(
            items: [item],
            placeholder: 'Сканируйте марку',
            onOpen: (row) {
              Navigator.of(context).push(
                MaterialPageRoute(builder: (_) => MarkDetailPage(rows: row.rows)),
              );
            },
          ),
        ),
      ),
    );

    expect(find.text('N4N57RSCBUZTQ'), findsNothing);
    await tester.tap(find.text('Молоко'));
    await tester.pumpAndSettle();

    expect(find.byType(BackButton), findsOneWidget);
    expect(find.text('Название'), findsOneWidget);
    expect(find.text('Молоко'), findsWidgets);
  });

  testWidgets('пока идёт проверка виден код марки и индикатор запроса', (tester) async {
    final item = ScanQueueItem('010460071404010521N4N57RSCBUZTQ', 'N4N57RSCBUZTQ');

    await tester.pumpWidget(
      MaterialApp(
        theme: WebixDarkTheme.data(),
        home: ContinuousScanTable(
          items: [item],
          placeholder: 'Сканируйте марку',
          onOpen: (_) {},
        ),
      ),
    );

    expect(find.text('N4N57RSCBUZTQ'), findsOneWidget);
    expect(find.byType(CircularProgressIndicator), findsOneWidget);
  });
}
