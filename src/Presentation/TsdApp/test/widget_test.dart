import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:tsd_app/infrastructure/discovery/lan_fmu_scanner.dart';
import 'package:tsd_app/infrastructure/update/app_update_client.dart';
import 'package:tsd_app/infrastructure/update/app_update_flow.dart';
import 'package:tsd_app/infrastructure/update/app_update_installer.dart';
import 'package:tsd_app/main.dart';
import 'package:tsd_app/presentation/home/home_screen.dart';

void main() {
  testWidgets('главный экран без адреса ищет Fmu-Api', (tester) async {
    SharedPreferences.setMockInitialValues({});
    await tester.pumpWidget(
      TsdApp(
        home: HomeScreen(
          appVersion: '1.1.0',
          lanScanner: LanFmuScanner(listAddresses: () async => []),
        ),
      ),
    );
    await tester.pump();
    await tester.idle();
    await tester.pump();

    expect(find.text('Укажите адрес Fmu-Api в настройках'), findsOneWidget);
    expect(find.text('Fmu-Api v 1.1.0'), findsOneWidget);
    expect(find.text('Сбор марок'), findsOneWidget);
  });

  testWidgets('сбой проверки обновления на меню не показывается', (tester) async {
    SharedPreferences.setMockInitialValues({'apiBaseUrl': 'http://10.0.0.2:2578'});
    await tester.pumpWidget(
      TsdApp(
        home: HomeScreen(
          appVersion: '1.1.0',
          lanScanner: LanFmuScanner(listAddresses: () async => []),
          updates: AppUpdateFlow(
            client: AppUpdateClient(
              httpClient: MockClient((request) async => http.Response('no', 500)),
            ),
            installer: AppUpdateInstaller(abiReader: () async => 'arm64-v8a'),
            currentBuildNumber: '1',
          ),
        ),
      ),
    );
    await tester.pump();
    await tester.idle();
    await tester.pump();

    expect(find.text('Не удалось проверить обновление'), findsNothing);
    expect(find.text('Сбор марок'), findsOneWidget);
  });
}
