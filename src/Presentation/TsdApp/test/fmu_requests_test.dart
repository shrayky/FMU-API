import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:tsd_app/infrastructure/api/fmu_requests.dart';

void main() {
  test('документ check кодирует марку в base64 с GS', () {
    const mark = '01gtin\x1d93crypto';
    final body = FmuRequests.documentCheck(inn: '7701234567', mark: mark);

    expect(body['action'], 'check');
    expect(body['type'], 'receipt');
    final position = (body['positions'] as List).first as Map<String, dynamic>;
    expect(position['organisation'], {'inn': '7701234567'});
    expect(
      utf8.decode(base64Decode((position['marking_codes'] as List).first as String)),
      mark,
    );
  });

  test('cises/info передаёт ИНН и сырую марку', () {
    const mark = '01gtin\x1d93crypto';
    expect(FmuRequests.cisesInfo(inn: '1', mark: mark), {
      'inn': '1',
      'cises': [mark],
    });
  });

  test('берёт ИНН первой организации', () {
    expect(
      FmuRequests.innFromOrganisationConfig({
        'printGroups': [
          {'inn': '7701234567'},
          {'inn': '7800000000'},
        ],
      }),
      '7701234567',
    );
    expect(FmuRequests.innFromOrganisationConfig({'printGroups': []}), '');
  });
}
