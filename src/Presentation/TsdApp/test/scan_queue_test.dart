import 'package:flutter_test/flutter_test.dart';
import 'package:tsd_app/application/mark/scan_queue.dart';
import 'package:tsd_app/domain/mark/mark_card_row.dart';
import 'package:tsd_app/domain/mark/mark_check_result.dart';

void main() {
  test('новая марка встаёт первой, повтор не добавляется', () {
    final queue = ScanQueue();

    queue.add('11111111111111');
    queue.add('22222222222222\r\n');
    final duplicate = queue.add('22222222222222');

    expect(duplicate, isNull);
    expect(queue.items.map((item) => item.mark), ['22222222222222', '11111111111111']);
    expect(queue.add(' \n'), isNull);
  });

  test('одновременно уходит не больше трёх запросов', () {
    final queue = ScanQueue();
    final first = queue.add('11111111111111')!;
    queue.add('22222222222222');
    queue.add('33333333333333');
    queue.add('44444444444444');

    final started = queue.takeForRequest();

    expect(started.map((item) => item.mark), ['44444444444444', '33333333333333', '22222222222222']);
    expect(first.phase, ScanItemPhase.queued);
    expect(queue.takeForRequest(), isEmpty);

    queue.complete(started.first, _result);
    final next = queue.takeForRequest();

    expect(next, [first]);
    expect(first.phase, ScanItemPhase.running);
  });

  test('ответ и ошибка заполняют строку', () {
    final queue = ScanQueue();
    final item = queue.add('010460071404010521N4N57RSCBUZTQ')!;

    expect(item.shortMark, 'N4N57RSCBUZTQ');
    expect(item.canOpen, isFalse);

    queue.complete(item, _result);

    expect(item.phase, ScanItemPhase.ready);
    expect(item.name, 'Молоко');
    expect(item.status, MarkSignalTone.ok);
    expect(item.expiry, MarkSignalTone.bad);
    expect(item.canOpen, isTrue);

    queue.fail(item, 'сеть');

    expect(item.phase, ScanItemPhase.failed);
    expect(item.name, 'сеть');
    expect(item.status, MarkSignalTone.bad);
    expect(item.owner, MarkSignalTone.bad);
    expect(item.expiry, MarkSignalTone.bad);
    expect(item.rows.single.value, 'сеть');
  });

  test('очистка убирает строки', () {
    final queue = ScanQueue();
    queue.add('11111111111111');

    queue.clear();

    expect(queue.items, isEmpty);
  });
}

const _result = MarkCheckResult(
  rows: [MarkCardRow(label: 'Название', value: 'Молоко')],
  name: 'Молоко',
  status: MarkSignalTone.ok,
  owner: MarkSignalTone.ok,
  expiry: MarkSignalTone.bad,
);
