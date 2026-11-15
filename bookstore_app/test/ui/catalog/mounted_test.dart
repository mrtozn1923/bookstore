import 'dart:async';

import 'package:flutter_test/flutter_test.dart';
import 'package:material_ui/material_ui.dart';

class SaveFavorite extends StatefulWidget {
  const SaveFavorite({
    super.key,
    required this.save,
    required this.checkMounted,
  });
  final Future<void> Function() save;
  final bool checkMounted;

  @override
  State<SaveFavorite> createState() => SaveFavoriteState();
}

class SaveFavoriteState extends State<SaveFavorite> {
  String _status = 'idle';

  Future<void> saveNow() async {
    setState(() => _status = 'saving');
    await widget.save();
    // The user may have left the screen while we were waiting
    if (widget.checkMounted && !mounted) return;
    setState(() => _status = 'saved');
  }

  @override
  Widget build(BuildContext context) =>
      TextButton(onPressed: saveNow, child: Text(_status));
}

void main() {
  for (final checkMounted in [false, true]) {
    testWidgets('leaving the screen during a save, checkMounted: $checkMounted', (
      tester,
    ) async {
      final pending = Completer<void>();
      await tester.pumpWidget(
        MaterialApp(
          home: SaveFavorite(
            save: () => pending.future,
            checkMounted: checkMounted,
          ),
        ),
      );
      final saving = tester
          .state<SaveFavoriteState>(find.byType(SaveFavorite))
          .saveNow();
      await tester.pumpWidget(
        const MaterialApp(home: Text('Cart')),
      ); // the screen is gone
      pending.complete();

      Object? error;
      try {
        await saving;
      } on FlutterError catch (e) {
        error = e;
      }
      debugPrint(
        'checkMounted: $checkMounted -> ${error == null ? 'no error' : error.toString().split('\n').first}',
      );
      expect(error == null, checkMounted);
    });
  }
}
