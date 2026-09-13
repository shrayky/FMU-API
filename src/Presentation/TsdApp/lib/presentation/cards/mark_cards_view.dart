import 'package:flutter/material.dart';

import '../../domain/mark/mark_card_row.dart';
import '../../theme/webix_dark_theme.dart';

/// Карточки сведений о марке в стиле Webix Dark.
class MarkCardsView extends StatelessWidget {
  const MarkCardsView({
    super.key,
    required this.rows,
    this.placeholder,
  });

  final List<MarkCardRow> rows;
  final String? placeholder;

  @override
  Widget build(BuildContext context) {
    if (rows.isEmpty) {
      return Center(
        child: Text(
          placeholder ?? 'Сканируйте марку',
          style: const TextStyle(
            color: WebixDarkTheme.text,
            fontSize: 16,
          ),
          textAlign: TextAlign.center,
        ),
      );
    }

    return ListView.separated(
      padding: const EdgeInsets.symmetric(vertical: 8),
      itemCount: rows.length,
      separatorBuilder: (_, _) => const Divider(height: 1, color: WebixDarkTheme.border),
      itemBuilder: (context, index) {
        final row = rows[index];
        final color = switch (row.tone) {
          MarkCardTone.ok => WebixDarkTheme.ok,
          MarkCardTone.warn || MarkCardTone.error => WebixDarkTheme.warn,
          MarkCardTone.none => WebixDarkTheme.text,
        };

        if (row.label.isEmpty) {
          return Padding(
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
            child: Text(row.value, style: TextStyle(color: color, fontSize: 14)),
          );
        }

        return Padding(
          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(row.label, style: const TextStyle(color: WebixDarkTheme.muted, fontSize: 13)),
              const SizedBox(height: 4),
              Text(
                row.value,
                style: TextStyle(color: color, fontSize: 16, height: 1.4),
              ),
            ],
          ),
        );
      },
    );
  }
}
