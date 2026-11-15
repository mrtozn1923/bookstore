import 'package:material_ui/material_ui.dart';
import 'package:provider/provider.dart';

import '../../../domain/models/book.dart';
import '../../cart/view_models/cart_view_model.dart';
import '../../core/ui/state_views.dart';
import '../view_models/book_details_view_model.dart';
import 'book_details_screen.dart';

/// What /catalog/books/:isbn shows: loading, an error or the book.
class BookDetailsRoute extends StatefulWidget {
  const BookDetailsRoute({super.key, required this.isbn});

  final String isbn;

  @override
  State<BookDetailsRoute> createState() => _BookDetailsRouteState();
}

class _BookDetailsRouteState extends State<BookDetailsRoute> {
  late final BookDetailsViewModel _viewModel;

  @override
  void initState() {
    super.initState();
    _viewModel = BookDetailsViewModel(
      isbn: widget.isbn,
      bookRepository: context.read(),
    );
  }

  @override
  void dispose() {
    _viewModel.dispose();
    super.dispose();
  }

  Future<void> _addToCart(Book book) async {
    // A bottom sheet is a route too: pop(context, 2) completes this Future
    final quantity = await showModalBottomSheet<int>(
      context: context,
      builder: (context) => Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          for (var n = 1; n <= 3; n++)
            ListTile(
              title: Text('$n'),
              onTap: () => Navigator.of(context).pop(n),
            ),
        ],
      ),
    );
    if (quantity == null || !mounted) return; // dismissed
    final cart = context.read<CartViewModel>();
    await cart.setQuantity.execute((
      book: book,
      quantity: cart.quantityOf(book) + quantity,
    ));
  }

  @override
  Widget build(BuildContext context) {
    return ListenableBuilder(
      listenable: Listenable.merge([_viewModel, _viewModel.load]),
      builder: (context, _) {
        if (_viewModel.book case final book?) {
          return BookDetailsScreen(
            book: book,
            onAddToCart: () => _addToCart(book),
          );
        }
        return Scaffold(
          appBar: AppBar(),
          body: _viewModel.load.failed
              ? ErrorView(
                  message: 'This book could not be found.',
                  onRetry: _viewModel.load.execute,
                )
              : const LoadingView(),
        );
      },
    );
  }
}
