/// Вид сообщения пользователю: успех, ошибка, предупреждение или справка.
enum MessageKind { success, failure, warning, info }

/// Сообщение пользователю: готовый текст, вид и JSON-подробности для журнала.
class AppMessage {
  const AppMessage({
    required this.kind,
    required this.text,
    this.title = '',
    this.data = const {},
  });

  final MessageKind kind;
  final String text;
  final String title;

  /// Структурные подробности: адрес, сырой ответ, исключение. В диалог не попадают.
  final Map<String, Object?> data;
}
