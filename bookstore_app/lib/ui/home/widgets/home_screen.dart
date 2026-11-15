import 'dart:async';

import 'package:flutter/widget_previews.dart';
import 'package:material_ui/material_ui.dart';

import '../../../data/sample_catalog.dart';
import '../../../domain/models/book.dart';
import '../../book_details/widgets/book_details_screen.dart';
import '../../catalog/widgets/catalog_screen.dart';

class HomeScreen extends StatefulWidget {
  const HomeScreen({super.key, this.catalog});

  final SampleCatalog? catalog;

  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> {
  late final SampleCatalog _catalog = widget.catalog ?? SampleCatalog();
  int _booksInCart = 0;

  void _addBook() {
    setState(() => _booksInCart++);
  }

  // Navigator and go_router come in the navigation post; for now a plain push
  void _openDetails(Book book) {
    unawaited(
      Navigator.of(context).push(
        MaterialPageRoute<void>(
          builder: (_) => BookDetailsScreen(book: book, onAddToCart: _addBook),
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return CatalogScreen(
      loadPage: _catalog.page,
      cartCount: _booksInCart,
      onAddToCart: (_) => _addBook(),
      onOpenBook: _openDetails,
    );
  }
}

@Preview(name: 'Home screen')
Widget homeScreenPreview() => const MaterialApp(home: HomeScreen());
