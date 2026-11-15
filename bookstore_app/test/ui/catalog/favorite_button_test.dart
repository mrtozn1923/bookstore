import 'package:bookstore_app/ui/catalog/widgets/favorite_button.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:material_ui/material_ui.dart';

Widget host(Widget child) => MaterialApp(home: Scaffold(body: child));

void main() {
  testWidgets('tapping toggles the icon and reports the new value', (
    tester,
  ) async {
    final changes = <bool>[];
    await tester.pumpWidget(
      host(FavoriteButton(isbn: '9780441172719', onChanged: changes.add)),
    );
    expect(find.byIcon(Icons.favorite_border), findsOneWidget);

    await tester.tap(find.byType(FavoriteButton));
    await tester.pump();

    expect(find.byIcon(Icons.favorite), findsOneWidget);
    expect(changes, [true]);
  });

  testWidgets('a different book resets the state in didUpdateWidget', (
    tester,
  ) async {
    await tester.pumpWidget(host(const FavoriteButton(isbn: '9780441172719')));
    await tester.tap(find.byType(FavoriteButton));
    await tester.pump();
    expect(find.byIcon(Icons.favorite), findsOneWidget);

    await tester.pumpWidget(host(const FavoriteButton(isbn: '9780141439587')));
    expect(find.byIcon(Icons.favorite_border), findsOneWidget);
  });
}
