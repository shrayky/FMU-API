import 'package:flutter/material.dart';

import '../../theme/webix_dark_theme.dart';

/// Значок журнала: блокнот с тремя строками и закладкой снизу.
class JournalIcon extends StatelessWidget {
  const JournalIcon({super.key, this.color = WebixDarkTheme.accent, this.size = 24});

  final Color color;
  final double size;

  @override
  Widget build(BuildContext context) {
    return CustomPaint(
      size: Size.square(size),
      painter: _JournalPainter(color),
    );
  }
}

/// Рисунок блокнота с тремя строками и закладкой.
class _JournalPainter extends CustomPainter {
  const _JournalPainter(this.color);

  final Color color;

  @override
  void paint(Canvas canvas, Size size) {
    final stroke = Paint()
      ..color = color
      ..style = PaintingStyle.stroke
      ..strokeWidth = 1.6;
    canvas.drawRRect(
      RRect.fromRectAndRadius(
        Rect.fromLTWH(5, 2, size.width - 10, size.height - 8),
        const Radius.circular(2),
      ),
      stroke,
    );

    final line = Paint()
      ..color = color
      ..strokeWidth = 1.4
      ..strokeCap = StrokeCap.round;
    for (var i = 0; i < 3; i++) {
      final y = 7.0 + i * 4.2;
      canvas.drawLine(Offset(8, y), Offset(size.width - 8, y), line);
    }

    final bookmark = Path()
      ..moveTo(size.width / 2 - 4, size.height - 8)
      ..lineTo(size.width / 2, size.height - 2)
      ..lineTo(size.width / 2 + 4, size.height - 8)
      ..close();
    canvas.drawPath(bookmark, Paint()..color = color);
  }

  @override
  bool shouldRepaint(covariant _JournalPainter oldDelegate) => oldDelegate.color != color;
}
