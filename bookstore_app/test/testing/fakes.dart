import 'package:bookstore_app/data/repositories/book_repository.dart';
import 'package:bookstore_app/data/repositories/cart_repository.dart';
import 'package:bookstore_app/data/sample_catalog.dart';
import 'package:bookstore_app/domain/models/book.dart';
import 'package:bookstore_app/utils/result.dart';

/// A working BookRepository that records the pages asked for and can fail.
class FakeBookRepository implements BookRepository {
  FakeBookRepository({SampleCatalog? catalog, this.failuresLeft = 0})
    : _catalog = catalog ?? SampleCatalog(total: 60, pageSize: 20);

  final SampleCatalog _catalog;
  final calls = <int>[];
  int failuresLeft;
  Object error = Exception('timeout');

  @override
  Future<Result<List<Book>>> getPage(int page) async {
    calls.add(page);
    if (failuresLeft > 0) {
      failuresLeft--;
      return Result.failure(error);
    }
    return Result.ok(await _catalog.page(page));
  }

  @override
  Future<Result<Book>> getByIsbn(String isbn) async {
    final book = await _catalog.byIsbn(isbn);
    return book == null ? Result.failure(BookNotFound(isbn)) : Result.ok(book);
  }
}

/// The in-memory cart, plus a way to change a price as the server would.
class FakeCartRepository extends InMemoryCartRepository {
  final _prices = <String, Book>{};

  void changePrice(Book book) => _prices[book.isbn] = book;

  @override
  Future<Result<List<CartLine>>> getLines() async {
    final result = await super.getLines();
    if (result case Ok(:final value)) {
      return Result.ok([
        for (final line in value)
          (book: _prices[line.book.isbn] ?? line.book, quantity: line.quantity),
      ]);
    }
    return result;
  }
}
