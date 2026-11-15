import 'package:bookstore_app/data/sample_books.dart';
import 'package:bookstore_app/domain/models/author.dart';
import 'package:bookstore_app/domain/models/book.dart';
import 'package:bookstore_app/domain/models/money.dart';
import 'package:bookstore_app/ui/catalog/widgets/book_card.dart';
import 'package:flutter/rendering.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:material_ui/material_ui.dart';

Widget host(Book book) => MaterialApp(
  home: Scaffold(body: BookCard(book: book)),
);

String describeTitle(WidgetTester tester, String title) {
  final textElement = tester.element(find.text(title));
  final richTextElement = tester.element(
    find.descendant(of: find.text(title), matching: find.byType(RichText)),
  );
  return '${textElement.widget.runtimeType} -> ${textElement.runtimeType}\n'
      '${richTextElement.widget.runtimeType} -> ${richTextElement.runtimeType} -> '
      '${richTextElement.renderObject.runtimeType}';
}

void main() {
  testWidgets(
    'a rebuild replaces widgets but keeps elements and render objects',
    (tester) async {
      final dune = sampleBooks.first;
      await tester.pumpWidget(host(dune));
      debugPrint(describeTitle(tester, 'Dune'));

      final widgetBefore = tester.widget(find.text('Dune'));
      final elementBefore = tester.element(find.text('Dune'));
      final paragraphBefore = tester.renderObject<RenderParagraph>(
        find.descendant(of: find.text('Dune'), matching: find.byType(RichText)),
      );

      const renamed = Book(
        isbn: '9780441172719',
        title: 'Dune (Deluxe Edition)',
        author: Author(name: 'Frank Herbert'),
        price: Money(52000),
        publishedYear: 2019,
      );
      await tester.pumpWidget(host(renamed));

      final widgetAfter = tester.widget(find.text('Dune (Deluxe Edition)'));
      final elementAfter = tester.element(find.text('Dune (Deluxe Edition)'));
      final paragraphAfter = tester.renderObject<RenderParagraph>(
        find.descendant(
          of: find.text('Dune (Deluxe Edition)'),
          matching: find.byType(RichText),
        ),
      );
      debugPrint('same widget: ${identical(widgetBefore, widgetAfter)}');
      debugPrint('same element: ${identical(elementBefore, elementAfter)}');
      debugPrint(
        'same RenderParagraph: ${identical(paragraphBefore, paragraphAfter)}',
      );

      expect(identical(elementBefore, elementAfter), isTrue);
      expect(identical(paragraphBefore, paragraphAfter), isTrue);
    },
  );
}
