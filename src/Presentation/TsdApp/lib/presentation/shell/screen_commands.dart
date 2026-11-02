import 'package:flutter/material.dart';

import '../../theme/webix_dark_theme.dart';

/// Текстовая команда у нижнего края экрана.
class ScreenCommand {
  const ScreenCommand({
    required this.label,
    required this.onPressed,
    this.accent = false,
  });

  final String label;
  final VoidCallback? onPressed;
  final bool accent;
}

/// Ряд команд у нижней панели.
class ScreenCommands extends StatelessWidget {
  const ScreenCommands({super.key, required this.commands});

  final List<ScreenCommand> commands;

  @override
  Widget build(BuildContext context) {
    return Focus(
      canRequestFocus: false,
      descendantsAreFocusable: false,
      child: Material(
        color: WebixDarkTheme.surface,
        child: DecoratedBox(
          decoration: const BoxDecoration(
            border: Border(top: BorderSide(color: WebixDarkTheme.border)),
          ),
          child: SafeArea(
            top: false,
            child: SizedBox(
              height: 52,
              child: Row(
                mainAxisAlignment: MainAxisAlignment.spaceEvenly,
                children: [
                  for (final command in commands)
                    TextButton(
                      onPressed: command.onPressed,
                      style: TextButton.styleFrom(
                        foregroundColor: command.accent ? WebixDarkTheme.accent : WebixDarkTheme.text,
                        disabledForegroundColor: WebixDarkTheme.muted,
                      ),
                      child: Text(command.label),
                    ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}
