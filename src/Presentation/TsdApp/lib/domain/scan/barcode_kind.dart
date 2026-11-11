import 'mark_code.dart';

/// Вид штрихкода, который экран различает по содержимому.
///
/// Символику (QR или Data Matrix) видит только камера, а интент, клавиатура и буфер
/// отдают строку. Поэтому вид определяется структурой кода и одинаков для любого источника.
enum BarcodeKind {
  /// Штрихкод документа сбора: QR, содержимое которого не марка и не EAN-13.
  document,

  /// Марка GS1 Data Matrix.
  mark,

  /// Линейный штрихкод товара EAN-13.
  ean13;

  /// Подпись вида для сообщения «нужен ...».
  String get title => switch (this) {
        BarcodeKind.document => 'QR код документа',
        BarcodeKind.mark => 'Data Matrix',
        BarcodeKind.ean13 => 'EAN-13',
      };
}

/// Вид кода по содержимому. Марку и EAN-13 узнаём по структуре, остальное считаем документом.
BarcodeKind barcodeKindOf(String code) {
  final normalized = MarkCode.normalize(code);
  if (MarkCode.looksLikeMark(normalized)) {
    return BarcodeKind.mark;
  }
  if (_looksLikeEan13(normalized)) {
    return BarcodeKind.ean13;
  }
  return BarcodeKind.document;
}

/// Штрихкод товара — тринадцать цифр. Всё остальное в документе считается маркой,
/// как в прототипе: он делит коды по длине, а не по структуре GS1.
bool isProductBarcode(String code) => RegExp(r'^\d{13}$').hasMatch(MarkCode.normalize(code));

/// Коды, по которым ищется товар: EAN-13 как есть, у марки — GTIN из идентификатора `01`.
List<String> barcodeLookups(String code) {
  final normalized = MarkCode.normalize(code);
  return switch (barcodeKindOf(normalized)) {
    BarcodeKind.ean13 => [normalized],
    BarcodeKind.mark => _gtins(normalized),
    BarcodeKind.document => const [],
  };
}

/// GTIN марки: четырнадцать цифр после `01`. Справочник хранит и 14 цифр, и EAN-13 без ведущего нуля.
List<String> _gtins(String mark) {
  final head = mark.split('\x1d').first;
  final gtin = RegExp(r'^01(\d{14})').firstMatch(head)?.group(1);
  if (gtin == null) {
    return const [];
  }

  final lookups = <String>{gtin};
  if (gtin.startsWith('0')) {
    lookups.add(gtin.substring(1));
  }
  return lookups.toList();
}

/// EAN-13: тринадцать цифр с верной контрольной суммой.
bool _looksLikeEan13(String code) {
  if (!RegExp(r'^\d{13}$').hasMatch(code)) {
    return false;
  }

  var sum = 0;
  for (var index = 0; index < 12; index += 1) {
    final digit = code.codeUnitAt(index) - 0x30;
    sum += index.isEven ? digit : digit * 3;
  }

  return (10 - sum % 10) % 10 == code.codeUnitAt(12) - 0x30;
}
