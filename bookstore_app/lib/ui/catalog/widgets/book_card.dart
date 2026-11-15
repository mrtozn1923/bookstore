import 'package:material_ui/material_ui.dart';

import '../../../domain/models/book.dart';
import 'book_thumbnail.dart';
import 'favorite_button.dart';

class BookCard extends StatelessWidget {
  const BookCard({super.key, required this.book, this.onAddToCart, this.onTap});

  final Book book;
  final VoidCallback? onAddToCart;
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context) {
    final textTheme = Theme.of(context).textTheme;
    return Card(
      clipBehavior: Clip.antiAlias,
      child: InkWell(
        onTap: onTap,
        child: Padding(
          padding: const EdgeInsets.all(12),
          child: Row(
            children: [
              BookThumbnail(url: book.coverUrl),
              const SizedBox(width: 12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      book.displayTitle,
                      style: textTheme.titleMedium,
                      maxLines: 2,
                      overflow: TextOverflow.ellipsis,
                    ),
                    Text(
                      '${book.author.name} · ${book.publishedYear}',
                      style: textTheme.bodySmall,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                    ),
                    Text(book.price.format(), style: textTheme.titleSmall),
                  ],
                ),
              ),
              FavoriteButton(isbn: book.isbn),
              IconButton(
                onPressed: onAddToCart,
                tooltip: 'Add to cart',
                icon: const Icon(Icons.add_shopping_cart),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
