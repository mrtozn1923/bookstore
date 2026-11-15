sealed class OrderStatus {
  const OrderStatus();
}

final class Pending extends OrderStatus {
  const Pending();
}

final class Shipped extends OrderStatus {
  const Shipped({required this.trackingNumber, required this.shippedAt});
  final String trackingNumber;
  final DateTime shippedAt;
}

final class Delivered extends OrderStatus {
  const Delivered({required this.deliveredAt});
  final DateTime deliveredAt;
}

final class Cancelled extends OrderStatus {
  const Cancelled({this.reason});
  final String? reason;
}

/// Label shown in the order history; the switch must cover every subtype.
String statusLabel(OrderStatus status, {required DateTime now}) =>
    switch (status) {
      Pending() => 'Waiting for payment',
      Shipped(:final trackingNumber, :final shippedAt)
          when now.difference(shippedAt).inDays > 7 =>
        'Delayed in shipping ($trackingNumber)',
      Shipped(:final trackingNumber) => 'On the way ($trackingNumber)',
      Delivered(:final deliveredAt) =>
        'Delivered on ${deliveredAt.toIso8601String().substring(0, 10)}',
      Cancelled(reason: null) => 'Cancelled',
      Cancelled(:final reason?) => 'Cancelled: $reason',
    };
