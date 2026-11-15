import 'package:bookstore_app/data/sample_books.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:material_ui/material_ui.dart';

final hobbit = sampleBooks.last;

Widget phone(Widget body) => MaterialApp(
  home: Scaffold(body: SafeArea(child: body)),
);

/// Pumps [widget] and returns the first line of every layout error reported.
Future<List<String>> layoutErrors(WidgetTester tester, Widget widget) async {
  final errors = <String>[];
  final original = FlutterError.onError;
  FlutterError.onError = (details) =>
      errors.add(details.exceptionAsString().split('\n').first);
  await tester.pumpWidget(widget);
  FlutterError.onError = original;
  return errors;
}

String report(List<String> errors) =>
    errors.isEmpty ? 'no error' : '${errors.first} (${errors.length} error(s))';

void main() {
  testWidgets('a Row with a long title overflows', (tester) async {
    tester.view.physicalSize = const Size(320, 640);
    tester.view.devicePixelRatio = 1;
    addTearDown(tester.view.reset);

    final withoutExpanded = await layoutErrors(
      tester,
      phone(
        Row(
          children: [
            const Icon(Icons.menu_book),
            Text(hobbit.displayTitle, style: const TextStyle(fontSize: 20)),
          ],
        ),
      ),
    );
    debugPrint('without Expanded: ${report(withoutExpanded)}');

    final withExpanded = await layoutErrors(
      tester,
      phone(
        Row(
          children: [
            const Icon(Icons.menu_book),
            Expanded(
              child: Text(
                hobbit.displayTitle,
                style: const TextStyle(fontSize: 20),
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
              ),
            ),
          ],
        ),
      ),
    );
    debugPrint('with Expanded: ${report(withExpanded)}');
  });

  testWidgets('a ListView inside a Column has unbounded height', (
    tester,
  ) async {
    final inColumn = await layoutErrors(
      tester,
      phone(
        Column(
          children: [
            const Text('Reviews'),
            ListView(children: const [Text('Great book')]),
          ],
        ),
      ),
    );
    debugPrint('ListView in Column: ${report(inColumn)}');

    final inExpanded = await layoutErrors(
      tester,
      phone(
        Column(
          children: [
            const Text('Reviews'),
            Expanded(child: ListView(children: const [Text('Great book')])),
          ],
        ),
      ),
    );
    debugPrint('wrapped in Expanded: ${report(inExpanded)}');
  });

  testWidgets('Expanded outside a Row or Column', (tester) async {
    final errors = await layoutErrors(
      tester,
      phone(
        const Padding(
          padding: EdgeInsets.all(16),
          child: Expanded(child: Text('Dune')),
        ),
      ),
    );
    debugPrint('Expanded in Padding: ${report(errors)}');
  });
}
