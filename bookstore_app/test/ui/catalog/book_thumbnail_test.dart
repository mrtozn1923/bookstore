import 'package:bookstore_app/ui/catalog/widgets/book_thumbnail.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:material_ui/material_ui.dart';

const coverUrl = 'https://covers.example.com/9780141439518.jpg';

void main() {
  testWidgets('a widget test makes no real network request', (tester) async {
    Object? error;
    await tester.pumpWidget(
      MaterialApp(
        home: Image.network(
          coverUrl,
          loadingBuilder: (context, child, progress) =>
              progress == null ? child : const Text('loading'),
          errorBuilder: (context, e, stackTrace) {
            error = e;
            return const Text('error');
          },
        ),
      ),
    );
    await tester.pumpAndSettle();
    debugPrint('errorBuilder ran: ${find.text('error').evaluate().isNotEmpty}');
    debugPrint('$error');
  });

  testWidgets('BookThumbnail falls back to the placeholder', (tester) async {
    await tester.pumpWidget(
      const MaterialApp(home: BookThumbnail(url: coverUrl)),
    );
    await tester.pumpAndSettle();
    expect(find.byIcon(Icons.menu_book), findsOneWidget);
    expect(tester.takeException(), isNull);
  });
}
