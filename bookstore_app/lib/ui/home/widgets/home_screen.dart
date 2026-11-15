import 'dart:async';

import 'package:flutter/widget_previews.dart';
import 'package:material_ui/material_ui.dart';

import '../../../data/sample_books.dart';
import '../../../domain/models/book.dart';
import '../../book_details/widgets/book_details_screen.dart';
import '../../catalog/widgets/book_card.dart';

class HomeScreen extends StatefulWidget {
  const HomeScreen({super.key});

  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> {
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
    return Scaffold(
      appBar: AppBar(
        title: const Text('Bookstore'),
        actions: [
          Padding(
            padding: const EdgeInsets.only(right: 16),
            child: Center(child: Text('Books in cart: $_booksInCart')),
          ),
        ],
      ),
      body: ListView(
        padding: const EdgeInsets.all(8),
        children: [
          for (final book in sampleBooks)
            BookCard(
              key: ValueKey(book.isbn),
              book: book,
              onAddToCart: _addBook,
              onTap: () => _openDetails(book),
            ),
        ],
      ),
    );
  }
}

@Preview(name: 'Home screen')
Widget homeScreenPreview() => const MaterialApp(home: HomeScreen());
