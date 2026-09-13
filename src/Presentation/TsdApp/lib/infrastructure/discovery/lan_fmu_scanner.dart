import 'dart:convert';
import 'dart:io';

/// Ищет Fmu-Api в подсети Wi-Fi короткими GET на порт 2578.
class LanFmuScanner {
  static const aboutPath = '/api/configuration/About';

  LanFmuScanner({
    this.port = 2578,
    this.timeout = const Duration(milliseconds: 300),
    this.concurrency = 32,
    Future<List<InternetAddress>> Function()? listAddresses,
    this._probe,
  }) : _listAddresses = listAddresses ?? _defaultAddresses;

  final int port;
  final Duration timeout;
  final int concurrency;
  final Future<List<InternetAddress>> Function() _listAddresses;
  final Future<bool> Function(Uri uri)? _probe;
  bool _cancelled = false;

  static bool looksLikeFmu(String body) => body.contains('FMU-API');

  void cancel() {
    _cancelled = true;
  }

  Future<String?> findFirst() async {
    _cancelled = false;
    final hosts = <String>[];
    for (final address in await _listAddresses()) {
      hosts.addAll(_hostsFor(address));
    }
    if (hosts.isEmpty) {
      return null;
    }

    String? found;
    Future<void> worker() async {
      while (!_cancelled && found == null && hosts.isNotEmpty) {
        final host = hosts.removeLast();
        final uri = Uri(scheme: 'http', host: host, port: port, path: aboutPath);
        final ok = await (_probe ?? _httpProbe)(uri);
        if (ok) {
          found = 'http://$host:$port';
        }
      }
    }

    final workers = concurrency.clamp(1, hosts.length);
    await Future.wait(List.generate(workers, (_) => worker()));
    return found;
  }

  Iterable<String> _hostsFor(InternetAddress address) {
    if (address.type != InternetAddressType.IPv4) {
      return const [];
    }

    final parts = address.address.split('.').map(int.parse).toList();
    if (!_isPrivate(parts)) {
      return const [];
    }

    final prefix = '${parts[0]}.${parts[1]}.${parts[2]}';
    return [
      for (var i = 1; i <= 254; i++)
        if (i != parts[3]) '$prefix.$i',
    ];
  }

  static bool _isPrivate(List<int> octets) {
    if (octets.length != 4) {
      return false;
    }

    if (octets[0] == 10) {
      return true;
    }

    if (octets[0] == 192 && octets[1] == 168) {
      return true;
    }

    return octets[0] == 172 && octets[1] >= 16 && octets[1] <= 31;
  }

  Future<bool> _httpProbe(Uri uri) async {
    final client = HttpClient();
    client.connectionTimeout = timeout;
    try {
      final request = await client.getUrl(uri).timeout(timeout);
      final response = await request.close().timeout(timeout);
      final body = await utf8.decoder.bind(response).join().timeout(timeout);
      return response.statusCode == 200 && looksLikeFmu(body);
    } catch (_) {
      return false;
    } finally {
      client.close(force: true);
    }
  }

  static Future<List<InternetAddress>> _defaultAddresses() async {
    final interfaces = await NetworkInterface.list(
      includeLinkLocal: false,
      type: InternetAddressType.IPv4,
    );
    return [for (final iface in interfaces) ...iface.addresses];
  }
}
