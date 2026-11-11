import '../exchange/exchange_cancellation.dart';

/// Марка этого товара чужая: владелец в True API не совпал с нашей организацией.
const markOwnerForeign = 'Марка этого товара не принадлежит нам, ее нельзя подобрать!';

/// Ответ на команду статуса марки не разобрался.
const markStatusBadFormat = 'Неверный формат ответа на команду проверки статуса марки!';

/// Токен True API пришёл с истёкшим сроком.
const markTokenExpired = 'Срок полученого токена истек!';

/// Проверка марки прервана пользователем.
const markCheckCancelled = 'Проверка марки отменена';

/// Ответ сетевой проверки марки: пустой [error] — проверка пройдена.
class MarkCheckAnswer {
  const MarkCheckAnswer.passed() : error = '';
  const MarkCheckAnswer.rejected(this.error);

  final String error;

  bool get passed => error.isEmpty;
}

/// Ответ на команду `СтатусСбораМарки`: разрешение добавить марку или текст отказа.
class MarkStatusAnswer {
  const MarkStatusAnswer.passed() : error = '';
  const MarkStatusAnswer.rejected(this.error);

  final String error;

  bool get passed => error.isEmpty;
}

/// Токен True API и срок его действия.
class TrueApiTokenAnswer {
  const TrueApiTokenAnswer({required this.token, this.expiresAt, this.error = ''});

  final String token;
  final DateTime? expiresAt;
  final String error;

  bool get passed => error.isEmpty;
}

/// Пакет марки по True API: код упаковки и марки внутри неё.
class MarkPackageAnswer {
  const MarkPackageAnswer({this.package = '', this.marks = const [], this.error = ''});

  final String package;

  /// Марки упаковки из истории агрегации: их подбор пишет все сразу.
  final List<String> marks;

  final String error;

  bool get passed => error.isEmpty;
  bool get hasPackage => package.isNotEmpty;
}

/// Сетевые проверки подбора марки: владелец, статус, токен и упаковка по True API.
abstract interface class MarkOnline {
  /// `POST {адрес Fmu-Api}/api/ts/checkMark`.
  Future<MarkCheckAnswer> owner(String mark, ExchangeCancellation cancellation);

  /// `GET ...dstsdapi/get?Команда=СтатусСбораМарки&Марка={марка}`.
  Future<MarkStatusAnswer> status(String mark, ExchangeCancellation cancellation);

  /// Токен True API: по ИНН организации документа, иначе общий.
  ///
  /// Адрес проверки марок задан — токен берётся у него; иначе у сервера обмена.
  Future<TrueApiTokenAnswer> token(ExchangeCancellation cancellation, {String inn = ''});

  /// Упаковка марки по True API: `cises/info` и история агрегации.
  Future<MarkPackageAnswer> package(
    String mark,
    String token,
    ExchangeCancellation cancellation,
  );
}
