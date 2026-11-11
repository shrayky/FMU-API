import 'dart:async';

import 'package:flutter_test/flutter_test.dart';
import 'package:tsd_app/domain/message/failure_text.dart';

void main() {
  test('таймаут показывается в секундах без микросекунд', () {
    final error = TimeoutException('Future not completed', const Duration(seconds: 10));

    expect(describeFailure(error), 'Сервер не ответил за 10 сек');
  });

  test('минуты и часы таймаута не теряются', () {
    expect(
      describeFailure(TimeoutException('later', const Duration(minutes: 1, seconds: 30))),
      'Сервер не ответил за 1 мин 30 сек',
    );
    expect(
      describeFailure(TimeoutException('later', const Duration(hours: 2, minutes: 5))),
      'Сервер не ответил за 2 ч 5 мин',
    );
  });

  test('таймаут меньше секунды показывается миллисекундами', () {
    expect(
      describeFailure(TimeoutException('later', const Duration(milliseconds: 500))),
      'Сервер не ответил за 500 мс',
    );
  });

  test('таймаут без длительности не показывает технический текст', () {
    expect(describeFailure(TimeoutException('Future not completed')), 'Сервер не ответил за отведённое время');
  });

  test('сетевые сбои переводятся на русский', () {
    expect(
      describeFailureText("SocketException: Failed host lookup: '10.0.0.5' (OS Error: ...)"),
      'Сервер не найден: проверьте IP-адрес и сеть',
    );
    expect(
      describeFailureText('SocketException: Connection refused (OS Error: Connection refused, errno = 111)'),
      'Сервер отклонил соединение: проверьте порт и запущен ли сервер',
    );
    expect(
      describeFailureText('HandshakeException: Handshake error in client (OS Error: CERTIFICATE_VERIFY_FAILED)'),
      'Ошибка защищённого соединения: проверьте сертификат сервера',
    );
  });

  test('неизвестный текст ошибки остаётся как есть', () {
    expect(describeFailureText('boom'), 'boom');
    expect(describeFailureText('   '), 'Неизвестная ошибка');
  });

  test('код ответа и первая строка тела попадают в текст ошибки', () {
    expect(describeHttpStatus(503, 'нет'), 'Сервер ответил кодом 503: нет');
    expect(describeHttpStatus(401, ''), 'Сервер ответил кодом 401');
    expect(
      describeHttpStatus(500, 'x' * 300),
      'Сервер ответил кодом 500: ${'x' * 200}…',
    );
  });
}
