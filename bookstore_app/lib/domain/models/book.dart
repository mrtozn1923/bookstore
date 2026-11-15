import 'author.dart';

class Book {
  const Book({
    required this.isbn,
    required this.title,
    required this.author,
    required this.price,
    required this.publishedYear,
    this.subtitle,
    this.coverUrl,
  });

  final String isbn;
  final String title;
  final String? subtitle;
  final Author author;

  /// Price in kurus (1 TRY = 100 kurus); money is never a double.
  final int price;
  final int publishedYear;
  final String? coverUrl;

  String get displayTitle => subtitle == null ? title : '$title: $subtitle';
}

typedef CartLine = ({Book book, int quantity});

({int itemCount, int total}) summarizeCart(List<CartLine> lines) {
  var itemCount = 0;
  var total = 0;
  for (final (:book, :quantity) in lines) {
    itemCount += quantity;
    total += book.price * quantity;
  }
  return (itemCount: itemCount, total: total);
}

String formatTry(int kurus) =>
    '${kurus ~/ 100}.${(kurus % 100).toString().padLeft(2, '0')} TRY';
