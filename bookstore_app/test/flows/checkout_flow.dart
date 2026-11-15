import 'package:bookstore_app/app.dart';
import 'package:bookstore_app/config/dependencies.dart';
import 'package:bookstore_app/data/repositories/order_repository.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:material_ui/material_ui.dart';
import 'package:provider/provider.dart';

import '../testing/fakes.dart';

/// Sign in, add Dune to the cart, check out, see the order. The same steps
/// run as a widget test (test/flows) and on a device (integration_test/).
Future<void> checkoutFlow(WidgetTester tester) async {
  final orders = SampleOrderRepository();
  await tester.pumpWidget(
    MultiProvider(
      providers: appProviders(
        bookRepository: FakeBookRepository(),
        orderRepository: orders,
      ),
      child: const BookstoreApp(),
    ),
  );
  await tester.pumpAndSettle();

  // 1. Orders need a signed-in user: the tab redirects to the sign-in form
  await tester.tap(find.text('Orders'));
  await tester.pumpAndSettle();
  await tester.enterText(
    find.widgetWithText(TextFormField, 'Email'),
    'ada@example.com',
  );
  await tester.enterText(
    find.widgetWithText(TextFormField, 'Password'),
    'change-me-123',
  );
  await tester.tap(find.widgetWithText(FilledButton, 'Sign in'));
  await tester.pumpAndSettle();
  debugPrint(
    'signed in, orders on screen: ${find.textContaining('BK-').evaluate().length}',
  );

  // 2. Add Dune from the catalog
  await tester.tap(find.text('Catalog'));
  await tester.pumpAndSettle();
  await tester.tap(find.byTooltip('Add to cart').first);
  await tester.pumpAndSettle();

  // 3. Check out with a delivery address
  await tester.tap(find.text('Cart'));
  await tester.pumpAndSettle();
  await tester.tap(find.text('Checkout'));
  await tester.pumpAndSettle();
  final fields = {
    'Full name': 'Ada Lovelace',
    'Address': '1 Example Street',
    'City': 'Istanbul',
    'Postal code': '34710',
    'Phone': '0212 555 01 01',
  };
  for (final MapEntry(:key, :value) in fields.entries) {
    await tester.enterText(find.widgetWithText(TextFormField, key), value);
  }
  await tester.tap(find.text('Use this address'));
  await tester.pumpAndSettle();
  debugPrint(
    'snack bar: ${tester.widget<Text>(find.descendant(of: find.byType(SnackBar), matching: find.byType(Text))).data}, '
    'cart empty: ${find.text('Your cart is empty.').evaluate().isNotEmpty}',
  );

  // 4. The new order is on top of the order list
  await tester.tap(find.text('Orders'));
  await tester.pumpAndSettle();
  final first = tester.widget<ListTile>(find.byType(ListTile).first);
  debugPrint('first order: ${(first.title! as Text).data}');
}
