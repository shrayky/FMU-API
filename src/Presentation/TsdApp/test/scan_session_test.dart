import 'package:flutter_test/flutter_test.dart';
import 'package:tsd_app/infrastructure/settings/app_settings.dart';
import 'package:tsd_app/presentation/scan/scan_session.dart';

void main() {
  test('сессия отдаёт экрану любой код, тип проверяет экран', () {
    final codes = <String>[];
    final session = ScanSession(
      settings: const AppSettings(),
      onCode: codes.add,
    );

    session.submit('010460071404010521AB\x1d93abcd');
    session.submit('4607118000005');
    session.submit('DOC-2026-000123');
    session.submit('привет');

    expect(codes, [
      '010460071404010521AB\x1d93abcd',
      '4607118000005',
      'DOC-2026-000123',
      'привет',
    ]);
  });

  test('пустой код не отдаётся', () {
    final codes = <String>[];
    final session = ScanSession(
      settings: const AppSettings(),
      onCode: codes.add,
    );

    session.submit('');

    expect(codes, isEmpty);
  });
}
