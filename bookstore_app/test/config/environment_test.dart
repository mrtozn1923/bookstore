import 'package:bookstore_app/app.dart';
import 'package:bookstore_app/config/api_config.dart';
import 'package:bookstore_app/config/dependencies.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:material_ui/material_ui.dart';
import 'package:provider/provider.dart';

// flutter test --dart-define-from-file=config/staging.json test/config
void main() {
  testWidgets('the build knows its environment', (tester) async {
    await tester.pumpWidget(
      MultiProvider(providers: appProviders(), child: const BookstoreApp()),
    );
    await tester.pumpAndSettle();
    // The DEBUG banner comes from MaterialApp in debug builds
    final banners = tester.widgetList<Banner>(find.byType(Banner));
    debugPrint(
      'APP_ENV=$appEnvironment, API_BASE_URL=$apiBaseUrl, '
      'banners: ${banners.map((b) => b.message).join(', ')}',
    );
  });
}
