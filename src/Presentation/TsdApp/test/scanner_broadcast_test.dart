import 'package:flutter_test/flutter_test.dart';
import 'package:tsd_app/infrastructure/scan/broadcast_monitor_channel.dart';
import 'package:tsd_app/infrastructure/scan/scanner_broadcast.dart';

void main() {
  test('выбирает известный extra ключ штрихкода', () {
    expect(
      ScannerBroadcast.guessExtraKey({
        'from': 'scanner',
        'barcode': '0101',
      }),
      'barcode',
    );
    expect(
      ScannerBroadcast.guessExtraKey({
        'SCAN_BARCODE1': 'abc',
      }),
      'SCAN_BARCODE1',
    );
  });

  test('без известных ключей берёт первый extra', () {
    expect(ScannerBroadcast.guessExtraKey({'foo': '1'}), 'foo');
    expect(ScannerBroadcast.guessExtraKey({}), 'barcode');
  });

  test('берёт ATOL extra', () {
    expect(
      ScannerBroadcast.guessExtraKey({
        'EXTRA_BARCODE_DECODING_DATA': '4600',
      }),
      'EXTRA_BARCODE_DECODING_DATA',
    );
  });

  test('Honeywell: decode_rslt важнее special_keys', () {
    expect(
      ScannerBroadcast.guessExtraKey({
        'special_keys': '9,10,13',
        'decode_rslt': '0104607003502720',
      }),
      'decode_rslt',
    );
  });

  test('не выбирает special_keys как extra штрихкода', () {
    expect(
      ScannerBroadcast.guessExtraKey({
        'special_keys': '9,10,13',
      }),
      'barcode',
    );
  });

  test('ACTION_DECODE_DATA: barcode, не length и barcodeType', () {
    expect(
      ScannerBroadcast.guessExtraKey({
        'length': '31',
        'barcode': '0104607003502720215(IPaf93j0WM',
        'barcode_string': '0104607003502720215(IPaf93j0WM',
        'barcodeType': '119',
      }),
      'barcode',
    );
  });

  test('не выбирает length и barcodeType как extra штрихкода', () {
    expect(
      ScannerBroadcast.guessExtraKey({
        'length': '31',
        'barcodeType': '119',
      }),
      'barcode',
    );
  });

  test('событие монитора не отбрасывает пустой action', () {
    final event = BroadcastEvent.fromArguments({
      'action': '',
      'extras': {'barcode': '1'},
    });
    expect(event, isNotNull);
    expect(event!.extras['barcode'], '1');
  });
}
