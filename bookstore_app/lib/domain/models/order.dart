import 'money.dart';
import 'order_status.dart';

class Order {
  const Order({
    required this.id,
    required this.placedAt,
    required this.total,
    required this.status,
  });

  final String id;
  final DateTime placedAt;
  final Money total;
  final OrderStatus status;
}
