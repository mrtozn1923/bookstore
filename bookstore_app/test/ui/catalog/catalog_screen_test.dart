import 'package:bookstore_app/data/sample_catalog.dart';
import 'package:bookstore_app/domain/models/book.dart';
import 'package:bookstore_app/ui/catalog/widgets/catalog_screen.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:material_ui/material_ui.dart';

void main() {
  late List<int> calls;
  late SampleCatalog catalog;

  setUp(() {
    calls = [];
    catalog = SampleCatalog(total: 60, pageSize: 20);
  });

  Future<List<Book>> loadPage(int page) {
    calls.add(page);
    return catalog.page(page);
  }

  Future<void> pumpPhone(WidgetTester tester, CatalogPageLoader loader) async {
    tester.view.physicalSize = const Size(390, 844);
    tester.view.devicePixelRatio = 1;
    addTearDown(tester.view.reset);
    await tester.pumpWidget(MaterialApp(home: CatalogScreen(loadPage: loader)));
  }

  testWidgets('loads the next page near the end of the list', (tester) async {
    await pumpPhone(tester, loadPage);
    expect(find.byType(CircularProgressIndicator), findsOneWidget);
    await tester.pumpAndSettle();
    debugPrint('after start: calls $calls');

    await tester.scrollUntilVisible(find.text('Sample Book 20'), 500);
    await tester.pumpAndSettle();
    debugPrint('after scrolling to book 20: calls $calls');

    await tester.scrollUntilVisible(find.text('Sample Book 60'), 500);
    await tester.pumpAndSettle();
    await tester.drag(find.byType(CustomScrollView), const Offset(0, -500));
    await tester.pumpAndSettle();
    debugPrint('after the last book: calls $calls');
  });

  testWidgets('pull to refresh reloads the first page', (tester) async {
    await pumpPhone(tester, loadPage);
    await tester.pumpAndSettle();

    await tester.fling(find.text('Dune'), const Offset(0, 400), 1000);
    await tester.pumpAndSettle();
    debugPrint('after pull to refresh: calls $calls');
    expect(calls, [0, 0]);
  });

  testWidgets('shows an error with a retry button', (tester) async {
    var failures = 1;
    Future<List<Book>> flaky(int page) async {
      calls.add(page);
      if (failures-- > 0) throw Exception('timeout');
      return catalog.page(page);
    }

    await pumpPhone(tester, flaky);
    await tester.pumpAndSettle();
    expect(find.text('The catalog could not be loaded.'), findsOneWidget);

    await tester.tap(find.text('Try again'));
    await tester.pumpAndSettle();
    expect(find.text('Dune'), findsOneWidget);
    debugPrint('error then retry: calls $calls');
  });

  testWidgets('an empty catalog shows the empty view', (tester) async {
    await pumpPhone(tester, (page) async => const []);
    await tester.pumpAndSettle();
    expect(find.text('No books in the catalog yet.'), findsOneWidget);
  });
}
