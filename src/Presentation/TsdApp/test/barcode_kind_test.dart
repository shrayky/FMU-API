import 'package:flutter_test/flutter_test.dart';
import 'package:tsd_app/domain/scan/barcode_kind.dart';

const _mark = '010460071404010521AB\x1d93abcd';
const _ean13 = '4607118000005';
const _document = 'DOC-2026-000123';

void main() {
  test('марка GS1 Data Matrix узнаётся по структуре', () {
    expect(barcodeKindOf(_mark), BarcodeKind.mark);
    expect(barcodeKindOf('04600714040105N4N57RSCBUZTQ12'), BarcodeKind.mark);
  });

  test('EAN-13 узнаётся по тринадцати цифрам с верной контрольной суммой', () {
    expect(barcodeKindOf(_ean13), BarcodeKind.ean13);
    expect(barcodeKindOf('4607118000001'), BarcodeKind.document);
    expect(barcodeKindOf('123456789012345'), BarcodeKind.document);
  });

  test('остальное считается QR документа', () {
    expect(barcodeKindOf(_document), BarcodeKind.document);
    expect(barcodeKindOf('https://erp.example.com/doc/123'), BarcodeKind.document);
    expect(barcodeKindOf('привет'), BarcodeKind.document);
  });

  test('подпись вида идёт в сообщение о неверном типе', () {
    expect(BarcodeKind.document.title, 'QR код документа');
    expect(BarcodeKind.mark.title, 'Data Matrix');
    expect(BarcodeKind.ean13.title, 'EAN-13');
  });

  test('товар — ровно тринадцать цифр, остальное марка', () {
    expect(isProductBarcode(_ean13), isTrue);
    expect(isProductBarcode('4607118000001'), isTrue);
    expect(isProductBarcode(_mark), isFalse);
    expect(isProductBarcode('0104600714040105'), isFalse);
    expect(isProductBarcode(_document), isFalse);
  });

  test('товар ищется по EAN-13 и по GTIN марки', () {
    expect(barcodeLookups(_ean13), [_ean13]);
    expect(
      barcodeLookups('010460711800000521ABC123\x1d93xyz'),
      containsAll(['04607118000005', _ean13]),
    );
    expect(barcodeLookups(_document), isEmpty);
  });
}
