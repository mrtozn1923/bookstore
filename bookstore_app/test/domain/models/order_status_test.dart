import 'package:bookstore_app/domain/models/order_status.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  final now = DateTime.utc(2026, 11, 15);

  test('each status has a label', () {
    final labels = [
      const Pending(),
      Shipped(trackingNumber: 'TR123', shippedAt: DateTime.utc(2026, 11, 13)),
      Shipped(trackingNumber: 'TR456', shippedAt: DateTime.utc(2026, 11, 1)),
      Delivered(deliveredAt: DateTime.utc(2026, 11, 10)),
      const Cancelled(),
      const Cancelled(reason: 'Out of stock'),
    ].map((s) => statusLabel(s, now: now));

    expect(labels, [
      'Waiting for payment',
      'On the way (TR123)',
      'Delayed in shipping (TR456)',
      'Delivered on 2026-11-10',
      'Cancelled',
      'Cancelled: Out of stock',
    ]);
  });
}
