import 'package:flutter/services.dart';

import '../../domain/message/app_message.dart';

/// Звук и вибрация сообщения: ошибка — сигнал и сильная вибрация, предупреждение — средняя.
class MessageEffects {
  const MessageEffects();

  void play(MessageKind kind) {
    switch (kind) {
      case MessageKind.failure:
        SystemSound.play(SystemSoundType.alert);
        HapticFeedback.heavyImpact();
      case MessageKind.warning:
        HapticFeedback.mediumImpact();
      case MessageKind.success:
        SystemSound.play(SystemSoundType.click);
      case MessageKind.info:
        break;
    }
  }
}
