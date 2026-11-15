import 'package:flutter_test/flutter_test.dart';
import 'package:material_ui/material_ui.dart';

var apiCalls = 0;

Future<int> fetchBookCount() async {
  apiCalls++;
  return 42;
}

// Wrong: build() can run many times, every run starts a new request
class CountInBuild extends StatelessWidget {
  const CountInBuild({super.key, required this.label});
  final String label;

  @override
  Widget build(BuildContext context) => FutureBuilder<int>(
    future: fetchBookCount(),
    builder: (context, snapshot) => Text('$label: ${snapshot.data ?? '...'}'),
  );
}

// Right: start the request once and keep the Future in the State
class CountInState extends StatefulWidget {
  const CountInState({super.key, required this.label});
  final String label;

  @override
  State<CountInState> createState() => _CountInStateState();
}

class _CountInStateState extends State<CountInState> {
  late final Future<int> _count = fetchBookCount();

  @override
  Widget build(BuildContext context) => FutureBuilder<int>(
    future: _count,
    builder: (context, snapshot) =>
        Text('${widget.label}: ${snapshot.data ?? '...'}'),
  );
}

void main() {
  testWidgets('a Future created in build runs on every rebuild', (
    tester,
  ) async {
    apiCalls = 0;
    for (final label in ['Books', 'Books ', 'Books  ']) {
      await tester.pumpWidget(MaterialApp(home: CountInBuild(label: label)));
    }
    debugPrint('Future in build(): $apiCalls API calls for 3 builds');
    expect(apiCalls, 3);
  });

  testWidgets('a Future kept in State runs once', (tester) async {
    apiCalls = 0;
    for (final label in ['Books', 'Books ', 'Books  ']) {
      await tester.pumpWidget(MaterialApp(home: CountInState(label: label)));
    }
    debugPrint('Future in State: $apiCalls API call for 3 builds');
    expect(apiCalls, 1);
  });
}
