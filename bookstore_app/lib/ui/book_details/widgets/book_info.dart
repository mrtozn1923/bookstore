import 'package:material_ui/material_ui.dart';

import '../../../domain/models/book.dart';

class BookInfo extends StatelessWidget {
  const BookInfo({super.key, required this.book, this.onAddToCart});

  final Book book;
  final VoidCallback? onAddToCart;

  @override
  Widget build(BuildContext context) {
    final textTheme = Theme.of(context).textTheme;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(book.displayTitle, style: textTheme.headlineSmall),
        const SizedBox(height: 4),
        Text(book.author.name, style: textTheme.titleMedium),
        const SizedBox(height: 16),
        // A Row overflowed here (320 dp, 2x text); Wrap moves the button down
        Wrap(
          alignment: WrapAlignment.spaceBetween,
          crossAxisAlignment: WrapCrossAlignment.center,
          spacing: 16,
          runSpacing: 8,
          children: [
            Text(book.price.format(), style: textTheme.titleLarge),
            FilledButton.icon(
              onPressed: onAddToCart,
              icon: const Icon(Icons.add_shopping_cart),
              label: const Text('Add to cart'),
            ),
          ],
        ),
        const SizedBox(height: 16),
        Wrap(
          spacing: 8,
          runSpacing: 8,
          children: [
            Chip(label: Text(book.format.name)),
            Chip(label: Text('${book.publishedYear}')),
            Chip(label: Text('ISBN ${book.isbn}')),
          ],
        ),
        if (book.description case final description?) ...[
          const SizedBox(height: 16),
          Text(description, style: textTheme.bodyLarge),
        ],
      ],
    );
  }
}
