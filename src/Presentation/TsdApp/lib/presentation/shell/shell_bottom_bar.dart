import 'package:flutter/material.dart';

import '../../theme/webix_dark_theme.dart';

/// Пункт нижней панели.
class ShellBarItem {
  const ShellBarItem({
    required this.label,
    required this.icon,
    required this.onTap,
    this.selected = false,
  });

  final String label;
  final IconData icon;
  final VoidCallback onTap;
  final bool selected;
}

/// Нижняя панель с иконкой и подписью.
class ShellBottomBar extends StatelessWidget {
  const ShellBottomBar({super.key, required this.items});

  final List<ShellBarItem> items;

  @override
  Widget build(BuildContext context) {
    return Material(
      color: WebixDarkTheme.surface,
      child: DecoratedBox(
        decoration: const BoxDecoration(
          border: Border(top: BorderSide(color: WebixDarkTheme.border)),
        ),
        child: SafeArea(
          top: false,
          child: SizedBox(
            height: 62,
            child: Row(
              children: [
                for (final item in items) Expanded(child: _ShellBarButton(item: item)),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

/// Кнопка пункта нижней панели.
class _ShellBarButton extends StatelessWidget {
  const _ShellBarButton({required this.item});

  final ShellBarItem item;

  @override
  Widget build(BuildContext context) {
    final color = item.selected ? WebixDarkTheme.accent : WebixDarkTheme.muted;
    return InkWell(
      onTap: item.onTap,
      canRequestFocus: false,
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Icon(item.icon, color: color, size: 22),
          const SizedBox(height: 2),
          Text(
            item.label,
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
            textAlign: TextAlign.center,
            style: TextStyle(color: color, fontSize: 11),
          ),
        ],
      ),
    );
  }
}
