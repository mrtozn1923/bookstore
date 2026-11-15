import 'package:material_ui/material_ui.dart';

import 'bookstore_colors.dart';

abstract final class AppTheme {
  /// The one color the whole palette is generated from.
  static const seed = Color(0xFF6B4E2E);

  static ThemeData light = _build(Brightness.light, BookstoreColors.light);
  static ThemeData dark = _build(Brightness.dark, BookstoreColors.dark);

  static ThemeData _build(Brightness brightness, BookstoreColors colors) {
    final scheme = ColorScheme.fromSeed(
      seedColor: seed,
      brightness: brightness,
    );
    return ThemeData(
      colorScheme: scheme,
      extensions: [colors],
      cardTheme: const CardThemeData(margin: EdgeInsets.symmetric(vertical: 4)),
      chipTheme: ChipThemeData(side: BorderSide(color: scheme.outlineVariant)),
    );
  }
}
