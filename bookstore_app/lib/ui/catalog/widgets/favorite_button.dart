import 'package:material_ui/material_ui.dart';

class FavoriteButton extends StatefulWidget {
  const FavoriteButton({
    super.key,
    required this.isbn,
    this.initiallyFavorite = false,
    this.onChanged,
  });

  final String isbn;
  final bool initiallyFavorite;
  final ValueChanged<bool>? onChanged;

  @override
  State<FavoriteButton> createState() => _FavoriteButtonState();
}

class _FavoriteButtonState extends State<FavoriteButton> {
  late bool _isFavorite;

  @override
  void initState() {
    super.initState();
    _isFavorite = widget.initiallyFavorite;
  }

  @override
  void didUpdateWidget(FavoriteButton oldWidget) {
    super.didUpdateWidget(oldWidget);
    // Same State, different book: reset instead of keeping the old value
    if (oldWidget.isbn != widget.isbn) {
      _isFavorite = widget.initiallyFavorite;
    }
  }

  void _toggle() {
    setState(() => _isFavorite = !_isFavorite);
    widget.onChanged?.call(_isFavorite);
  }

  @override
  Widget build(BuildContext context) {
    return IconButton(
      onPressed: _toggle,
      tooltip: _isFavorite ? 'Remove from favorites' : 'Add to favorites',
      icon: Icon(_isFavorite ? Icons.favorite : Icons.favorite_border),
    );
  }
}
