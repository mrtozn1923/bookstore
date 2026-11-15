import 'package:flutter/widget_previews.dart';
import 'package:material_ui/material_ui.dart';

import '../../../data/sample_books.dart';
import '../../../domain/models/book.dart';
import 'book_cover.dart';
import 'book_info.dart';

class BookDetailsScreen extends StatelessWidget {
  const BookDetailsScreen({super.key, required this.book, this.onAddToCart});

  /// Below this width the cover sits above the text; from it up, on the left.
  static const twoColumnMinWidth = 600.0;

  final Book book;
  final VoidCallback? onAddToCart;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text(book.title)),
      body: SafeArea(
        child: LayoutBuilder(
          builder: (context, constraints) {
            final info = BookInfo(book: book, onAddToCart: onAddToCart);
            if (constraints.maxWidth >= twoColumnMinWidth) {
              return Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Padding(
                    padding: const EdgeInsets.all(24),
                    child: SizedBox(width: 240, child: BookCover(book: book)),
                  ),
                  Expanded(
                    child: SingleChildScrollView(
                      padding: const EdgeInsets.fromLTRB(0, 24, 24, 24),
                      child: info,
                    ),
                  ),
                ],
              );
            }
            return SingleChildScrollView(
              padding: const EdgeInsets.all(16),
              child: Column(
                children: [
                  SizedBox(width: 180, child: BookCover(book: book)),
                  const SizedBox(height: 16),
                  info,
                ],
              ),
            );
          },
        ),
      ),
    );
  }
}

@Preview(name: 'Book details, phone', size: Size(390, 844))
Widget phonePreview() =>
    MaterialApp(home: BookDetailsScreen(book: sampleBooks.last));

@Preview(name: 'Book details, tablet', size: Size(1024, 768))
Widget tabletPreview() =>
    MaterialApp(home: BookDetailsScreen(book: sampleBooks.last));
