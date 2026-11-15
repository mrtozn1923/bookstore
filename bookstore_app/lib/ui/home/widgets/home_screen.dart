import 'dart:async';

import 'package:flutter/widget_previews.dart';
import 'package:material_ui/material_ui.dart';
import 'package:provider/provider.dart';

import '../../../config/dependencies.dart';
import '../../../domain/models/book.dart';
import '../../book_details/widgets/book_details_screen.dart';
import '../../cart/view_models/cart_view_model.dart';
import '../../cart/widgets/cart_screen.dart';
import '../../catalog/view_models/catalog_view_model.dart';
import '../../catalog/widgets/catalog_screen.dart';

class HomeScreen extends StatefulWidget {
  const HomeScreen({super.key});

  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> {
  // One ViewModel per screen instance, created once, disposed with the screen
  late final CatalogViewModel _catalog;

  @override
  void initState() {
    super.initState();
    _catalog = CatalogViewModel(bookRepository: context.read());
  }

  @override
  void dispose() {
    _catalog.dispose();
    super.dispose();
  }

  // Navigator and go_router come in the navigation post; for now a plain push
  void _push(Widget screen) {
    unawaited(
      Navigator.of(context)
          .push(MaterialPageRoute<void>(builder: (_) => screen)),
    );
  }

  void _openDetails(Book book) {
    final cart = context.read<CartViewModel>();
    _push(
      BookDetailsScreen(book: book, onAddToCart: () => cart.add.execute(book)),
    );
  }

  @override
  Widget build(BuildContext context) {
    return CatalogScreen(
      viewModel: _catalog,
      onOpenBook: _openDetails,
      onOpenCart: () =>
          _push(CartScreen(viewModel: context.read<CartViewModel>())),
    );
  }
}

@Preview(name: 'Home screen')
Widget homeScreenPreview() => MultiProvider(
  providers: appProviders(),
  child: const MaterialApp(home: HomeScreen()),
);
