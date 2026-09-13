import 'package:flutter/material.dart';

/// Тёмная тема в цветах Webix Dark.
class WebixDarkTheme {
  static const background = Color(0xFF2A2B2D);
  static const surface = Color(0xFF20262B);
  static const border = Color(0x14FFFFFF);
  static const text = Color(0xE6FFFFFF);
  static const muted = Color(0xFF9CA3AF);
  static const ok = Color(0xFF4ADE80);
  static const warn = Color(0xFFF87171);
  static const accent = Color(0xFF60A5FA);

  static ThemeData data() {
    final base = ThemeData(
      brightness: Brightness.dark,
      useMaterial3: true,
      fontFamily: 'sans-serif',
    );

    return base.copyWith(
      scaffoldBackgroundColor: background,
      colorScheme: const ColorScheme.dark(
        surface: background,
        primary: accent,
        onSurface: text,
        error: warn,
      ),
      appBarTheme: const AppBarTheme(
        backgroundColor: surface,
        foregroundColor: text,
        elevation: 0,
      ),
      inputDecorationTheme: const InputDecorationTheme(
        labelStyle: TextStyle(color: muted),
        hintStyle: TextStyle(color: muted),
        enabledBorder: UnderlineInputBorder(borderSide: BorderSide(color: border)),
        focusedBorder: UnderlineInputBorder(borderSide: BorderSide(color: accent)),
      ),
    );
  }
}
