import 'package:bookstore_app/app.dart';
import 'package:bookstore_app/config/dependencies.dart';
import 'package:bookstore_app/data/sample_books.dart';
import 'package:bookstore_app/ui/core/formatting.dart';
import 'package:bookstore_app/ui/core/themes/bookstore_colors.dart';
import 'package:bookstore_app/ui/core/themes/theme.dart';
import 'package:cupertino_ui/cupertino_ui.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:material_ui/material_ui.dart';
import 'package:provider/provider.dart';

String hex(Color c) =>
    '#${c.toARGB32().toRadixString(16).substring(2).toUpperCase()}';

/// WCAG contrast ratio between two colors.
double contrast(Color a, Color b) {
  final la = a.computeLuminance(), lb = b.computeLuminance();
  final (hi, lo) = la > lb ? (la, lb) : (lb, la);
  return (hi + 0.05) / (lo + 0.05);
}

void main() {
  test('one seed, two schemes', () {
    for (final (name, theme) in [
      ('light', AppTheme.light),
      ('dark', AppTheme.dark),
    ]) {
      final s = theme.colorScheme;
      debugPrint(
        '$name: primary ${hex(s.primary)}, onPrimary ${hex(s.onPrimary)}, '
        'surface ${hex(s.surface)}, onSurface ${hex(s.onSurface)}, '
        'primary/onPrimary contrast ${contrast(s.primary, s.onPrimary).toStringAsFixed(1)}',
      );
    }
  });

  test('our own tokens on the surface', () {
    for (final (name, theme, colors) in [
      ('light', AppTheme.light, BookstoreColors.light),
      ('dark', AppTheme.dark, BookstoreColors.dark),
    ]) {
      final ratio = contrast(colors.price, theme.colorScheme.surface);
      debugPrint('$name: price on surface ${ratio.toStringAsFixed(1)}');
      expect(ratio, greaterThanOrEqualTo(4.5));
    }
    final half = BookstoreColors.light.lerp(BookstoreColors.dark, 0.5);
    debugPrint('lerp 0.5 price: ${hex(half.price)}');
  });

  testWidgets('the app follows the platform brightness', (tester) async {
    for (final brightness in Brightness.values) {
      tester.platformDispatcher.platformBrightnessTestValue = brightness;
      await tester.pumpWidget(
        MultiProvider(providers: appProviders(), child: const BookstoreApp()),
      );
      await tester.pumpAndSettle();
      final context = tester.element(find.text('Dune'));
      debugPrint(
        'platform $brightness -> theme ${Theme.of(context).brightness}, '
        'price color ${hex(BookstoreColors.of(context).price)}',
      );
    }
    tester.platformDispatcher.clearPlatformBrightnessTestValue();
  });

  testWidgets('Turkish locale: Material strings, dates and money', (
    tester,
  ) async {
    for (final locale in const [Locale('en'), Locale('tr')]) {
      late BuildContext context;
      await tester.pumpWidget(
        MaterialApp(
          locale: locale,
          localizationsDelegates: GlobalMaterialLocalizations.delegates,
          supportedLocales: const [Locale('en'), Locale('tr')],
          home: Builder(
            builder: (c) {
              context = c;
              return const SizedBox();
            },
          ),
        ),
      );
      final material = MaterialLocalizations.of(context);
      debugPrint(
        '$locale: ${material.okButtonLabel} / ${material.cancelButtonLabel}, '
        '${context.formatDate(DateTime.utc(2026, 11, 15))}, '
        '${context.formatPrice(sampleBooks.first.price)}',
      );
    }
  });

  testWidgets('Switch.adaptive follows the platform', (tester) async {
    for (final platform in [TargetPlatform.android, TargetPlatform.iOS]) {
      debugDefaultTargetPlatformOverride = platform;
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(body: Switch.adaptive(value: true, onChanged: (_) {})),
        ),
      );
      final switches = find.byType(Switch);
      debugPrint(
        '$platform -> ${switches.evaluate().length} Switch, '
        '${find.byType(CupertinoSwitch).evaluate().length} CupertinoSwitch, '
        'size ${tester.getSize(switches)}',
      );
    }
    debugDefaultTargetPlatformOverride = null;
  });
}
