import 'package:bookstore_app/ui/auth/widgets/sign_in_screen.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:material_ui/material_ui.dart';

Finder field(String label) => find.widgetWithText(TextFormField, label);

void main() {
  testWidgets('empty submit shows every error, typing clears them', (
    tester,
  ) async {
    final calls = <String>[];
    await tester.pumpWidget(
      MaterialApp(
        home: SignInScreen(
          onSignIn: (email, password) async {
            calls.add(email);
            return null;
          },
        ),
      ),
    );

    await tester.tap(find.widgetWithText(FilledButton, 'Sign in'));
    await tester.pump();
    debugPrint(
      'after empty submit: '
      '${find.text('This field is required').evaluate().length} errors',
    );

    await tester.enterText(field('Email'), 'ada@');
    await tester.pump();
    debugPrint(
      'typed "ada@": '
      '${find.text('Enter a valid email address').evaluate().length} email error',
    );

    await tester.enterText(field('Email'), 'ada@example.com');
    await tester.enterText(field('Password'), 'change-me-123');
    await tester.pump();
    debugPrint(
      'fixed: ${find.textContaining('required').evaluate().length} '
      'required errors left',
    );

    await tester.tap(find.widgetWithText(FilledButton, 'Sign in'));
    await tester.pump();
    expect(calls, ['ada@example.com']);
  });

  testWidgets('the keyboard action moves focus to the next field', (
    tester,
  ) async {
    await tester.pumpWidget(
      MaterialApp(home: SignInScreen(onSignIn: (e, p) async => null)),
    );
    await tester.tap(field('Email'));
    await tester.pump();
    final email = tester.state<EditableTextState>(
      find.descendant(of: field('Email'), matching: find.byType(EditableText)),
    );
    final password = tester.state<EditableTextState>(
      find.descendant(
        of: field('Password'),
        matching: find.byType(EditableText),
      ),
    );
    debugPrint('before next: email focused ${email.widget.focusNode.hasFocus}');
    await tester.testTextInput.receiveAction(TextInputAction.next);
    await tester.pump();
    debugPrint(
      'after next: password focused ${password.widget.focusNode.hasFocus}',
    );
  });

  testWidgets('a server error is shown above the form', (tester) async {
    await tester.pumpWidget(
      MaterialApp(
        home: SignInScreen(
          onSignIn: (email, password) async => 'Wrong email or password.',
        ),
      ),
    );
    await tester.enterText(field('Email'), 'ada@example.com');
    await tester.enterText(field('Password'), 'change-me-123');
    await tester.testTextInput.receiveAction(TextInputAction.done);
    await tester.pump();
    expect(find.text('Wrong email or password.'), findsOneWidget);
  });
}
