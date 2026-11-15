import 'dart:async';

import 'package:material_ui/material_ui.dart';
import 'package:provider/provider.dart';

import '../../../domain/models/book.dart';
import '../../../utils/result.dart';
import '../../cart/view_models/cart_view_model.dart';
import '../../core/ui/state_views.dart';
import '../view_models/catalog_view_model.dart';
import 'book_card.dart';

class CatalogScreen extends StatefulWidget {
  const CatalogScreen({
    super.key,
    required this.viewModel,
    this.onOpenBook,
    this.onOpenCart,
  });

  final CatalogViewModel viewModel;
  final ValueChanged<Book>? onOpenBook;
  final VoidCallback? onOpenCart;

  @override
  State<CatalogScreen> createState() => _CatalogScreenState();
}

class _CatalogScreenState extends State<CatalogScreen> {
  /// Start loading the next page this far before the end of the list.
  static const loadAheadPixels = 500.0;

  late final CartViewModel _cart = context.read<CartViewModel>();

  @override
  void initState() {
    super.initState();
    widget.viewModel.refresh.addListener(_onRefreshResult);
    _cart.add.addListener(_onAddResult);
  }

  @override
  void dispose() {
    widget.viewModel.refresh.removeListener(_onRefreshResult);
    _cart.add.removeListener(_onAddResult);
    super.dispose();
  }

  void _onRefreshResult() {
    if (!widget.viewModel.refresh.failed) return;
    widget.viewModel.refresh.clearResult();
    ScaffoldMessenger.of(context).showSnackBar(
      const SnackBar(content: Text('Could not refresh the catalog')),
    );
  }

  void _onAddResult() {
    if (_cart.add.result case Failure(:final error)) {
      _cart.add.clearResult();
      ScaffoldMessenger.of(context)
          .showSnackBar(SnackBar(content: Text('$error')));
    }
  }

  bool _onScroll(ScrollNotification notification) {
    final loadMore = widget.viewModel.loadMore;
    if (notification.metrics.extentAfter < loadAheadPixels &&
        widget.viewModel.hasMore &&
        !loadMore.running &&
        !loadMore.failed) {
      unawaited(loadMore.execute());
    }
    return false;
  }

  Widget _card(Book book) => BookCard(
    key: ValueKey(book.isbn),
    book: book,
    onAddToCart: () => _cart.add.execute(book),
    onTap: () => widget.onOpenBook?.call(book),
  );

  @override
  Widget build(BuildContext context) {
    final viewModel = widget.viewModel;
    return Scaffold(
      body: RefreshIndicator(
        onRefresh: viewModel.refresh.execute,
        child: NotificationListener<ScrollNotification>(
          onNotification: _onScroll,
          child: ListenableBuilder(
            listenable: Listenable.merge([viewModel, viewModel.loadMore]),
            builder: (context, _) => CustomScrollView(
              physics: const AlwaysScrollableScrollPhysics(),
              slivers: [
                SliverAppBar(
                  floating: true,
                  title: const Text('Bookstore'),
                  actions: [
                    // Rebuilds only when the number of books in the cart changes
                    Selector<CartViewModel, int>(
                      selector: (_, cart) => cart.itemCount,
                      builder: (context, count, _) => TextButton.icon(
                        onPressed: widget.onOpenCart,
                        icon: const Icon(Icons.shopping_cart_outlined),
                        label: Text('Books in cart: $count'),
                      ),
                    ),
                  ],
                ),
                if (viewModel.books.isEmpty)
                  SliverFillRemaining(
                    hasScrollBody: false,
                    child: _emptyState(),
                  )
                else ...[
                  SliverPadding(
                    padding: const EdgeInsets.all(8),
                    sliver: SliverLayoutBuilder(
                      builder: (context, constraints) =>
                          constraints.crossAxisExtent < 600
                          ? SliverList.builder(
                              itemCount: viewModel.books.length,
                              itemBuilder: (context, i) =>
                                  _card(viewModel.books[i]),
                            )
                          : SliverGrid.builder(
                              gridDelegate:
                                  const SliverGridDelegateWithMaxCrossAxisExtent(
                                    maxCrossAxisExtent: 420,
                                    mainAxisExtent: 120,
                                  ),
                              itemCount: viewModel.books.length,
                              itemBuilder: (context, i) =>
                                  _card(viewModel.books[i]),
                            ),
                    ),
                  ),
                  SliverToBoxAdapter(child: _footer()),
                ],
              ],
            ),
          ),
        ),
      ),
    );
  }

  Widget _emptyState() {
    final loadMore = widget.viewModel.loadMore;
    if (loadMore.failed) {
      return ErrorView(
        message: 'The catalog could not be loaded.',
        onRetry: loadMore.execute,
      );
    }
    if (loadMore.running) return const LoadingView();
    return const EmptyView(message: 'No books in the catalog yet.');
  }

  Widget _footer() {
    final loadMore = widget.viewModel.loadMore;
    if (loadMore.failed) {
      return ErrorView(
        message: 'More books could not be loaded.',
        onRetry: loadMore.execute,
      );
    }
    if (loadMore.running) {
      return const Padding(padding: EdgeInsets.all(16), child: LoadingView());
    }
    return const SizedBox(height: 16);
  }
}
