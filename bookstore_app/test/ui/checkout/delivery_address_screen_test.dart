import 'package:bookstore_app/domain/models/delivery_address.dart';
import 'package:bookstore_app/ui/checkout/widgets/delivery_address_screen.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:material_ui/material_ui.dart';

Finder field(String label) => find.widgetWithText(TextFormField, label);

bool hasFocus(WidgetTester tester, String label) => tester
    .widget<EditableText>(
      find.descendant(of: field(label), matching: find.byType(EditableText)),
    )
    .focusNode
    .hasFocus;

Future<void> fillValid(WidgetTester tester) async {
  await tester.enterText(field('Full name'), 'Ada Lovelace');
  await tester.enterText(field('Address'), '1 Example Street');
  await tester.enterText(field('City'), 'Istanbul');
  await tester.enterText(field('Postal code'), '34710');
  await tester.enterText(field('Phone'), '0212 555 01 01');
}

const problem = {
  'title': 'One or more validation errors occurred.',
  'status': 400,
  'errors': {
    'ShippingAddress.PostalCode': ['We do not deliver to this postal code.'],
    'Items[0].Quantity': ['Only 2 copies are in stock.'],
  },
};

void main() {
  testWidgets('focus jumps to the first invalid field', (tester) async {
    await tester.pumpWidget(
      MaterialApp(home: DeliveryAddressScreen(onSubmit: (a) async => null)),
    );
    await tester.enterText(field('Full name'), 'Ada Lovelace');
    await tester.enterText(field('Postal code'), '34a7x1');
    await tester.tap(find.text('Use this address'));
    await tester.pump();
    final postal = tester.widget<EditableText>(
      find.descendant(
        of: field('Postal code'),
        matching: find.byType(EditableText),
      ),
    );
    debugPrint(
      'postal code text after formatters: "${postal.controller.text}"',
    );
    debugPrint(
      'focused: Address ${hasFocus(tester, 'Address')}, '
      'Full name ${hasFocus(tester, 'Full name')}',
    );
  });

  testWidgets('server errors go to their fields', (tester) async {
    final submitted = <DeliveryAddress>[];
    await tester.pumpWidget(
      MaterialApp(
        home: DeliveryAddressScreen(
          onSubmit: (address) async {
            submitted.add(address);
            return problem;
          },
        ),
      ),
    );
    await fillValid(tester);
    await tester.tap(find.text('Use this address'));
    await tester.pumpAndSettle();

    debugPrint(
      'postal code error: '
      '${find.text('We do not deliver to this postal code.').evaluate().length}, '
      'form error: ${find.text('Only 2 copies are in stock.').evaluate().length}, '
      'postal code focused: ${hasFocus(tester, 'Postal code')}',
    );

    await tester.enterText(field('Postal code'), '06420');
    await tester.pump();
    debugPrint(
      'after editing: '
      '${find.text('We do not deliver to this postal code.').evaluate().length} postal code error',
    );
    expect(submitted.single.city, 'Istanbul');
  });

  testWidgets('the keyboard shrinks the body, the field stays visible', (
    tester,
  ) async {
    tester.view.physicalSize = const Size(320, 568);
    tester.view.devicePixelRatio = 1;
    addTearDown(tester.view.reset);
    await tester.pumpWidget(
      MaterialApp(home: DeliveryAddressScreen(onSubmit: (a) async => null)),
    );
    double bodyMaxHeight() => tester
        .renderObject<RenderBox>(
          find.ancestor(
            of: find.byType(SingleChildScrollView),
            matching: find.byType(SafeArea),
          ),
        )
        .constraints
        .maxHeight;
    debugPrint(
      'no keyboard: body max height ${bodyMaxHeight()}, '
      'phone field bottom ${tester.getRect(field('Phone')).bottom}',
    );

    // A 260 px soft keyboard opens when the phone field is tapped
    tester.view.viewInsets = const FakeViewPadding(bottom: 260);
    await tester.showKeyboard(field('Phone'));
    await tester.pumpAndSettle();
    debugPrint(
      'keyboard 260: body max height ${bodyMaxHeight()}, '
      'phone field bottom ${tester.getRect(field('Phone')).bottom} '
      '(keyboard top ${568 - 260})',
    );
  });
}
