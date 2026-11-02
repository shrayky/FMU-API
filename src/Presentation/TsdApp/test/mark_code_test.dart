import 'package:flutter_test/flutter_test.dart';
import 'package:tsd_app/domain/scan/mark_code.dart';

void main() {
  test('нормализует GS и отбрасывает переводы строк', () {
    expect(MarkCode.normalize('01\u241d93\r\n'), '01\x1d93');
  });

  test('отличает марку от штрихкода и текста', () {
    expect(MarkCode.looksLikeMark('010460071404010521AB\x1d93abcd'), isTrue);
    expect(MarkCode.looksLikeMark('\x1d010460071404010521AB\x1d93abcd'), isTrue);
    expect(MarkCode.looksLikeMark('10460071404010521AB\x1d93abcd'), isTrue);
    expect(MarkCode.looksLikeMark('04600714040105N4N57RSCBUZTQ12'), isTrue);
    expect(MarkCode.looksLikeMark('010460071404010521'), isFalse);
    expect(MarkCode.looksLikeMark('010460071404010521\x1d93abcd'), isFalse);
    expect(MarkCode.looksLikeMark('46071180000011'), isFalse);
    expect(MarkCode.looksLikeMark('4607118000001'), isFalse);
    expect(MarkCode.looksLikeMark('1234567890123456789012345'), isFalse);
    expect(MarkCode.looksLikeMark('привет'), isFalse);
    expect(MarkCode.looksLikeMark('123'), isFalse);
  });

  test('короткая марка — серийный номер без криптохвоста', () {
    expect(
      MarkCode.shortMark('010460071404010521N4N57RSCBUZTQ\x1d93dGVz'),
      'N4N57RSCBUZTQ',
    );
  });

  test('без серийного номера оставляет код до GS и обрезает длинный', () {
    expect(MarkCode.shortMark('46071180000011'), '46071180000011');
    expect(MarkCode.shortMark('1234567890123456789012345\x1d93abcd'), '1234567890123456…');
  });
}
