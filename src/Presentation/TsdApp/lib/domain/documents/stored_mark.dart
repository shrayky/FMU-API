import '../scan/mark_code.dart';

/// Подобранная марка документа: строка таблицы `marks` с ключом `document_id` и `mark`.
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
}

/// Текст марки для записи: символ GS (код 29) заменяется на `\u001d`.
///
/// Так марка переживает JSON и SQLite, а сервер обмена разворачивает запись обратно.
String markStorageText(String code) => MarkCode.normalize(code).replaceAll('\x1d', r'\u001d');
