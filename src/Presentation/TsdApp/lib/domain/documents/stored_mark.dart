import '../scan/mark_code.dart';

/// Подобранная марка документа: строка таблицы `marks` с ключом `document_id` и `cis`.
class StoredMark {
  const StoredMark({
    required this.mark,
    required this.nomenclatureUid,
    this.units = 1,
    this.quantity = 1,
    this.price,
    this.name = '',
  });

  /// Текст марки в виде базы и обмена: разделитель GS записан как `\u001d`.
  final String mark;

  /// УИД товара, чью строку состава закрывает марка.
  final String nomenclatureUid;

  /// Сколько единиц товара принесла марка: палета, упаковка или штучная.
  final int units;

  /// Количество к списанию: килограммы весового товара или штуки.
  final num quantity;

  /// Цена из Frontol: оператор может поправить её перед выгрузкой.
  final num? price;

  /// Название товара из Честного знака: хранится, чтобы не спрашивать снова.
  final String name;

  /// Код идентификации без криптохвоста: по нему марка сравнивается и удаляется.
  String get cis => markCis(mark);

  StoredMark copyWith({num? quantity, num? price, String? name}) {
    return StoredMark(
      mark: mark,
      nomenclatureUid: nomenclatureUid,
      units: units,
      quantity: quantity ?? this.quantity,
      price: price ?? this.price,
      name: name ?? this.name,
    );
  }
}

/// Текст марки для записи: символ GS (код 29) заменяется на `\u001d`.
///
/// Так марка переживает JSON и SQLite, а сервер обмена разворачивает запись обратно.
String markStorageText(String code) => MarkCode.normalize(code).replaceAll('\x1d', r'\u001d');

/// Ключ марки для базы и сравнения: код идентификации без криптохвоста.
///
/// Марка со сканера приходит с хвостом, а True API знает только короткий КИ:
/// без общего ключа одна единица попадала бы в документ дважды.
String markCis(String code) => MarkCode.cis(code);
