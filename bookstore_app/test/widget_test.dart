import 'package:bookstore_app/app.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:material_ui/material_ui.dart';

void main() {
  testWidgets('tapping the button adds a book to the cart', (tester) async {
    await tester.pumpWidget(const BookstoreApp());
    expect(find.text('Books in cart: 0'), findsOneWidget);

    await tester.tap(find.byIcon(Icons.add_shopping_cart));
    await tester.pump();

    expect(find.text('Books in cart: 1'), findsOneWidget);
  });
}
