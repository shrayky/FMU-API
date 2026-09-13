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

  test('событие монитора не отбрасывает пустой action', () {
    final event = BroadcastEvent.fromArguments({
      'action': '',
      'extras': {'barcode': '1'},
    });
    expect(event, isNotNull);
    expect(event!.extras['barcode'], '1');
  });
}
