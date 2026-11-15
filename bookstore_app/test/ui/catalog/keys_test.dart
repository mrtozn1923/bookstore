import 'package:flutter_test/flutter_test.dart';
import 'package:material_ui/material_ui.dart';

// A row that remembers whether the user picked it (e.g. for a gift list)
class PickRow extends StatefulWidget {
  const PickRow({super.key, required this.title});
  final String title;

  @override
  State<PickRow> createState() => _PickRowState();
}

class _PickRowState extends State<PickRow> {
  bool _picked = false;

  @override
  Widget build(BuildContext context) => TextButton(
    onPressed: () => setState(() => _picked = !_picked),
    child: Text('${_picked ? '[x]' : '[ ]'} ${widget.title}'),
  );
}

Widget list(List<String> titles, {required bool useKeys}) => MaterialApp(
  home: Scaffold(
    body: Column(
      children: [
        for (final title in titles)
          PickRow(key: useKeys ? ValueKey(title) : null, title: title),
      ],
    ),
  ),
);

String rows(WidgetTester tester) => tester
    .widgetList<Text>(
      find.descendant(of: find.byType(PickRow), matching: find.byType(Text)),
    )
    .map((t) => t.data)
    .join(', ');

void main() {
  for (final useKeys in [false, true]) {
    testWidgets('removing the first row, keys: $useKeys', (tester) async {
      await tester.pumpWidget(
        list(['Dune', 'Emma', 'Middlemarch'], useKeys: useKeys),
      );
      await tester.tap(find.text('[ ] Dune'));
      await tester.pump();
      debugPrint('keys: $useKeys, before: ${rows(tester)}');

      await tester.pumpWidget(list(['Emma', 'Middlemarch'], useKeys: useKeys));
      debugPrint('keys: $useKeys, after : ${rows(tester)}');
      expect(find.text('[x] Emma'), useKeys ? findsNothing : findsOneWidget);
    });
  }
}
