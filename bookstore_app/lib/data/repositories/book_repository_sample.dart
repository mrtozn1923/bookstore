import '../../domain/models/book.dart';
import '../../utils/result.dart';
import '../sample_catalog.dart';
import 'book_repository.dart';

/// Until the app talks to Bookstore.Api, books come from the sample catalog.
class SampleBookRepository implements BookRepository {
  SampleBookRepository(this._catalog);

  final SampleCatalog _catalog;

  @override
  Future<Result<List<Book>>> getPage(int page) async {
    try {
      return Result.ok(await _catalog.page(page));
    } on Exception catch (e) {
      return Result.failure(e);
    }
  }
}
