import 'dart:typed_data';
import 'dart:ui' as ui;

import 'package:flutter_test/flutter_test.dart';
import 'package:material_ui/material_ui.dart';

/// A 1200 x 1800 PNG, the size of a typical cover scan.
Future<Uint8List> bigCover() async {
  final recorder = ui.PictureRecorder();
  Canvas(recorder).drawRect(
    const Rect.fromLTWH(0, 0, 1200, 1800),
    Paint()..color = const Color(0xFF3F51B5),
  );
  final image = await recorder.endRecording().toImage(1200, 1800);
  final data = await image.toByteData(format: ui.ImageByteFormat.png);
  return data!.buffer.asUint8List();
}

Future<ui.Image> decodedImage(WidgetTester tester, Image widget) async {
  await tester.pumpWidget(Center(child: widget));
  await tester.runAsync(
    () => precacheImage(widget.image, tester.element(find.byWidget(widget))),
  );
  await tester.pump();
  return tester.widget<RawImage>(find.byType(RawImage)).image!;
}

String describe(String label, ui.Image image) {
  final kb = image.width * image.height * 4 / 1024;
  return '$label: decoded ${image.width}x${image.height}, '
      '${kb.toStringAsFixed(0)} KB in memory';
}

void main() {
  testWidgets('cacheWidth decodes at the drawn size', (tester) async {
    final bytes = (await tester.runAsync(bigCover))!;

    final full = await decodedImage(
      tester,
      Image.memory(bytes, width: 48, height: 72),
    );
    debugPrint(describe('without cacheWidth', full));

    // 48 dp thumbnail on a 3x screen = 144 physical pixels
    final small = await decodedImage(
      tester,
      Image.memory(bytes, width: 48, height: 72, cacheWidth: 144),
    );
    debugPrint(describe('cacheWidth: 144', small));
  });
}
