import 'package:flutter/widget_previews.dart';
import 'package:material_ui/material_ui.dart';

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
      appBar: AppBar(title: const Text('Bookstore')),
      body: Center(
        child: Text(
          'Books in cart: $_booksInCart',
          style: Theme.of(context).textTheme.headlineSmall,
        ),
      ),
      floatingActionButton: FloatingActionButton(
        onPressed: _addBook,
        tooltip: 'Add book',
        child: const Icon(Icons.add_shopping_cart),
      ),
    );
  }
}

@Preview(name: 'Home screen')
Widget homeScreenPreview() => const MaterialApp(home: HomeScreen());
