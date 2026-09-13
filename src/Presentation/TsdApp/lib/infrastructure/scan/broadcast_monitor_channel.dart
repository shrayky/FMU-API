import 'package:flutter/services.dart';

/// Одно пойманное сообщение сканера (broadcast, буфер или клавиатура).
class BroadcastEvent {
  const BroadcastEvent({
    required this.action,
    required this.extras,
    required this.at,
  });

  final String action;
  final Map<String, String> extras;
  final DateTime at;

  static BroadcastEvent? fromArguments(dynamic arguments) {
    if (arguments is! Map) {
      return null;
    }

    final raw = Map<Object?, Object?>.from(arguments);
    final extrasRaw = raw['extras'];
    final extras = <String, String>{};
    if (extrasRaw is Map) {
      extrasRaw.forEach((key, value) {
        extras[key.toString()] = value?.toString() ?? '';
      });
    }

    return BroadcastEvent(
      action: (raw['action'] ?? '').toString(),
      extras: extras,
      at: DateTime.now(),
    );
  }
}

/// Канал мониторинга broadcast-интентов сканера ТСД.
class BroadcastMonitorChannel {
  BroadcastMonitorChannel({
    MethodChannel? channel,
  }) : _channel = channel ?? const MethodChannel('ru.fmuapi.tsd/broadcast-monitor');

  final MethodChannel _channel;
  void Function(BroadcastEvent event)? onEvent;

  Future<int> start(List<String> actions) async {
    _channel.setMethodCallHandler((call) async {
      if (call.method != 'onBroadcast') {
        return;
      }

      final event = BroadcastEvent.fromArguments(call.arguments);
      if (event == null) {
        return;
      }

      onEvent?.call(event);
    });

    final count = await _channel.invokeMethod<int>('startMonitor', {'actions': actions});
    return count ?? 0;
  }

  Future<void> stop() async {
    _channel.setMethodCallHandler(null);
    try {
      await _channel.invokeMethod('stopMonitor');
    } catch (_) {}
  }
}
