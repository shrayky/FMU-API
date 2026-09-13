import 'dart:io';

import 'package:flutter_test/flutter_test.dart';
import 'package:tsd_app/infrastructure/discovery/lan_fmu_scanner.dart';

void main() {
  test('сканирует /24 и возвращает первый живой Fmu-Api', () async {
    final probed = <String>[];
    final scanner = LanFmuScanner(
      listAddresses: () async => [InternetAddress('192.168.1.10')],
      probe: (uri) async {
        probed.add(uri.toString());
        return uri.host == '192.168.1.5';
      },
      concurrency: 16,
    );

    expect(await scanner.findFirst(), 'http://192.168.1.5:2578');
    expect(probed, contains('http://192.168.1.5:2578/api/configuration/About'));
    expect(probed.any((uri) => uri.contains('://192.168.1.10:')), isFalse);
    expect(probed.any((uri) => uri.contains('://192.168.1.0:')), isFalse);
    expect(probed.any((uri) => uri.contains('://192.168.1.255:')), isFalse);
  });

  test('игнорирует loopback и публичные адреса', () async {
    final probed = <String>[];
    final scanner = LanFmuScanner(
      listAddresses: () async => [
        InternetAddress('127.0.0.1'),
        InternetAddress('8.8.8.8'),
      ],
      probe: (uri) async {
        probed.add(uri.host);
        return false;
      },
    );

    expect(await scanner.findFirst(), isNull);
    expect(probed, isEmpty);
  });

  test('тело About с FMU-API считается своим сервером', () {
    expect(LanFmuScanner.looksLikeFmu('FMU-API version 1 assembly 2'), isTrue);
    expect(LanFmuScanner.looksLikeFmu('nginx'), isFalse);
  });
}
