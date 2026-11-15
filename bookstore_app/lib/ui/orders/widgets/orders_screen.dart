import 'package:material_ui/material_ui.dart';

import '../../../domain/models/order.dart';
import '../../../domain/models/order_status.dart';
import '../../core/formatting.dart';
import '../../core/ui/state_views.dart';

class OrdersScreen extends StatelessWidget {
  const OrdersScreen({super.key, required this.orders, required this.now});

  final List<Order> orders;
  final DateTime now;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('My orders')),
      body: orders.isEmpty
          ? const EmptyView(message: 'You have no orders yet.')
          : ListView.builder(
              itemCount: orders.length,
              itemBuilder: (context, i) {
                final order = orders[i];
                return ListTile(
                  key: ValueKey(order.id),
                  title: Text(
                    '${order.id} · ${context.formatPrice(order.total)}',
                  ),
                  subtitle: Text(
                    '${context.formatDate(order.placedAt)} · '
                    '${statusLabel(order.status, now: now)}',
                  ),
                );
              },
            ),
    );
  }
}
