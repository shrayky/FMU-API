import 'package:flutter/material.dart';

import '../../theme/webix_dark_theme.dart';

/// Кнопка камеры справа над нижней панелью.
class CameraScanButton extends StatelessWidget {
  const CameraScanButton({super.key, required this.onPressed, required this.heroTag});

  final VoidCallback onPressed;
  final Object heroTag;

  @override
  Widget build(BuildContext context) {
    return FloatingActionButton(
      heroTag: heroTag,
      onPressed: onPressed,
      backgroundColor: WebixDarkTheme.accent,
      foregroundColor: WebixDarkTheme.text,
      child: const Icon(Icons.qr_code_scanner),
    );
  }
}
