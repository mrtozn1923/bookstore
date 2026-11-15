import 'package:material_ui/material_ui.dart';
import 'package:provider/provider.dart';

import '../../core/ui/state_views.dart';
import '../view_models/orders_view_model.dart';
import 'orders_screen.dart';

/// What /orders shows: loading, an error or the order list.
class OrdersRoute extends StatefulWidget {
  const OrdersRoute({super.key});

  @override
  State<OrdersRoute> createState() => _OrdersRouteState();
}

class _OrdersRouteState extends State<OrdersRoute> {
  late final OrdersViewModel _viewModel;

  @override
  void initState() {
    super.initState();
    _viewModel = OrdersViewModel(orderRepository: context.read());
  }

  @override
  void dispose() {
    _viewModel.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return ListenableBuilder(
      listenable: Listenable.merge([_viewModel, _viewModel.load]),
      builder: (context, _) {
        final load = _viewModel.load;
        if (load.failed && _viewModel.orders.isEmpty) {
          return Scaffold(
            appBar: AppBar(title: const Text('My orders')),
            body: ErrorView(
              message: 'Your orders could not be loaded.',
              onRetry: load.execute,
            ),
          );
        }
        if (load.running && _viewModel.orders.isEmpty) {
          return Scaffold(
            appBar: AppBar(title: const Text('My orders')),
            body: const LoadingView(),
          );
        }
        return OrdersScreen(orders: _viewModel.orders, now: DateTime.now());
      },
    );
  }
}
