import 'package:material_ui/material_ui.dart';

/// Bookstore's own color tokens that ColorScheme has no slot for.
@immutable
class BookstoreColors extends ThemeExtension<BookstoreColors> {
  const BookstoreColors({required this.price, required this.ebookBadge});

  final Color price;
  final Color ebookBadge;

  static const light = BookstoreColors(
    price: Color(0xFF1B6B3A),
    ebookBadge: Color(0xFF7A4FB5),
  );

  static const dark = BookstoreColors(
    price: Color(0xFF8FD8A6),
    ebookBadge: Color(0xFFD3B8FF),
  );

  /// Falls back to the defaults when a theme without the extension is used
  /// (a test or a preview that builds its own MaterialApp).
  static BookstoreColors of(BuildContext context) {
    final theme = Theme.of(context);
    return theme.extension<BookstoreColors>() ??
        (theme.brightness == Brightness.dark ? dark : light);
  }

  @override
  BookstoreColors copyWith({Color? price, Color? ebookBadge}) =>
      BookstoreColors(
        price: price ?? this.price,
        ebookBadge: ebookBadge ?? this.ebookBadge,
      );

  @override
  BookstoreColors lerp(BookstoreColors? other, double t) {
    if (other == null) return this;
    return BookstoreColors(
      price: Color.lerp(price, other.price, t)!,
      ebookBadge: Color.lerp(ebookBadge, other.ebookBadge, t)!,
    );
  }
}
