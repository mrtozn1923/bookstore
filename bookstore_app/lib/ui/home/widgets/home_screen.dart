import 'package:flutter/widget_previews.dart';
import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';
import 'package:provider/provider.dart';

import '../../../config/dependencies.dart';
import '../../../routing/routes.dart';
import '../../catalog/view_models/catalog_view_model.dart';
import '../../catalog/widgets/catalog_screen.dart';

/// The catalog tab. Owns the CatalogViewModel so that it survives rebuilds of
/// the route (sign-in, sign-out) and is disposed with the tab.
class HomeScreen extends StatefulWidget {
  const HomeScreen({super.key});

  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> {
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

  @override
  Widget build(BuildContext context) {
    return CatalogScreen(
      viewModel: _catalog,
      onOpenBook: (book) => context.go(Routes.book(book.isbn)),
      onOpenCart: () => context.go(Routes.cart),
    );
  }
}

@Preview(name: 'Catalog tab')
Widget homeScreenPreview() => MultiProvider(
  providers: appProviders(),
  child: const MaterialApp(home: HomeScreen()),
);
