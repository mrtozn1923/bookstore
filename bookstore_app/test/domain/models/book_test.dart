import 'package:bookstore_app/domain/models/author.dart';
import 'package:bookstore_app/domain/models/book.dart';
import 'package:bookstore_app/domain/models/money.dart';
import 'package:flutter_test/flutter_test.dart';

const herbert = Author(name: 'Frank Herbert', birthYear: 1920);
const dune = Book(
  isbn: '9780441172719',
  title: 'Dune',
  author: herbert,
  price: Money(48900),
  publishedYear: 1965,
);
const middlemarch = Book(
  isbn: '9780141439549',
  title: 'Middlemarch',
  subtitle: 'A Study of Provincial Life',
  author: Author(name: 'George Eliot'),
  price: Money(27500),
  publishedYear: 1871,
  format: .ebook,
);

void main() {
  test('displayTitle adds the subtitle only when there is one', () {
    expect(dune.displayTitle, 'Dune');
    expect(middlemarch.displayTitle, 'Middlemarch: A Study of Provincial Life');
  });

  test('summarizeCart counts items and sums prices', () {
    final summary = summarizeCart([
      (book: dune, quantity: 2),
      (book: middlemarch, quantity: 1),
    ]);
    expect(summary.itemCount, 3);
    expect(summary.total.format(), '1253.00 TRY');
  });

  test('const books with the same values are the same instance', () {
    const again = Book(
      isbn: '9780441172719',
      title: 'Dune',
      author: herbert,
      price: Money(48900),
      publishedYear: 1965,
    );
    expect(identical(dune, again), isTrue);
  });

  test('books are equal when the ISBN matches', () {
    final fromApi = Book(
      isbn: '9780441172719',
      title: 'Dune (40th Anniversary Edition)',
      author: herbert,
      price: Money(52000),
      publishedYear: 2005,
    );
    expect(fromApi, dune);
    expect({dune, fromApi}, hasLength(1));
  });

  test('only ebooks skip shipping', () {
    expect(dune.shipsPhysically, isTrue);
    expect(middlemarch.shipsPhysically, isFalse);
  });
}
