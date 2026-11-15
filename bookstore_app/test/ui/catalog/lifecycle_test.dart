import 'package:flutter_test/flutter_test.dart';
import 'package:material_ui/material_ui.dart';

final log = <String>[];

class LifecycleProbe extends StatefulWidget {
  const LifecycleProbe({super.key, required this.title});
  final String title;

  @override
  // ignore: no_logic_in_create_state
  State<LifecycleProbe> createState() {
    log.add('createState');
    return _LifecycleProbeState();
  }
}

class _LifecycleProbeState extends State<LifecycleProbe> {
  @override
  void initState() {
    super.initState();
    log.add('initState');
  }

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    log.add('didChangeDependencies');
  }

  @override
  void didUpdateWidget(LifecycleProbe oldWidget) {
    super.didUpdateWidget(oldWidget);
    log.add('didUpdateWidget(${oldWidget.title} -> ${widget.title})');
  }

  @override
  void dispose() {
    log.add('dispose');
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    log.add('build(${widget.title})');
    return Text(widget.title, textDirection: TextDirection.ltr);
  }
}

void main() {
  testWidgets('State lifecycle', (tester) async {
    await tester.pumpWidget(const LifecycleProbe(title: 'Dune'));
    log.add('--- parent rebuilds with a new title');
    await tester.pumpWidget(const LifecycleProbe(title: 'Emma'));
    log.add('--- widget removed from the tree');
    await tester.pumpWidget(const SizedBox());
    debugPrint(log.join('\n'));
    expect(log.first, 'createState');
    expect(log.last, 'dispose');
  });
}
