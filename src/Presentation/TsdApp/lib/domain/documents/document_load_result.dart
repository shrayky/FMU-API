import 'scanned_document.dart';

/// Итог загрузки документа по штрихкоду.
class DocumentLoadResult {
  const DocumentLoadResult.saved(ScannedDocument this.document)
      : error = '',
        details = '',
        blocked = false;

  const DocumentLoadResult.failed(this.error, {this.details = ''})
      : document = null,
        blocked = false;

  const DocumentLoadResult.blocked(this.error)
      : document = null,
        details = '',
        blocked = true;

  final ScannedDocument? document;

  /// Текст ошибки для человека.
  final String error;

  /// Сырой ответ или исключение: в диалог не попадает, идёт в журнал работы.
  final String details;

  /// Запрос не ушёл в сеть: сервер, адрес или протокол не готовы.
  final bool blocked;
}
