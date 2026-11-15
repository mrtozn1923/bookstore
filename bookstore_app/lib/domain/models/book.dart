import 'author.dart';
import 'money.dart';

enum BookFormat { paperback, hardcover, ebook }

class Book {
  const Book({
    required this.isbn,
    required this.title,
    required this.author,
    required this.price,
    required this.publishedYear,
    this.subtitle,
    this.coverUrl,
    this.format = .paperback,
  });

  final String isbn;
  final String title;
  final String? subtitle;
  final Author author;

  final Money price;
  final int publishedYear;
  final String? coverUrl;
  final BookFormat format;

  String get displayTitle => subtitle == null ? title : '$title: $subtitle';

  bool get shipsPhysically => switch (format) {
    .paperback || .hardcover => true,
    .ebook => false,
  };

  // A book is identified by its ISBN, whatever the other fields say
  @override
  bool operator ==(Object other) => other is Book && other.isbn == isbn;

  @override
  int get hashCode => isbn.hashCode;
}

typedef CartLine = ({Book book, int quantity});

({int itemCount, Money total}) summarizeCart(List<CartLine> lines) {
  var itemCount = 0;
  var total = Money.zero;
  for (final (:book, :quantity) in lines) {
    itemCount += quantity;
    total += book.price * quantity;
  }
  return (itemCount: itemCount, total: total);
}
