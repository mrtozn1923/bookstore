import 'dart:async';

import 'package:material_ui/material_ui.dart';

import '../../../domain/models/book.dart';
import '../../core/ui/state_views.dart';
import 'book_card.dart';

typedef CatalogPageLoader = Future<List<Book>> Function(int page);

class CatalogScreen extends StatefulWidget {
  const CatalogScreen({
    super.key,
    required this.loadPage,
    this.cartCount = 0,
    this.onAddToCart,
    this.onOpenBook,
  });

  final CatalogPageLoader loadPage;
  final int cartCount;
  final ValueChanged<Book>? onAddToCart;
  final ValueChanged<Book>? onOpenBook;

  @override
  State<CatalogScreen> createState() => _CatalogScreenState();
}

class _CatalogScreenState extends State<CatalogScreen> {
  /// Start loading the next page this far before the end of the list.
  static const loadAheadPixels = 500.0;

  final _books = <Book>[];
  var _nextPage = 0;
  var _hasMore = true;
  var _isLoading = false;
  Object? _error;

  @override
  void initState() {
    super.initState();
    unawaited(_loadMore());
  }

  Future<void> _loadMore() async {
    if (_isLoading || !_hasMore) return;
    setState(() {
      _isLoading = true;
      _error = null;
    });
    try {
      final page = await widget.loadPage(_nextPage);
      if (!mounted) return;
      setState(() {
        _books.addAll(page);
        _nextPage++;
        _hasMore = page.isNotEmpty;
        _isLoading = false;
      });
    } catch (error) {
      if (!mounted) return;
      setState(() {
        _error = error;
        _isLoading = false;
      });
    }
  }

  Future<void> _refresh() async {
    try {
      final first = await widget.loadPage(0);
      if (!mounted) return;
      setState(() {
        _books
          ..clear()
          ..addAll(first);
        _nextPage = 1;
        _hasMore = first.isNotEmpty;
        _error = null;
      });
    } catch (_) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Could not refresh the catalog')),
      );
    }
  }

  bool _onScroll(ScrollNotification notification) {
    if (notification.metrics.extentAfter < loadAheadPixels && _error == null) {
      unawaited(_loadMore());
    }
    return false;
  }

  Widget _card(Book book) => BookCard(
    key: ValueKey(book.isbn),
    book: book,
    onAddToCart: () => widget.onAddToCart?.call(book),
    onTap: () => widget.onOpenBook?.call(book),
  );

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: RefreshIndicator(
        onRefresh: _refresh,
        child: NotificationListener<ScrollNotification>(
          onNotification: _onScroll,
          child: CustomScrollView(
            physics: const AlwaysScrollableScrollPhysics(),
            slivers: [
              SliverAppBar(
                floating: true,
                title: const Text('Bookstore'),
                actions: [
                  Padding(
                    padding: const EdgeInsets.only(right: 16),
                    child: Center(
                      child: Text('Books in cart: ${widget.cartCount}'),
                    ),
                  ),
                ],
              ),
              if (_books.isEmpty)
                SliverFillRemaining(hasScrollBody: false, child: _emptyState())
              else ...[
                SliverPadding(
                  padding: const EdgeInsets.all(8),
                  sliver: SliverLayoutBuilder(
                    builder: (context, constraints) =>
                        constraints.crossAxisExtent < 600
                        ? SliverList.builder(
                            itemCount: _books.length,
                            itemBuilder: (context, i) => _card(_books[i]),
                          )
                        : SliverGrid.builder(
                            gridDelegate:
                                const SliverGridDelegateWithMaxCrossAxisExtent(
                                  maxCrossAxisExtent: 420,
                                  mainAxisExtent: 120,
                                ),
                            itemCount: _books.length,
                            itemBuilder: (context, i) => _card(_books[i]),
                          ),
                  ),
                ),
                SliverToBoxAdapter(child: _footer()),
              ],
            ],
          ),
        ),
      ),
    );
  }

  Widget _emptyState() {
    if (_error != null) {
      return ErrorView(
        message: 'The catalog could not be loaded.',
        onRetry: _loadMore,
      );
    }
    if (_isLoading) return const LoadingView();
    return const EmptyView(message: 'No books in the catalog yet.');
  }

  Widget _footer() {
    if (_error != null) {
      return ErrorView(
        message: 'More books could not be loaded.',
        onRetry: _loadMore,
      );
    }
    if (_isLoading) {
      return const Padding(padding: EdgeInsets.all(16), child: LoadingView());
    }
    return const SizedBox(height: 16);
  }
}
