import 'package:drift/drift.dart';

import '../../domain/models/author.dart';
import '../../domain/models/book.dart';
import '../../domain/models/money.dart';
import '../../utils/result.dart';
import '../services/app_database.dart';
import 'cart_repository.dart';

/// The cart in SQLite: it survives closing the app. Same contract as before.
class DriftCartRepository implements CartRepository {
  DriftCartRepository(this._db, {DateTime Function()? now})
    : _now = now ?? DateTime.now;

  final AppDatabase _db;
  final DateTime Function() _now;

  @override
  Future<Result<List<CartLine>>> getLines() async {
    // SELECT ... FROM cart_lines INNER JOIN books ON books.isbn = cart_lines.isbn
    final query = _db.select(_db.cartLines).join([
      innerJoin(_db.books, _db.books.isbn.equalsExp(_db.cartLines.isbn)),
    ])..orderBy([OrderingTerm.asc(_db.books.title)]);
    final rows = await query.get();
    return Result.ok([
      for (final row in rows)
        (
          book: bookFromRow(row.readTable(_db.books)),
          quantity: row.readTable(_db.cartLines).quantity,
        ),
    ]);
  }

  @override
  Future<Result<List<CartLine>>> setQuantity(Book book, int quantity) async {
    if (quantity > InMemoryCartRepository.maxCopies) {
      return const Result.failure(
        TooManyCopies(InMemoryCartRepository.maxCopies),
      );
    }
    await _db.transaction(() async {
      await _db
          .into(_db.books)
          .insert(
            bookToRow(book, _now()),
            // Keep the catalog position if the book is already cached
            onConflict: DoUpdate(
              (_) => BooksCompanion(
                title: Value(book.title),
                priceKurus: Value(book.price.kurus),
              ),
            ),
          );
      if (quantity <= 0) {
        await (_db.delete(
          _db.cartLines,
        )..where((l) => l.isbn.equals(book.isbn))).go();
      } else {
        await _db
            .into(_db.cartLines)
            .insertOnConflictUpdate(
              CartLinesCompanion.insert(isbn: book.isbn, quantity: quantity),
            );
      }
    });
    return getLines();
  }

  @override
  Future<Result<List<CartLine>>> clear() async {
    await _db.delete(_db.cartLines).go();
    return const Result.ok([]);
  }
}

Book bookFromRow(CachedBook row) => Book(
  isbn: row.isbn,
  title: row.title,
  author: Author(name: row.author),
  price: Money(row.priceKurus),
  publishedYear: row.publishedYear,
  description: row.description,
);

BooksCompanion bookToRow(
  Book book,
  DateTime fetchedAt, {
  int? page,
  int? position,
}) => BooksCompanion.insert(
  isbn: book.isbn,
  title: book.title,
  author: book.author.name,
  priceKurus: book.price.kurus,
  publishedYear: book.publishedYear,
  description: Value(book.description),
  catalogPage: Value(page),
  catalogPosition: Value(position),
  fetchedAt: fetchedAt,
);
