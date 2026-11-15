import '../../domain/models/book.dart';
import '../../utils/result.dart';

class BookNotFound implements Exception {
  const BookNotFound(this.isbn);
  final String isbn;

  @override
  String toString() => 'No book with ISBN $isbn';
}

/// The single source of books for the UI layer.
abstract class BookRepository {
  Future<Result<List<Book>>> getPage(int page);
  Future<Result<Book>> getByIsbn(String isbn);
}
