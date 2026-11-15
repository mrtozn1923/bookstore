import 'dart:async';
import 'dart:collection';

import 'package:flutter/foundation.dart';

import '../../../data/repositories/book_repository.dart';
import '../../../domain/models/book.dart';
import '../../../utils/command.dart';
import '../../../utils/result.dart';

class CatalogViewModel extends ChangeNotifier {
  CatalogViewModel({required BookRepository bookRepository})
    : _repository = bookRepository {
    loadMore = Command0(_loadMore);
    refresh = Command0(_refresh);
    unawaited(loadMore.execute());
  }

  final BookRepository _repository;
  final _books = <Book>[];
  var _nextPage = 0;
  var _hasMore = true;

  late final Command0<void> loadMore;
  late final Command0<void> refresh;

  List<Book> get books => UnmodifiableListView(_books);
  bool get hasMore => _hasMore;

  Future<Result<void>> _loadMore() async {
    if (!_hasMore) return const Result.ok(null);
    switch (await _repository.getPage(_nextPage)) {
      case Ok(:final value):
        _books.addAll(value);
        _nextPage++;
        _hasMore = value.isNotEmpty;
        notifyListeners();
        return const Result.ok(null);
      case Failure(:final error):
        return Result.failure(error);
    }
  }

  Future<Result<void>> _refresh() async {
    switch (await _repository.getPage(0)) {
      case Ok(:final value):
        _books
          ..clear()
          ..addAll(value);
        _nextPage = 1;
        _hasMore = value.isNotEmpty;
        notifyListeners();
        return const Result.ok(null);
      case Failure(:final error):
        return Result.failure(error);
    }
  }
}
