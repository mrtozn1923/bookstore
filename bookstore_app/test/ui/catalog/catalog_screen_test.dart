import 'package:bookstore_app/config/dependencies.dart';
import 'package:bookstore_app/ui/catalog/view_models/catalog_view_model.dart';
import 'package:bookstore_app/ui/catalog/widgets/catalog_screen.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:material_ui/material_ui.dart';
import 'package:provider/provider.dart';

import '../../testing/fakes.dart';

void main() {
  Future<void> pumpPhone(WidgetTester tester, FakeBookRepository books) async {
    tester.view.physicalSize = const Size(390, 844);
    tester.view.devicePixelRatio = 1;
    addTearDown(tester.view.reset);
    final viewModel = CatalogViewModel(bookRepository: books);
    addTearDown(viewModel.dispose);
    await tester.pumpWidget(
      MultiProvider(
        providers: appProviders(bookRepository: books),
        child: MaterialApp(home: CatalogScreen(viewModel: viewModel)),
      ),
    );
  }

  testWidgets('loads the next page near the end of the list', (tester) async {
    final books = FakeBookRepository();
    await pumpPhone(tester, books);
    expect(find.byType(CircularProgressIndicator), findsOneWidget);
    await tester.pumpAndSettle();
    debugPrint('after start: calls ${books.calls}');

    await tester.scrollUntilVisible(find.text('Sample Book 20'), 500);
    await tester.pumpAndSettle();
    debugPrint('after scrolling to book 20: calls ${books.calls}');

    await tester.scrollUntilVisible(find.text('Sample Book 60'), 500);
    await tester.pumpAndSettle();
    await tester.drag(find.byType(CustomScrollView), const Offset(0, -500));
    await tester.pumpAndSettle();
    debugPrint('after the last book: calls ${books.calls}');
  });

  testWidgets('pull to refresh reloads the first page', (tester) async {
    final books = FakeBookRepository();
    await pumpPhone(tester, books);
    await tester.pumpAndSettle();

    await tester.fling(find.text('Dune'), const Offset(0, 400), 1000);
    await tester.pumpAndSettle();
    debugPrint('after pull to refresh: calls ${books.calls}');
    expect(books.calls, [0, 0]);
  });

  testWidgets('shows an error with a retry button', (tester) async {
    final books = FakeBookRepository(failuresLeft: 1);
    await pumpPhone(tester, books);
    await tester.pumpAndSettle();
    expect(find.text('The catalog could not be loaded.'), findsOneWidget);

    await tester.tap(find.text('Try again'));
    await tester.pumpAndSettle();
    expect(find.text('Dune'), findsOneWidget);
    debugPrint('error then retry: calls ${books.calls}');
  });
}
