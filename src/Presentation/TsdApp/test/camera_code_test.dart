import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_scanner/mobile_scanner.dart';
import 'package:tsd_app/domain/scan/barcode_kind.dart';
import 'package:tsd_app/infrastructure/scan/camera_code.dart';
import 'package:tsd_app/presentation/mark_collection/mark_collection_screen.dart';
import 'package:tsd_app/presentation/scan/camera_scan_page.dart';

void main() {
  test('берёт код из rawValue', () {
    expect(
      CameraCode.codeFrom(rawValue: '01gtin93serial'),
      '01gtin93serial',
    );
  });

  test('из байтов сохраняет GS', () {
    expect(
      CameraCode.codeFrom(rawBytes: [0x30, 0x31, 0x1d, 0x39, 0x33]),
      '01\x1d93',
    );
  });

  test('если в байтах есть GS, они важнее rawValue без GS', () {
    expect(
      CameraCode.codeFrom(
        rawValue: '0193',
        rawBytes: [0x30, 0x31, 0x1d, 0x39, 0x33],
      ),
      '01\x1d93',
    );
  });

  test('пустой результат не даёт код', () {
    expect(CameraCode.codeFrom(rawValue: '  ', rawBytes: const []), isNull);
  });

  test('каждый вид штрихкода получает свой формат камеры', () {
    expect(cameraFormatOf(BarcodeKind.document), BarcodeFormat.qrCode);
    expect(cameraFormatOf(BarcodeKind.mark), BarcodeFormat.dataMatrix);
    expect(cameraFormatOf(BarcodeKind.ean13), BarcodeFormat.ean13);
  });

  test('до загрузки документа камера читает QR, после — Data Matrix и EAN-13', () {
    expect(collectionCameraKinds(documentLoaded: false), [BarcodeKind.document]);
    expect(collectionCameraKinds(documentLoaded: true), [BarcodeKind.mark, BarcodeKind.ean13]);
  });
}
