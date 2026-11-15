import 'package:drift/drift.dart';
import 'package:flutter/foundation.dart';

import '../../domain/models/book.dart';
import '../../utils/result.dart';
import '../services/api_client.dart';
import '../services/app_database.dart';
import 'book_repository.dart';
import 'cart_repository_drift.dart';

/// Whether the catalog on screen came from the device instead of the API.
class OfflineStatus extends ChangeNotifier {
  DateTime? _savedAt;

  bool get offline => _savedAt != null;
  DateTime? get savedAt => _savedAt;

  void showingSaved(DateTime? savedAt) {
    if (savedAt == _savedAt) return;
    _savedAt = savedAt;
    notifyListeners();
  }
}

/// Fresh cache first, then the network, then stale cache when the network
/// fails. The ViewModels still see a plain BookRepository.
class CachedBookRepository implements BookRepository {
  // Dart 3.12 private named parameters: callers write remote:, db:, status:
  CachedBookRepository({
    required this._remote,
    required this._db,
    required this._status,
    this.maxAge = const Duration(minutes: 10),
    DateTime Function()? now,
  }) : _now = now ?? DateTime.now;

  final BookRepository _remote;
  final AppDatabase _db;
  final OfflineStatus _status;
  final Duration maxAge;
  final DateTime Function() _now;

  @override
  Future<Result<List<Book>>> getPage(int page) async {
    final cached = await _cachedPage(page);
    final fetchedAt = cached.isEmpty ? null : cached.first.fetchedAt;
    if (fetchedAt != null && _now().difference(fetchedAt) < maxAge) {
      _status.showingSaved(null);
      return Result.ok([for (final row in cached) bookFromRow(row)]);
    }
    switch (await _remote.getPage(page)) {
      case Ok(:final value):
        await _savePage(page, value);
        _status.showingSaved(null);
        return Result.ok(value);
      // Only a network failure falls back; a 500 or bad JSON stays an error
      case Failure(error: NetworkException()) when cached.isNotEmpty:
        _status.showingSaved(fetchedAt);
        return Result.ok([for (final row in cached) bookFromRow(row)]);
      case Failure(:final error):
        return Result.failure(error);
    }
  }

  @override
  Future<Result<Book>> getByIsbn(String isbn) async {
    final result = await _remote.getByIsbn(isbn);
    if (result case Failure(error: NetworkException())) {
      final row = await (_db.select(
        _db.books,
      )..where((b) => b.isbn.equals(isbn))).getSingleOrNull();
      if (row != null) return Result.ok(bookFromRow(row));
    }
    return result;
  }

  Future<List<CachedBook>> _cachedPage(int page) =>
      (_db.select(_db.books)
            ..where((b) => b.catalogPage.equals(page))
            ..orderBy([(b) => OrderingTerm.asc(b.catalogPosition)]))
          .get();

  Future<void> _savePage(int page, List<Book> books) {
    final now = _now();
    return _db.batch((batch) {
      batch.insertAllOnConflictUpdate(_db.books, [
        for (final (i, book) in books.indexed)
          bookToRow(book, now, page: page, position: i),
      ]);
    });
  }
}
