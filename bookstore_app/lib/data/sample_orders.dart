import '../domain/models/money.dart';
import '../domain/models/order.dart';
import '../domain/models/order_status.dart';

/// Temporary order history until the app talks to Bookstore.Api.
final sampleOrders = [
  Order(
    id: 'BK-1042',
    placedAt: DateTime.utc(2026, 11, 12),
    total: const Money(70400),
    status: Shipped(
      trackingNumber: 'TR123456',
      shippedAt: DateTime.utc(2026, 11, 13),
    ),
  ),
  Order(
    id: 'BK-1017',
    placedAt: DateTime.utc(2026, 10, 30),
    total: const Money(21500),
    status: Delivered(deliveredAt: DateTime.utc(2026, 11, 2)),
  ),
];
