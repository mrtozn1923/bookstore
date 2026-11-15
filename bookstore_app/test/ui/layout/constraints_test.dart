import 'package:flutter/rendering.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:material_ui/material_ui.dart';

final log = <String>[];

/// Prints the constraints it receives from its parent, then picks a size.
Widget probe(String label, {Widget? child}) => LayoutBuilder(
  builder: (context, constraints) {
    log.add('$label: $constraints');
    return child ?? const SizedBox(width: 100, height: 40);
  },
);

void main() {
  setUp(log.clear);

  testWidgets('constraints go down', (tester) async {
    tester.view.physicalSize = const Size(390, 844);
    tester.view.devicePixelRatio = 1;
    addTearDown(tester.view.reset);

    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: probe(
            'Scaffold body',
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Column(
                children: [
                  probe('Column child'),
                  SizedBox(
                    width: 200,
                    height: 100,
                    child: probe('SizedBox(200x100) child'),
                  ),
                  Center(child: probe('Center child')),
                  Row(
                    children: [
                      probe('Row child'),
                      Expanded(child: probe('Expanded in Row')),
                    ],
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
    debugPrint(log.join('\n'));
  });

  testWidgets('sizes go up, parent sets position', (tester) async {
    tester.view.physicalSize = const Size(390, 844);
    tester.view.devicePixelRatio = 1;
    addTearDown(tester.view.reset);

    await tester.pumpWidget(
      const MaterialApp(
        home: Scaffold(
          body: Padding(
            padding: EdgeInsets.all(16),
            child: Column(
              key: Key('column'),
              children: [
                Text('Dune', key: Key('title')),
                SizedBox(key: Key('box'), width: 200, height: 100),
              ],
            ),
          ),
        ),
      ),
    );
    for (final key in ['title', 'box', 'column']) {
      final box = tester.renderObject<RenderBox>(find.byKey(Key(key)));
      debugPrint(
        '$key: size ${box.size}, offset in parent '
        '${(box.parentData! as BoxParentData).offset}',
      );
    }
  });
}
