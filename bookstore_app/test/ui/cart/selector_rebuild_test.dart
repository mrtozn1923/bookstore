import 'package:bookstore_app/data/sample_books.dart';
import 'package:bookstore_app/domain/models/book.dart';
import 'package:bookstore_app/domain/models/money.dart';
import 'package:bookstore_app/ui/cart/view_models/cart_view_model.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:material_ui/material_ui.dart';
import 'package:provider/provider.dart';

import '../../testing/fakes.dart';

var watchBuilds = 0;
var selectorBuilds = 0;

class WatchingBadge extends StatelessWidget {
  const WatchingBadge({super.key});

  @override
  Widget build(BuildContext context) {
    watchBuilds++;
    final count = context.watch<CartViewModel>().itemCount;
    return Text('watch: $count');
  }
}

class SelectingBadge extends StatelessWidget {
  const SelectingBadge({super.key});

  @override
  Widget build(BuildContext context) {
    return Selector<CartViewModel, int>(
      selector: (_, cart) => cart.itemCount,
      builder: (context, count, _) {
        selectorBuilds++;
        return Text('selector: $count');
      },
    );
  }
}

void main() {
  testWidgets('a price change rebuilds watch but not Selector', (tester) async {
    final repository = FakeCartRepository();
    final cart = CartViewModel(cartRepository: repository);
    await tester.pumpWidget(
      ChangeNotifierProvider.value(
        value: cart,
        child: const Directionality(
          textDirection: TextDirection.ltr,
          child: Column(children: [WatchingBadge(), SelectingBadge()]),
        ),
      ),
    );
    final dune = sampleBooks.first;

    await cart.add.execute(dune);
    await tester.pump();
    debugPrint(
      'after add: watch $watchBuilds builds, Selector $selectorBuilds builds',
    );

    // The server lowers Dune's price; the number of books does not change
    repository.changePrice(
      Book(
        isbn: dune.isbn,
        title: dune.title,
        author: dune.author,
        price: const Money(39900),
        publishedYear: dune.publishedYear,
      ),
    );
    await cart.load.execute();
    await tester.pump();
    debugPrint(
      'after price change: watch $watchBuilds builds, '
      'Selector $selectorBuilds builds, total ${cart.total.format()}',
    );
  });
}
