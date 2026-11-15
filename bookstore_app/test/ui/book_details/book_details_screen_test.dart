import 'package:bookstore_app/data/sample_books.dart';
import 'package:bookstore_app/ui/book_details/widgets/book_cover.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:material_ui/material_ui.dart';
import 'package:bookstore_app/ui/book_details/widgets/book_details_screen.dart';

Future<void> pumpDetails(
  WidgetTester tester,
  Size size, {
  double textScale = 1,
}) async {
  tester.view.physicalSize = size;
  tester.view.devicePixelRatio = 1;
  addTearDown(tester.view.reset);
  await tester.pumpWidget(
    MaterialApp(
      home: MediaQuery.withClampedTextScaling(
        minScaleFactor: textScale,
        maxScaleFactor: textScale,
        child: BookDetailsScreen(book: sampleBooks.last),
      ),
    ),
  );
}

String where(WidgetTester tester, Finder finder) {
  final rect = tester.getRect(finder);
  return '(${rect.left.round()}, ${rect.top.round()}) '
      '${rect.width.round()}x${rect.height.round()}';
}

void main() {
  for (final (name, size) in [
    ('phone', const Size(390, 844)),
    ('tablet', const Size(1024, 768)),
  ]) {
    testWidgets('$name layout', (tester) async {
      await pumpDetails(tester, size);
      final cover = find.byType(BookCover);
      final title = find.text('The Hobbit: or There and Back Again');
      debugPrint(
        '$name ${size.width.round()}: cover ${where(tester, cover)}, '
        'title ${where(tester, title)}',
      );
      final coverRect = tester.getRect(cover);
      final titleRect = tester.getRect(title);
      if (size.width >= BookDetailsScreen.twoColumnMinWidth) {
        expect(titleRect.left, greaterThan(coverRect.right));
      } else {
        expect(titleRect.top, greaterThan(coverRect.bottom));
      }
    });
  }

  testWidgets('320 dp and 2x text do not overflow', (tester) async {
    await pumpDetails(tester, const Size(320, 568), textScale: 2);
    expect(tester.takeException(), isNull);
    debugPrint('320 dp, text x2: no overflow');
  });
}
