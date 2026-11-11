/// Результат вызова команды обмена. Тело — сырой ответ, разбор делает срез команды.
class ExchangeCallResult {
  const ExchangeCallResult({
    required this.success,
    this.error = '',
    this.body = '',
    this.details = '',
  });

  final bool success;

  /// Текст ошибки для человека.
  final String error;

  final String body;

  /// Сырой текст исключения или ответа: в диалог не попадает, идёт в журнал работы.
  final String details;
}
