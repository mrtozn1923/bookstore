import 'package:bookstore_app/app.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  testWidgets('adding a book from a card updates the cart count', (
    tester,
  ) async {
    await tester.pumpWidget(const BookstoreApp());
    expect(find.text('Books in cart: 0'), findsOneWidget);
    expect(find.text('Dune'), findsOneWidget);

    await tester.tap(find.byTooltip('Add to cart').first);
    await tester.pump();

    expect(find.text('Books in cart: 1'), findsOneWidget);
  });
}
