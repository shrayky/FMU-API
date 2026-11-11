import 'package:flutter/material.dart';

import '../../domain/message/app_message.dart';
import '../../theme/webix_dark_theme.dart';

/// Диалог сообщения: иконка по виду, текст и кнопка «Ок».
class MessageDialog extends StatelessWidget {
  const MessageDialog({super.key, required this.message});

  final AppMessage message;

  @override
  Widget build(BuildContext context) {
    return AlertDialog(
      backgroundColor: WebixDarkTheme.dialogSurface,
      icon: Icon(messageIcon(message.kind), color: messageColor(message.kind), size: 32),
      title: message.title.isEmpty ? null : Text(message.title),
      content: Text(message.text),
      actions: [
        TextButton(
          onPressed: () => Navigator.of(context).pop(),
          child: const Text('Ок', style: TextStyle(color: WebixDarkTheme.accent)),
        ),
      ],
    );
  }
}

/// Баннер снизу с той же иконкой и цветом, что у диалога.
class MessageSnackBar extends StatelessWidget {
  const MessageSnackBar({super.key, required this.message});

  final AppMessage message;

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        Icon(messageIcon(message.kind), color: messageColor(message.kind), size: 20),
        const SizedBox(width: 12),
        Expanded(child: Text(message.text)),
      ],
    );
  }
}

IconData messageIcon(MessageKind kind) {
  switch (kind) {
    case MessageKind.success:
      return Icons.check_circle_outline;
    case MessageKind.failure:
      return Icons.error_outline;
    case MessageKind.warning:
      return Icons.warning_amber_rounded;
    case MessageKind.info:
      return Icons.info_outline;
  }
}

Color messageColor(MessageKind kind) {
  switch (kind) {
    case MessageKind.success:
      return WebixDarkTheme.ok;
    case MessageKind.failure:
      return WebixDarkTheme.warn;
    case MessageKind.warning:
      return WebixDarkTheme.warning;
    case MessageKind.info:
      return WebixDarkTheme.accent;
  }
}
