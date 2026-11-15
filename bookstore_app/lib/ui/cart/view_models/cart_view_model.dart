import 'dart:async';

import 'package:flutter/foundation.dart';

import '../../../data/repositories/cart_repository.dart';
import '../../../domain/models/book.dart';
import '../../../domain/models/money.dart';
import '../../../utils/command.dart';
import '../../../utils/result.dart';

class CartViewModel extends ChangeNotifier {
  CartViewModel({required CartRepository cartRepository})
    : _repository = cartRepository {
    load = Command0(_load);
    add = Command1(_add);
    setQuantity = Command1(_setQuantity);
    // The constructor cannot await; the command reports the outcome itself
    unawaited(load.execute());
  }

  final CartRepository _repository;
  List<CartLine> _lines = const [];

  late final Command0<void> load;
  late final Command1<void, Book> add;
  late final Command1<void, CartLine> setQuantity;

  List<CartLine> get lines => _lines;
  int get itemCount => summarizeCart(_lines).itemCount;
  Money get total => summarizeCart(_lines).total;

  int quantityOf(Book book) =>
      _lines.where((line) => line.book == book).firstOrNull?.quantity ?? 0;

  Future<Result<void>> _load() => _apply(_repository.getLines());

  Future<Result<void>> _add(Book book) =>
      _apply(_repository.setQuantity(book, quantityOf(book) + 1));

  Future<Result<void>> _setQuantity(CartLine line) =>
      _apply(_repository.setQuantity(line.book, line.quantity));

  /// The only place the state changes: one direction, repository -> state -> View.
  Future<Result<void>> _apply(Future<Result<List<CartLine>>> change) async {
    switch (await change) {
      case Ok(:final value):
        _lines = List.unmodifiable(value);
        notifyListeners();
        return const Result.ok(null);
      case Failure(:final error):
        return Result.failure(error);
    }
  }
}
