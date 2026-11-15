import 'package:flutter/foundation.dart';

import '../../domain/models/book.dart';
import '../../domain/models/delivery_address.dart';
import '../../domain/models/order.dart';
import '../../domain/models/order_status.dart';
import '../../utils/result.dart';
import '../sample_orders.dart';

/// Order history and checkout. Notifies after a new order is placed.
abstract class OrderRepository extends ChangeNotifier {
  Future<Result<List<Order>>> getOrders();
  Future<Result<Order>> placeOrder({
    required List<CartLine> lines,
    required DeliveryAddress address,
    required String email,
  });
}

/// Sample orders for tests and previews; main() uses ApiOrderRepository.
class SampleOrderRepository extends OrderRepository {
  final _orders = [...sampleOrders];

  @override
  Future<Result<List<Order>>> getOrders() async => Result.ok(List.of(_orders));

  @override
  Future<Result<Order>> placeOrder({
    required List<CartLine> lines,
    required DeliveryAddress address,
    required String email,
  }) async {
    final order = Order(
      id: 'BK-${1043 + _orders.length}',
      placedAt: DateTime.now().toUtc(),
      total: summarizeCart(lines).total,
      status: const Pending(),
    );
    _orders.insert(0, order);
    notifyListeners();
    return Result.ok(order);
  }
}
