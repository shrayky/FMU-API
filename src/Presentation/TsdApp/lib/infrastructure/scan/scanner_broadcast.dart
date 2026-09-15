/// Известные broadcast-action сканеров ТСД и выбор extra-ключа.
class ScannerBroadcast {
  static const knownActions = [
    'nlscan.action.SCANNER_RESULT',
    'android.intent.ACTION_DECODE_DATA',
    'android.intent.ACTION_DECODE',
    'com.android.server.scannerservice.broadcast',
    'android.intent.action.SCANRESULT',
    'android.intent.action.RECEIVE_SCAN_RESULT',
    'android.intent.action.BARCODE',
    'android.intent.action.DECODE_DATA',
    'com.symbol.datawedge.api.RESULT_ACTION',
    'com.honeywell.aidc.action.ACTION_BARCODE_DATA',
    'com.honeywell.intent.action.SCAN_RESULT',
    'scan.rcv.message',
    'com.scanner.broadcast',
    'device.scanner.ACTION',
    'device.scanner.EVENT',
    'com.rscja.scanner.action.SCAN_RESULT',
    'com.ubx.decoder.broadcast.SCAN',
    'com.cipherlab.barcodebaseapi.ACTION_BARCODE_DATA',
    'ru.atol.scanner.action.SCAN',
    'com.xcheng.scanner.action.BARCODE_DECODING_BROADCAST',
    'com.sunmi.scanner.ACTION_DATA_CODE_RECEIVED',
    'com.android.scanner.broadcast',
    'scanner.action.BARCODE',
    'com.zebra.scanner.ACTION',
  ];

  static const preferredExtras = [
    'barcode',
    'barcode_string',
    'data',
    'SCAN_BARCODE1',
    'SCANNER_RESULT',
    'EXTRA_BARCODE_DECODING_DATA',
    'com.symbol.datawedge.data_string',
    'data_string',
    'barcode_data',
    'value',
    'scannerdata',
    'decode_rslt',
    'decode_data',
    'decode_data_disp',
  ];

  static const ignoredExtras = {
    'special_keys',
    'charset',
    'codeId',
    'aimId',
    'timestamp',
    'version',
    'length',
    'barcodeType',
    'barcode_type',
  };

  static String guessExtraKey(Map<String, String> extras) {
    for (final key in preferredExtras) {
      if (extras.containsKey(key) && !_isIgnored(key)) {
        return key;
      }
    }

    for (final key in extras.keys) {
      if (!_isIgnored(key)) {
        return key;
      }
    }

    return 'barcode';
  }

  static bool _isIgnored(String key) => ignoredExtras.contains(key);
}
