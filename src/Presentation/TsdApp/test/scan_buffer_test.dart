import 'package:flutter_test/flutter_test.dart';
import 'package:tsd_app/domain/scan/scan_buffer.dart';

void main() {
  test('символы копятся, GS не завершает скан', () {
    final buffer = ScanBuffer();

    expect(buffer.pushChar('0').code, isNull);
    expect(buffer.pushChar('1').code, isNull);
    expect(buffer.pushChar('\x1d').code, isNull);
    expect(buffer.pushChar('9').code, isNull);
    expect(buffer.current, '01\x1d9');
  });

  test('CR #13 завершает скан и отдаёт код с GS', () {
    final buffer = ScanBuffer();

    for (final ch in '01\x1d93abc'.split('')) {
      expect(buffer.pushChar(ch).code, isNull);
    }

    final result = buffer.pushChar('\r');

    expect(result.code, '01\x1d93abc');
    expect(buffer.current, isEmpty);
  });

  test('CR на пустом буфере не даёт код', () {
    final buffer = ScanBuffer();

    expect(buffer.pushChar('\r').code, isNull);
  });

  test('LF после уже завершённого CR игнорируется', () {
    final buffer = ScanBuffer();
    buffer.pushChar('A');

    expect(buffer.pushChar('\r').code, 'A');
    expect(buffer.pushChar('\n').code, isNull);
  });

  test('интент и вставка завершают скан сразу', () {
    final buffer = ScanBuffer();
    buffer.pushChar('x');

    final result = buffer.completeRaw('01\x1d93xyz');

    expect(result.code, '01\x1d93xyz');
    expect(buffer.current, isEmpty);
  });

  test('пустая вставка не даёт код', () {
    expect(ScanBuffer().completeRaw('   ').code, isNull);
  });
}
