enum MarkCardTone { none, ok, warn, error }

/// Строка карточки сведений о марке.
class MarkCardRow {
  const MarkCardRow({
    required this.label,
    required this.value,
    this.tone = MarkCardTone.none,
  });

  final String label;
  final String value;
  final MarkCardTone tone;
}
