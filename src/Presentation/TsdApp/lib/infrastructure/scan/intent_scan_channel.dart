import 'package:flutter/services.dart';

/// Приём штрихкода из broadcast-интента сканера ТСД.
class IntentScanChannel {
  IntentScanChannel({
    MethodChannel? channel,
  }) : _channel = channel ?? const MethodChannel('ru.fmuapi.tsd/scan');

  final MethodChannel _channel;
  void Function(String code)? onScan;

  void start() {
    _channel.setMethodCallHandler((call) async {
      if (call.method == 'onScan' && call.arguments is String) {
        final code = (call.arguments as String).trim();
        if (code.isNotEmpty) {
          onScan?.call(code);
        }
      }
    });
  }

  Future<void> configure({
    required String action,
    required String extra,
  }) {
    return _channel.invokeMethod('configure', {
      'action': action,
      'extra': extra,
    });
  }

  void stop() {
    _channel.setMethodCallHandler(null);
  }
}
