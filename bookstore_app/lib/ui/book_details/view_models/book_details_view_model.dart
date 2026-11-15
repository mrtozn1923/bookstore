import 'dart:async';

import 'package:flutter/foundation.dart';

import '../../../data/repositories/book_repository.dart';
import '../../../domain/models/book.dart';
import '../../../utils/command.dart';
import '../../../utils/result.dart';

/// Loads one book by ISBN, so that /catalog/books/:isbn works as a deep link.
class BookDetailsViewModel extends ChangeNotifier {
  BookDetailsViewModel({
    required this.isbn,
    required BookRepository bookRepository,
  }) : _repository = bookRepository {
    load = Command0(_load);
    unawaited(load.execute());
  }

  final String isbn;
  final BookRepository _repository;
  Book? _book;

  late final Command0<void> load;

  Book? get book => _book;

  Future<Result<void>> _load() async {
    switch (await _repository.getByIsbn(isbn)) {
      case Ok(:final value):
        _book = value;
        notifyListeners();
        return const Result.ok(null);
      case Failure(:final error):
        return Result.failure(error);
    }
  }
}
