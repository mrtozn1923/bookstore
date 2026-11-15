import 'package:flutter/widget_previews.dart';
import 'package:material_ui/material_ui.dart';

import '../../../data/sample_books.dart';
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
            ),
        ],
      ),
    );
  }
}

@Preview(name: 'Home screen')
Widget homeScreenPreview() => const MaterialApp(home: HomeScreen());
