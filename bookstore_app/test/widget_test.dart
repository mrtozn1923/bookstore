import 'package:bookstore_app/app.dart';
import 'package:bookstore_app/ui/book_details/widgets/book_details_screen.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  testWidgets('adding a book from a card updates the cart count', (
    tester,
  ) async {
    await tester.pumpWidget(const BookstoreApp());
    await tester.pumpAndSettle(); // the first catalog page arrives
    expect(find.text('Books in cart: 0'), findsOneWidget);
    expect(find.text('Dune'), findsOneWidget);

    await tester.tap(find.byTooltip('Add to cart').first);
    await tester.pump();

    expect(find.text('Books in cart: 1'), findsOneWidget);
  });

  testWidgets('tapping a card opens the book details', (tester) async {
    await tester.pumpWidget(const BookstoreApp());
    await tester.pumpAndSettle();

    await tester.tap(find.text('Dune'));
    await tester.pumpAndSettle();

    expect(find.byType(BookDetailsScreen), findsOneWidget);
    expect(find.textContaining('Arrakis'), findsOneWidget);
  });
}
