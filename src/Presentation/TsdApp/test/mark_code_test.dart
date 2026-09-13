import 'package:flutter_test/flutter_test.dart';
import 'package:tsd_app/domain/scan/mark_code.dart';

void main() {
  test('нормализует GS и отбрасывает переводы строк', () {
    expect(MarkCode.normalize('01\u241d93\r\n'), '01\x1d93');
  });

  test('отличает марку от обычного текста', () {
    expect(MarkCode.looksLikeMark('010460071404010521AB\x1d93abcd'), isTrue);
    expect(MarkCode.looksLikeMark('010460071404010521'), isTrue);
    expect(MarkCode.looksLikeMark('46071180000011'), isTrue);
    expect(MarkCode.looksLikeMark('привет'), isFalse);
    expect(MarkCode.looksLikeMark('123'), isFalse);
  });
}
