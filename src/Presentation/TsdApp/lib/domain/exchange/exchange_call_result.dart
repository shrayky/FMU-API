/// Результат вызова команды обмена. Тело — сырой ответ, разбор делает срез команды.
class ExchangeCallResult {
  const ExchangeCallResult({
    required this.success,
    this.error = '',
    this.body = '',
  });

  final bool success;
  final String error;
  final String body;
}
