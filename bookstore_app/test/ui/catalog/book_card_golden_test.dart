@Tags(['golden'])
library;

import 'package:bookstore_app/data/sample_books.dart';
import 'package:bookstore_app/ui/catalog/widgets/book_card.dart';
import 'package:bookstore_app/ui/core/themes/theme.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:material_ui/material_ui.dart';

void main() {
  testWidgets('BookCard looks the same as the approved image', (tester) async {
    tester.view.physicalSize = const Size(390, 120);
    tester.view.devicePixelRatio = 1;
    addTearDown(tester.view.reset);
    await tester.pumpWidget(
      MaterialApp(
        theme: AppTheme.light,
        home: Scaffold(body: BookCard(book: sampleBooks.first)),
      ),
    );
    await expectLater(
      find.byType(BookCard),
      matchesGoldenFile('goldens/book_card_dune.png'),
    );
  });
}
