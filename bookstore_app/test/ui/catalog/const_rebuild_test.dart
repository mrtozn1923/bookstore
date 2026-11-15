import 'package:flutter_test/flutter_test.dart';
import 'package:material_ui/material_ui.dart';

var headerBuilds = 0;

class CatalogHeader extends StatelessWidget {
  const CatalogHeader({super.key});

  @override
  Widget build(BuildContext context) {
    headerBuilds++;
    return const Text('Catalog');
  }
}

class CartBadge extends StatefulWidget {
  const CartBadge({super.key, required this.constHeader});
  final bool constHeader;

  @override
  State<CartBadge> createState() => _CartBadgeState();
}

class _CartBadgeState extends State<CartBadge> {
  int _count = 0;

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        widget.constHeader ? const CatalogHeader() : CatalogHeader(),
        TextButton(
          onPressed: () => setState(() => _count++),
          child: Text('Cart: $_count'),
        ),
      ],
    );
  }
}

void main() {
  for (final constHeader in [false, true]) {
    testWidgets('header builds with const: $constHeader', (tester) async {
      headerBuilds = 0;
      await tester.pumpWidget(
        MaterialApp(home: CartBadge(constHeader: constHeader)),
      );
      for (var i = 0; i < 3; i++) {
        await tester.tap(find.byType(TextButton));
        await tester.pump();
      }
      debugPrint(
        'const: $constHeader -> CatalogHeader.build ran $headerBuilds times for 1 + 3 frames',
      );
    });
  }
}
