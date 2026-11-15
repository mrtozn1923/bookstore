import '../../domain/models/book.dart';
import '../../utils/result.dart';

/// The single source of books for the UI layer.
abstract class BookRepository {
  Future<Result<List<Book>>> getPage(int page);
}
