import '../../domain/models/book.dart';
import '../../utils/result.dart';

/// Every method returns the cart as it is after the change.
abstract class CartRepository {
  Future<Result<List<CartLine>>> getLines();
  Future<Result<List<CartLine>>> setQuantity(Book book, int quantity);
  Future<Result<List<CartLine>>> clear();
}

class TooManyCopies implements Exception {
  const TooManyCopies(this.max);
  final int max;

  @override
  String toString() => 'At most $max copies of a book per order';
}

/// Keeps the cart in memory; post 14 stores it on the device.
class InMemoryCartRepository implements CartRepository {
  static const maxCopies = 10;

  final _lines = <CartLine>[];

  @override
  Future<Result<List<CartLine>>> getLines() async => Result.ok(List.of(_lines));

  @override
  Future<Result<List<CartLine>>> setQuantity(Book book, int quantity) async {
    if (quantity > maxCopies) {
      return const Result.failure(TooManyCopies(maxCopies));
    }
    final index = _lines.indexWhere((line) => line.book == book);
    if (quantity <= 0) {
      if (index >= 0) _lines.removeAt(index);
    } else if (index >= 0) {
      _lines[index] = (book: book, quantity: quantity);
    } else {
      _lines.add((book: book, quantity: quantity));
    }
    return Result.ok(List.of(_lines));
  }

  @override
  Future<Result<List<CartLine>>> clear() async {
    _lines.clear();
    return const Result.ok([]);
  }
}
