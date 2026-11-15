import 'package:flutter_test/flutter_test.dart';
import 'package:material_ui/material_ui.dart';

var created = 0;
var built = 0;

class CountingRow extends StatelessWidget {
  const CountingRow(this.index, {super.key});
  final int index;

  @override
  Widget build(BuildContext context) {
    built++;
    return SizedBox(height: 56, child: Text('Book $index'));
  }
}

Widget row(int index) {
  created++;
  return CountingRow(index);
}

Widget host(Widget list) => MaterialApp(home: Scaffold(body: list));

int alive(WidgetTester tester) =>
    find.byType(CountingRow, skipOffstage: false).evaluate().length;

void main() {
  setUp(() => created = built = 0);

  testWidgets('ListView(children:)', (tester) async {
    await tester.pumpWidget(
      host(ListView(children: [for (var i = 0; i < 1000; i++) row(i)])),
    );
    debugPrint(
      'children: created $created, built $built, alive ${alive(tester)}',
    );
  });

  testWidgets('ListView.builder', (tester) async {
    await tester.pumpWidget(
      host(ListView.builder(itemCount: 1000, itemBuilder: (_, i) => row(i))),
    );
    debugPrint(
      'builder : created $created, built $built, alive ${alive(tester)}',
    );

    await tester.drag(find.byType(ListView), const Offset(0, -5000));
    await tester.pump();
    debugPrint(
      'builder after scrolling 5000 px: created $created, built $built, '
      'alive ${alive(tester)}',
    );
  });

  testWidgets('ListView.builder with shrinkWrap in a scroll view', (
    tester,
  ) async {
    await tester.pumpWidget(
      host(
        SingleChildScrollView(
          child: Column(
            children: [
              const Text('Catalog'),
              ListView.builder(
                shrinkWrap: true,
                physics: const NeverScrollableScrollPhysics(),
                itemCount: 1000,
                itemBuilder: (_, i) => row(i),
              ),
            ],
          ),
        ),
      ),
    );
    debugPrint(
      'shrinkWrap: created $created, built $built, alive ${alive(tester)}',
    );
  });
}
