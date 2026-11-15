import 'package:bookstore_app/data/repositories/cart_repository.dart';
import 'package:bookstore_app/data/sample_books.dart';
import 'package:bookstore_app/ui/cart/view_models/cart_view_model.dart';
import 'package:bookstore_app/utils/result.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter_test/flutter_test.dart';

final dune = sampleBooks.first;

void main() {
  test('adding the same book twice increments its quantity', () async {
    final cart = CartViewModel(cartRepository: InMemoryCartRepository());
    var notifications = 0;
    cart.addListener(() => notifications++);
    await cart.load.execute();

    await cart.add.execute(dune);
    await cart.add.execute(dune);

    debugPrint(
      'lines ${cart.lines.length}, quantity ${cart.quantityOf(dune)}, '
      'items ${cart.itemCount}, total ${cart.total.format()}, '
      'ViewModel notifications $notifications',
    );
  });

  test('the command reports running, then the result', () async {
    final cart = CartViewModel(cartRepository: InMemoryCartRepository());
    final states = <String>[];
    cart.add.addListener(
      () => states.add(
        'running=${cart.add.running} completed=${cart.add.completed} '
        'failed=${cart.add.failed}',
      ),
    );
    await cart.add.execute(dune);
    debugPrint(states.join('\n'));
  });

  test('the eleventh copy fails and leaves the cart as it was', () async {
    final cart = CartViewModel(cartRepository: InMemoryCartRepository());
    for (var i = 0; i < 10; i++) {
      await cart.add.execute(dune);
    }
    await cart.add.execute(dune);
    final result = cart.add.result;
    debugPrint(
      'failed=${cart.add.failed}, '
      'error: ${result is Failure ? result.error : '-'}, '
      'quantity ${cart.quantityOf(dune)}',
    );
    expect(cart.quantityOf(dune), 10);
  });
}
