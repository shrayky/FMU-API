import 'dart:convert';
import 'dart:io';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:tsd_app/infrastructure/update/app_update_client.dart';
import 'package:tsd_app/infrastructure/update/app_update_flow.dart';
import 'package:tsd_app/infrastructure/update/app_update_installer.dart';

void main() {
  test('пакет новее текущей сборки предлагается к установке', () async {
    final flow = AppUpdateFlow(
      client: AppUpdateClient(
        httpClient: MockClient((request) async {
          return http.Response(
            jsonEncode({
              'versionName': '1.0.3',
              'versionCode': 4,
              'sha256': 'abc',
              'url': '/api/tsd/update/apk?abi=arm64-v8a',
            }),
            200,
          );
        }),
      ),
      installer: AppUpdateInstaller(abiReader: () async => 'arm64-v8a'),
      currentBuildNumber: '3',
    );

    final offer = await flow.check('http://10.0.0.2:2578');

    expect(offer?.versionName, '1.0.3');
    expect(offer?.versionCode, 4);
  });

  test('та же сборка обновлением не считается', () async {
    final flow = AppUpdateFlow(
      client: AppUpdateClient(
        httpClient: MockClient((request) async {
          return http.Response(
            jsonEncode({
              'versionName': '1.0.2',
              'versionCode': 3,
              'sha256': 'abc',
              'url': '/api/tsd/update/apk?abi=arm64-v8a',
            }),
            200,
          );
        }),
      ),
      installer: AppUpdateInstaller(abiReader: () async => 'arm64-v8a'),
      currentBuildNumber: '3',
    );

    expect(await flow.check('http://10.0.0.2:2578'), isNull);
  });

  test('отсутствие пакета на ноде не ошибка', () async {
    final flow = AppUpdateFlow(
      client: AppUpdateClient(httpClient: MockClient((request) async => http.Response('missing', 404))),
      installer: AppUpdateInstaller(abiReader: () async => 'arm64-v8a'),
      currentBuildNumber: '3',
    );

    expect(await flow.check('http://10.0.0.2:2578'), isNull);
  });

  test('файл с чужой суммой не устанавливается', () async {
    final bytes = utf8.encode('apk');
    final directory = Directory.systemTemp.createTempSync('tsd-update');
    var installed = false;
    final flow = AppUpdateFlow(
      client: AppUpdateClient(
        httpClient: MockClient((request) async {
          if (request.url.path.endsWith('/apk')) {
            return http.Response.bytes(bytes, 200);
          }
          return http.Response(
            jsonEncode({
              'versionName': '1.0.3',
              'versionCode': 4,
              'sha256': 'dead',
              'url': '/api/tsd/update/apk?abi=arm64-v8a',
            }),
            200,
          );
        }),
      ),
      installer: AppUpdateInstaller(
        abiReader: () async => 'arm64-v8a',
        directoryReader: () async => directory.path,
        installPackage: (_) async {
          installed = true;
        },
      ),
      currentBuildNumber: '3',
    );

    final offer = await flow.check('http://10.0.0.2:2578');
    await expectLater(flow.install('http://10.0.0.2:2578', offer!), throwsA(isA<Exception>()));
    expect(installed, isFalse);
    expect(directory.listSync(), isEmpty);
    directory.deleteSync(recursive: true);
  });
}
