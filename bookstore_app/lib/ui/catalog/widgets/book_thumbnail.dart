import 'package:material_ui/material_ui.dart';

/// Small cover in a list row. Decodes the image at the size it is drawn.
class BookThumbnail extends StatelessWidget {
  const BookThumbnail({super.key, this.url, this.width = 48});

  final String? url;
  final double width;

  @override
  Widget build(BuildContext context) {
    final height = width * 3 / 2;
    final placeholder = SizedBox(
      width: width,
      height: height,
      child: ColoredBox(
        color: Theme.of(context).colorScheme.surfaceContainerHighest,
        child: const Icon(Icons.menu_book),
      ),
    );
    final url = this.url;
    if (url == null) return placeholder;

    final dpr = MediaQuery.devicePixelRatioOf(context);
    return Image.network(
      url,
      width: width,
      height: height,
      fit: BoxFit.cover,
      cacheWidth: (width * dpr).round(),
      loadingBuilder: (context, child, progress) =>
          progress == null ? child : placeholder,
      errorBuilder: (context, error, stackTrace) => placeholder,
    );
  }
}
