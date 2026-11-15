import 'package:material_ui/material_ui.dart';

import '../../../domain/models/book.dart';
import '../../core/themes/bookstore_colors.dart';

/// 2:3 cover with a format badge in the top-left corner.
class BookCover extends StatelessWidget {
  const BookCover({super.key, required this.book});

  final Book book;

  @override
  Widget build(BuildContext context) {
    final colors = Theme.of(context).colorScheme;
    return AspectRatio(
      aspectRatio: 2 / 3,
      child: Stack(
        fit: StackFit.expand,
        children: [
          DecoratedBox(
            decoration: BoxDecoration(
              color: colors.primaryContainer,
              borderRadius: BorderRadius.circular(8),
            ),
            child: Icon(
              Icons.menu_book,
              size: 64,
              color: colors.onPrimaryContainer,
            ),
          ),
          if (book.format == .ebook)
            Positioned(
              top: 8,
              left: 8,
              child: Chip(
                label: const Text('E-book'),
                visualDensity: VisualDensity.compact,
                backgroundColor: BookstoreColors.of(context).ebookBadge,
                labelStyle: TextStyle(color: colors.surface),
              ),
            ),
        ],
      ),
    );
  }
}
