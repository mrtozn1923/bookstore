import 'package:material_ui/material_ui.dart';

import '../../../domain/models/book.dart';
import '../../core/formatting.dart';
import '../../core/ui/state_views.dart';
import '../view_models/cart_view_model.dart';

class CartScreen extends StatelessWidget {
  const CartScreen({super.key, required this.viewModel, this.onCheckout});

  final CartViewModel viewModel;
  final VoidCallback? onCheckout;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Cart')),
      body: ListenableBuilder(
        listenable: viewModel,
        builder: (context, _) {
          if (viewModel.lines.isEmpty) {
            return const EmptyView(
              message: 'Your cart is empty.',
              icon: Icons.shopping_cart_outlined,
            );
          }
          return Column(
            children: [
              Expanded(
                child: ListView.builder(
                  itemCount: viewModel.lines.length,
                  itemBuilder: (context, i) {
                    final line = viewModel.lines[i];
                    return _CartLineTile(
                      key: ValueKey(line.book.isbn),
                      line: line,
                      onChange: (quantity) => viewModel.setQuantity.execute((
                        book: line.book,
                        quantity: quantity,
                      )),
                    );
                  },
                ),
              ),
              SafeArea(
                top: false,
                child: Padding(
                  padding: const EdgeInsets.all(16),
                  child: Row(
                    children: [
                      Expanded(
                        child: Text(
                          'Total: ${context.formatPrice(viewModel.total)}',
                          style: Theme.of(context).textTheme.titleMedium,
                        ),
                      ),
                      FilledButton(
                        onPressed: onCheckout,
                        child: const Text('Checkout'),
                      ),
                    ],
                  ),
                ),
              ),
            ],
          );
        },
      ),
    );
  }
}

class _CartLineTile extends StatelessWidget {
  const _CartLineTile({super.key, required this.line, required this.onChange});

  final CartLine line;
  final ValueChanged<int> onChange;

  @override
  Widget build(BuildContext context) {
    return ListTile(
      title: Text(line.book.displayTitle),
      subtitle: Text(context.formatPrice(line.book.price)),
      trailing: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          IconButton(
            tooltip: 'Remove one',
            onPressed: () => onChange(line.quantity - 1),
            icon: const Icon(Icons.remove),
          ),
          Text('${line.quantity}'),
          IconButton(
            tooltip: 'Add one',
            onPressed: () => onChange(line.quantity + 1),
            icon: const Icon(Icons.add),
          ),
        ],
      ),
    );
  }
}
