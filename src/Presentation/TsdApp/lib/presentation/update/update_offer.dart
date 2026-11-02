import 'package:flutter/material.dart';

import '../../theme/webix_dark_theme.dart';

/// Полоса с предложением установить более новую сборку.
class UpdateOffer extends StatelessWidget {
  const UpdateOffer({
    super.key,
    required this.versionName,
    required this.busy,
    required this.onUpdate,
  });

  final String versionName;
  final bool busy;
  final VoidCallback onUpdate;

  @override
  Widget build(BuildContext context) {
    return Material(
      color: WebixDarkTheme.surface,
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 4),
        child: Row(
          children: [
            Expanded(child: Text('Доступна версия $versionName')),
            TextButton(
              onPressed: busy ? null : onUpdate,
              child: Text(busy ? 'Загрузка…' : 'Обновить'),
            ),
          ],
        ),
      ),
    );
  }
}

/// Строка на главном экране, когда нода не ответила на проверку обновления.
class UpdateCheckFailure extends StatelessWidget {
  const UpdateCheckFailure({super.key});

  @override
  Widget build(BuildContext context) {
    return const Material(
      color: WebixDarkTheme.surface,
      child: Padding(
        padding: EdgeInsets.symmetric(horizontal: 16, vertical: 4),
        child: Row(
          children: [
            Expanded(child: Text('Не удалось проверить обновление')),
          ],
        ),
      ),
    );
  }
}
