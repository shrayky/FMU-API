import '../scan/mark_code.dart';

/// Подобранная марка документа: строка таблицы `marks` с ключом `document_id` и `cis`.
class StoredMark {
  const StoredMark({
    required this.mark,
    required this.nomenclatureUid,
    this.units = 1,
  });

  /// Текст марки в виде базы и обмена: разделитель GS записан как `\u001d`.
  final String mark;

  /// УИД товара, чью строку состава закрывает марка.
  final String nomenclatureUid;

  /// Сколько единиц товара принесла марка: палета, упаковка или штучная.
  final int units;

  /// Код идентификации без криптохвоста: по нему марка сравнивается и удаляется.
  String get cis => markCis(mark);
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
