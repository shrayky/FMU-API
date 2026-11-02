import 'package:flutter/material.dart';

import '../../domain/mark/mark_card_row.dart';
import '../../theme/webix_dark_theme.dart';
import 'mark_cards_view.dart';

/// Полная карточка марки из непрерывного сканирования.
class MarkDetailPage extends StatelessWidget {
  const MarkDetailPage({super.key, required this.rows});

  final List<MarkCardRow> rows;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Марка')),
      body: ColoredBox(
        color: WebixDarkTheme.background,
        child: MarkCardsView(rows: rows, placeholder: 'Нет данных'),
      ),
    );
  }
}
