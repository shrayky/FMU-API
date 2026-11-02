/// Имена протоколов на форме сервера обмена.
const defaultExchangeProtocol = 'DS T\u0421Д';
const dataMobileProtocol = 'Data Mobile';
const exchangeProtocolNames = <String>[defaultExchangeProtocol, dataMobileProtocol];

/// Сервер обмена, выбранный в основных настройках.
class ExchangeServer {
  const ExchangeServer({
    required this.id,
    this.name = '',
    this.host = '',
    this.port = 0,
    this.secure = false,
    this.publication = '',
    this.user = '',
    this.password = '',
    this.timeoutSeconds = 0,
    this.protocol = defaultExchangeProtocol,
  });

  final String id;
  final String name;
  final String host;
  final int port;
  final bool secure;
  final String publication;
  final String user;
  final String password;
  final int timeoutSeconds;
  final String protocol;

  String get endpoint {
    if (host.isEmpty && publication.isEmpty && port == 0) {
      return '';
    }
    return '$host:$port/$publication';
  }

  Map<String, Object?> toJson() {
    return {
      'id': id,
      'name': name,
      'host': host,
      'port': port,
      'secure': secure,
      'publication': publication,
      'user': user,
      'password': password,
      'timeoutSeconds': timeoutSeconds,
      'protocol': protocol,
    };
  }

  static ExchangeServer? fromJson(Object? json) {
    if (json is! Map) {
      return null;
    }

    final id = json['id'];
    if (id is! String || id.isEmpty) {
      return null;
    }

    return ExchangeServer(
      id: id,
      name: _text(json['name']),
      host: _text(json['host']),
      port: _port(json['port']),
      secure: json['secure'] is bool ? json['secure'] as bool : false,
      publication: _text(json['publication']),
      user: _text(json['user']),
      password: _text(json['password']),
      timeoutSeconds: _port(json['timeoutSeconds']),
      protocol: _text(json['protocol']).isEmpty ? defaultExchangeProtocol : _text(json['protocol']),
    );
  }

  static String _text(Object? value) => value is String ? value : '';

  static int _port(Object? value) {
    if (value is int) {
      return value;
    }
    if (value is num) {
      return value.toInt();
    }
    if (value is String) {
      return int.tryParse(value.trim()) ?? 0;
    }
    return 0;
  }
}
