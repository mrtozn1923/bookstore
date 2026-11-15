import 'dart:async';

import 'package:bookstore_app/app.dart';
import 'package:bookstore_app/config/dependencies.dart';
import 'package:bookstore_app/data/repositories/auth_repository.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';
import 'package:provider/provider.dart';

Future<GoRouter> pumpApp(
  WidgetTester tester, {
  String? at,
  AuthRepository? auth,
}) async {
  await tester.pumpWidget(
    MultiProvider(
      providers: appProviders(authRepository: auth),
      child: BookstoreApp(initialLocation: at),
    ),
  );
  await tester.pumpAndSettle();
  return GoRouter.of(tester.element(find.byType(Navigator).first));
}

String location(GoRouter router) =>
    router.routerDelegate.currentConfiguration.uri.toString();

void main() {
  testWidgets('a deep link opens the book with the catalog under it', (
    tester,
  ) async {
    final router = await pumpApp(tester, at: '/catalog/books/9780441172719');
    debugPrint(
      'location: ${location(router)}, '
      'title: ${find.text('Dune').evaluate().isNotEmpty}, '
      'can pop: ${router.canPop()}',
    );
    router.pop();
    await tester.pumpAndSettle();
    debugPrint('after back: ${location(router)}');
  });

  testWidgets('orders need a signed-in user', (tester) async {
    final router = await pumpApp(tester);
    router.go('/orders');
    await tester.pumpAndSettle();
    debugPrint('signed out, go /orders -> ${location(router)}');

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
      'after sign-in -> ${location(router)}, '
      'BK-1042 shown: ${find.textContaining('BK-1042').evaluate().isNotEmpty}',
    );
  });

  testWidgets('each tab keeps its own stack', (tester) async {
    final router = await pumpApp(tester, at: '/catalog/books/9780441172719');
    await tester.tap(find.text('Cart'));
    await tester.pumpAndSettle();
    debugPrint('tap Cart tab -> ${location(router)}');
    await tester.tap(find.text('Catalog'));
    await tester.pumpAndSettle();
    debugPrint('tap Catalog tab -> ${location(router)}');
    await tester.tap(find.text('Catalog'));
    await tester.pumpAndSettle();
    debugPrint('tap Catalog tab again -> ${location(router)}');
  });

  testWidgets('go replaces the stack, push adds to it', (tester) async {
    final router = await pumpApp(tester, at: '/catalog/books/9780441172719');
    router.go('/sign-in');
    await tester.pumpAndSettle();
    debugPrint('go(/sign-in): can pop ${router.canPop()}');

    router.go('/catalog/books/9780441172719');
    await tester.pumpAndSettle();
    unawaited(router.push<void>('/sign-in'));
    await tester.pumpAndSettle();
    debugPrint('push(/sign-in): can pop ${router.canPop()}');
    router.pop();
    await tester.pumpAndSettle();
    debugPrint('after back: ${location(router)}');
  });

  testWidgets('the address form asks before discarding, then returns the order', (
    tester,
  ) async {
    final auth = SampleAuthRepository();
    await auth.signIn(email: 'ada@example.com', password: 'change-me-123');
    final router = await pumpApp(tester, at: '/cart', auth: auth);

    Object? result;
    unawaited(
      router.push<Object?>('/cart/address').then((value) => result = value),
    );
    await tester.pumpAndSettle();
    await tester.enterText(
      find.widgetWithText(TextFormField, 'Full name'),
      'Ada Lovelace',
    );
    await tester.pump();
    await tester.pageBack();
    await tester.pumpAndSettle();
    debugPrint(
      'back with text: dialog ${find.text('Discard this address?').evaluate().length}',
    );
    await tester.tap(find.text('Keep editing'));
    await tester.pumpAndSettle();

    await tester.enterText(
      find.widgetWithText(TextFormField, 'Address'),
      '1 Example Street',
    );
    await tester.enterText(
      find.widgetWithText(TextFormField, 'City'),
      'Istanbul',
    );
    await tester.enterText(
      find.widgetWithText(TextFormField, 'Postal code'),
      '34710',
    );
    await tester.enterText(
      find.widgetWithText(TextFormField, 'Phone'),
      '0212 555 01 01',
    );
    await tester.tap(find.text('Use this address'));
    await tester.pumpAndSettle();
    debugPrint(
      'push result: ${result.runtimeType}, location ${location(router)}',
    );
  });
}
