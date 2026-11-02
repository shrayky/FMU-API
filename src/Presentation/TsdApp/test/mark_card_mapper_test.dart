import 'package:flutter_test/flutter_test.dart';
import 'package:tsd_app/application/mark/mark_card_mapper.dart';
import 'package:tsd_app/domain/mark/mark_check_result.dart';

void main() {
  test('собирает строки карточки и пропускает пустые поля', () {
    final rows = MarkCardMapper.fromResponses(
      trueApi: {
        'status': 'ok',
        'data': [
          {
            'cisInfo': {
              'productName': 'Молоко',
              'status': 'INTRODUCED',
              'expireDate': '2027-01-15T00:00:00',
              'ownerName': 'ООО Ромашка',
              'ownerInn': '7701234567',
              'brand': 'Простоквашино',
              'producerName': 'Завод',
              'producedDate': '2026-01-10',
              'gtin': '04600714040105',
              'tnVedEaes': '0401',
              'productWeight': 900,
            },
          },
        ],
      },
      permissive: {
        'truemark_response': {
          'codes': [
            {'verified': true},
          ],
        },
      },
    );

    expect(
      rows.map((row) => '${row.label}:${row.value}').toList(),
      [
        'Название:Молоко',
        'Статус:В обороте',
        'Срок годности:15.01.2027',
        'Владелец:ООО Ромашка, ИНН 7701234567',
        'Бренд:Простоквашино',
        'Производитель:Завод',
        'Дата производства:10.01.2026',
        'Криптозащита КМ:Проверено',
        'GTIN:04600714040105',
        'ТН ВЭД:0401',
        'Заявленный объём / вес нетто:900 г',
      ],
    );
    expect(rows.firstWhere((row) => row.label == 'Статус').tone, MarkCardTone.ok);
    expect(rows.firstWhere((row) => row.label == 'Криптозащита КМ').tone, MarkCardTone.ok);
  });

  test('RETIRED и непроверенная криптозащита дают предупреждение', () {
    final rows = MarkCardMapper.fromResponses(
      trueApi: {
        'status': 'ok',
        'data': [
          {
            'cisInfo': {
              'productName': 'Сыр',
              'status': 'RETIRED',
              'expireDate': '2020-01-01',
            },
          },
        ],
      },
      permissive: {
        'truemark_response': {
          'codes': [
            {'verified': false},
          ],
        },
      },
    );

    expect(rows.firstWhere((row) => row.label == 'Статус').value, 'Выбыл');
    expect(rows.firstWhere((row) => row.label == 'Статус').tone, MarkCardTone.warn);
    expect(rows.firstWhere((row) => row.label == 'Криптозащита КМ').value, 'Не проверено');
    expect(rows.firstWhere((row) => row.label == 'Срок годности').tone, MarkCardTone.warn);
  });

  test('ошибки источников попадают в карточку', () {
    final rows = MarkCardMapper.fromResponses(
      trueApi: {'status': 'error', 'reason': 'нет токена'},
      permissive: {'error': 'сеть недоступна'},
    );

    expect(rows.map((row) => row.value), [
      'True API: нет токена',
      'Разрешительный режим: сеть недоступна',
    ]);
    expect(rows.first.tone, MarkCardTone.error);
    expect(rows.last.tone, MarkCardTone.none);
  });

  test('ошибка разрешительного режима не выходит на передний план', () {
    final rows = MarkCardMapper.fromResponses(
      trueApi: {
        'status': 'ok',
        'data': [
          {
            'cisInfo': {
              'productName': 'Молоко',
              'status': 'INTRODUCED',
            },
          },
        ],
      },
      permissive: {'error': 'сеть недоступна'},
    );

    expect(rows.first.label, 'Название');
    expect(rows.last.value, 'Разрешительный режим: сеть недоступна');
    expect(rows.last.tone, MarkCardTone.none);
  });

  test('индикаторы зелёные, когда марка в обороте у своей организации и срок не вышел', () {
    final result = MarkCardMapper.checkResult(
      trueApi: _cis({
        'productName': 'Молоко',
        'status': 'INTRODUCED',
        'expireDate': '2027-01-15T00:00:00',
        'ownerInn': '7701234567',
      }),
      permissive: null,
      organisationInn: ' 7701234567 ',
    );

    expect(result.name, 'Молоко');
    expect(result.status, MarkSignalTone.ok);
    expect(result.owner, MarkSignalTone.ok);
    expect(result.expiry, MarkSignalTone.ok);
    expect(result.rows.firstWhere((row) => row.label == 'Название').value, 'Молоко');
  });

  test('чужой владелец, выбытие и просрочка дают красные индикаторы', () {
    final result = MarkCardMapper.checkResult(
      trueApi: _cis({
        'productName': 'Сыр',
        'status': 'RETIRED',
        'expireDate': '2020-01-01',
        'ownerInn': '7701234567',
      }),
      permissive: null,
      organisationInn: '0000000000',
    );

    expect(result.name, 'Сыр');
    expect(result.status, MarkSignalTone.bad);
    expect(result.owner, MarkSignalTone.bad);
    expect(result.expiry, MarkSignalTone.bad);
  });

  test('ошибка True API красит все индикаторы', () {
    final result = MarkCardMapper.checkResult(
      trueApi: {'status': 'error', 'reason': 'нет токена'},
      permissive: null,
      organisationInn: '7701234567',
    );

    expect(result.name, 'True API: нет токена');
    expect(result.status, MarkSignalTone.bad);
    expect(result.owner, MarkSignalTone.bad);
    expect(result.expiry, MarkSignalTone.bad);
  });
}

Map<String, dynamic> _cis(Map<String, dynamic> info) {
  return {
    'status': 'ok',
    'data': [
      {'cisInfo': info},
    ],
  };
}
