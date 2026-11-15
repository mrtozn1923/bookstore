import 'package:bookstore_app/routing/router.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  test('the auth guard', () {
    for (final (location, signedIn) in [
      ('/orders', false),
      ('/cart/address', false),
      ('/catalog/books/9780441172719', false),
      ('/sign-in?from=%2Forders', true),
      ('/sign-in?from=%2F%2Fother.example', true),
    ]) {
      final target = authRedirect(Uri.parse(location), signedIn: signedIn);
      debugPrint('$location (signed in: $signedIn) -> ${target ?? 'stays'}');
    }
  });
}
