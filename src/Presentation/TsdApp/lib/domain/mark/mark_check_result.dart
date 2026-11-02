import 'mark_card_row.dart';

/// Цвет индикатора в таблице непрерывного сканирования.
enum MarkSignalTone { pending, ok, bad }

/// Карточка марки и три индикатора проверки.
class MarkCheckResult {
  const MarkCheckResult({
    required this.rows,
    required this.name,
    required this.status,
    required this.owner,
    required this.expiry,
  });

  final List<MarkCardRow> rows;
  final String name;
  final MarkSignalTone status;
  final MarkSignalTone owner;
  final MarkSignalTone expiry;
}
