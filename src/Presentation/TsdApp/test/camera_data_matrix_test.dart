import 'package:flutter_test/flutter_test.dart';
import 'package:tsd_app/infrastructure/scan/camera_data_matrix.dart';

void main() {
  test('берёт код из rawValue', () {
    expect(
      CameraDataMatrix.codeFrom(rawValue: '01gtin93serial'),
      '01gtin93serial',
    );
  });

  test('из байтов сохраняет GS', () {
    expect(
      CameraDataMatrix.codeFrom(rawBytes: [0x30, 0x31, 0x1d, 0x39, 0x33]),
      '01\x1d93',
    );
  });

  test('если в байтах есть GS, они важнее rawValue без GS', () {
    expect(
      CameraDataMatrix.codeFrom(
        rawValue: '0193',
        rawBytes: [0x30, 0x31, 0x1d, 0x39, 0x33],
      ),
      '01\x1d93',
    );
  });

  test('пустой результат не даёт код', () {
    expect(CameraDataMatrix.codeFrom(rawValue: '  ', rawBytes: const []), isNull);
  });
}
