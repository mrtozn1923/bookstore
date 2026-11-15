import '../../domain/models/book.dart';
import '../../domain/models/delivery_address.dart';
import '../../domain/models/order.dart';
import '../../utils/result.dart';
import '../services/api_client.dart';
import '../services/api_json.dart';
import 'order_repository.dart';

class ApiOrderRepository extends OrderRepository {
  ApiOrderRepository(this._api);

  final ApiClient _api;

  @override
  Future<Result<List<Order>>> getOrders() async {
    return switch (await _api.get('/api/orders', auth: true)) {
      Ok(value: List<Object?> items) => parseJson(
        () => [for (final item in items) orderFromJson(item)],
      ),
      Ok(:final value) => Result.failure(FormatException('Not a list: $value')),
      Failure(:final error) => Result.failure(error),
    };
  }

  /// The API orders a server-side cart: create it, fill it, then order it.
  /// Each step needs the previous one's answer, so these awaits are serial.
  @override
  Future<Result<Order>> placeOrder({
    required List<CartLine> lines,
    required DeliveryAddress address,
    required String email,
  }) async {
    final String cartId;
    switch (await _api.post('/api/carts', null)) {
      case Ok(value: {'id': final String id}):
        cartId = id;
      case Ok(:final value):
        return Result.failure(FormatException('Not a cart: $value'));
      case Failure(:final error):
        return Result.failure(error);
    }
    for (final (:book, :quantity) in lines) {
      final put = await _api.put('/api/carts/$cartId/items/${book.isbn}', {
        'quantity': quantity,
      });
      if (put case Failure(:final error)) return Result.failure(error);
    }
    final order = await _api.post('/api/orders', {
      'cartId': cartId,
      'customerName': address.fullName,
      'customerEmail': email,
    }, auth: true);
    switch (order) {
      case Ok(:final value):
        notifyListeners();
        return parseJson(() => orderFromJson(value));
      case Failure(error: ApiException(:final statusCode, :final problem)):
        return Result.failure(ApiException(statusCode, toFormProblem(problem)));
      case Failure(:final error):
        return Result.failure(error);
    }
  }
}

/// The API calls the recipient CustomerName; the address form calls it
/// FullName. The renaming happens here, once, not in the screen.
Map<String, Object?> toFormProblem(Map<String, Object?> problem) => {
  ...problem,
  if (problem['errors'] case final Map<String, Object?> errors)
    'errors': {
      for (final MapEntry(:key, :value) in errors.entries)
        key == 'CustomerName' ? 'ShippingAddress.FullName' : key: value,
    },
};
