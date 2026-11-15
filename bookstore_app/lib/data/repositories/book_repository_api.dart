import '../../domain/models/book.dart';
import '../../utils/result.dart';
import '../services/api_client.dart';
import '../services/api_json.dart';
import 'book_repository.dart';

/// Books from Bookstore.Api. The UI still sees only BookRepository.
class ApiBookRepository implements BookRepository {
  ApiBookRepository(this._api, {this.pageSize = 20});

  final ApiClient _api;
  final int pageSize;

  @override
  Future<Result<List<Book>>> getPage(int page) async {
    // The app counts pages from 0, the API from 1
    final result = await _api.get(
      '/api/books',
      query: {'page': '${page + 1}', 'pageSize': '$pageSize'},
    );
    return switch (result) {
      Ok(value: {'items': List<Object?> items}) => parseJson(
        () => [for (final item in items) bookFromJson(item)],
      ),
      Ok(:final value) => Result.failure(FormatException('Not a page: $value')),
      Failure(:final error) => Result.failure(error),
    };
  }

  @override
  Future<Result<Book>> getByIsbn(String isbn) async {
    return switch (await _api.get('/api/books/$isbn')) {
      Ok(:final value) => parseJson(() => bookFromJson(value)),
      Failure(error: ApiException(statusCode: 404)) => Result.failure(
        BookNotFound(isbn),
      ),
      Failure(:final error) => Result.failure(error),
    };
  }
}
