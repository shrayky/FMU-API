/// Текст сбоя для человека: таймаут, сеть, TLS и код ответа сервера.
///
/// Сырой текст исключения в диалог не попадает: подробность остаётся в журнале работы.
String describeFailure(Object error) => describeFailureText('$error');

String describeFailureText(String raw) {
  final text = raw.trim();
  if (text.isEmpty) {
    return 'Неизвестная ошибка';
  }

  if (text.contains('TimeoutException')) {
    final timeout = _timeout(text);
    return timeout == null ? 'Сервер не ответил за отведённое время' : describeTimeout(timeout);
  }
  if (text.contains('Failed host lookup')) {
    return 'Сервер не найден: проверьте IP-адрес и сеть';
  }
  if (text.contains('Connection refused')) {
    return 'Сервер отклонил соединение: проверьте порт и запущен ли сервер';
  }
  if (text.contains('Network is unreachable')) {
    return 'Сеть недоступна: проверьте подключение';
  }
  if (text.contains('Connection timed out') || text.contains('Operation timed out')) {
    return 'Сервер не ответил: проверьте сеть и адрес';
  }
  if (text.contains('Connection reset') || text.contains('Broken pipe')) {
    return 'Соединение с сервером прервано';
  }
  if (text.contains('Connection closed before full header')) {
    return 'Сервер закрыл соединение до ответа';
  }
  if (text.contains('HandshakeException') || text.contains('CERTIFICATE_VERIFY_FAILED')) {
    return 'Ошибка защищённого соединения: проверьте сертификат сервера';
  }
  if (text.contains('Invalid port') || text.contains('Invalid URI')) {
    return 'Некорректный адрес сервера';
  }
  return text;
}

/// Длительность в коротком виде: «500 мс», «10 сек», «1 мин 30 сек», «2 ч 5 мин».
String describeTimeout(Duration duration) {
  final milliseconds = duration.inMilliseconds.abs();
  if (milliseconds < 1000) {
    return 'Сервер не ответил за $milliseconds мс';
  }

  final hours = duration.inHours;
  final minutes = duration.inMinutes.remainder(60);
  final seconds = duration.inSeconds.remainder(60);
  if (hours > 0) {
    final rest = minutes > 0 ? ' $minutes мин' : '';
    return 'Сервер не ответил за $hours ч$rest';
  }
  if (minutes > 0) {
    final rest = seconds > 0 ? ' $seconds сек' : '';
    return 'Сервер не ответил за $minutes мин$rest';
  }
  return 'Сервер не ответил за $seconds сек';
}

/// Код ответа не 200 и первая строка тела: тело целиком уходит в журнал работы.
String describeHttpStatus(int statusCode, String body) {
  final lines = body
      .split('\n')
      .map((line) => line.trim())
      .where((line) => line.isNotEmpty)
      .toList();
  if (lines.isEmpty) {
    return 'Сервер ответил кодом $statusCode';
  }

  final line = lines.first;
  final short = line.length > 200 ? '${line.substring(0, 200)}…' : line;
  return 'Сервер ответил кодом $statusCode: $short';
}

Duration? _timeout(String text) {
  final match = RegExp(r'TimeoutException after (-?\d+):(\d{2}):(\d{2})(?:\.(\d+))?').firstMatch(text);
  if (match == null) {
    return null;
  }

  final microseconds = int.parse((match.group(4) ?? '0').padRight(6, '0').substring(0, 6));
  return Duration(
    hours: int.parse(match.group(1)!),
    minutes: int.parse(match.group(2)!),
    seconds: int.parse(match.group(3)!),
    microseconds: microseconds,
  );
}
