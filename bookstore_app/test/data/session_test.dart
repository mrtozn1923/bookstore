import 'package:bookstore_app/app.dart';
import 'package:bookstore_app/config/dependencies.dart';
import 'package:bookstore_app/data/repositories/auth_repository_api.dart';
import 'package:bookstore_app/data/repositories/book_repository_cached.dart';
import 'package:bookstore_app/data/services/api_client.dart';
import 'package:bookstore_app/data/services/token_store_secure.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:provider/provider.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:shared_preferences_platform_interface/in_memory_shared_preferences_async.dart';
import 'package:shared_preferences_platform_interface/shared_preferences_async_platform_interface.dart';

ApiClient apiAnswering(int status) => ApiClient(
  baseUrl: Uri.parse('http://127.0.0.1:52813'),
  tokens: SecureTokenStore(),
  client: MockClient(
    (_) async => http.Response(
      '{"tokenType":"Bearer","accessToken":"test-access","expiresIn":3600,'
      '"refreshToken":"test-refresh"}',
      status,
    ),
  ),
);

void main() {
  setUp(() {
    // In tests the plugins have no platform side: in-memory stand-ins
    FlutterSecureStorage.setMockInitialValues({});
    SharedPreferencesAsyncPlatform.instance =
        InMemorySharedPreferencesAsync.empty();
  });

  test('a restarted app is still signed in', () async {
    final first = ApiAuthRepository(
      apiAnswering(200),
      SharedPreferencesAsync(),
    );
    await first.signIn(email: 'ada@example.com', password: 'not-checked');

    // A new process: new objects, the same device storage
    final second = ApiAuthRepository(
      apiAnswering(200),
      SharedPreferencesAsync(),
    );
    debugPrint('before restore: ${second.isSignedIn}');
    await second.restore();
    debugPrint('after restore: ${second.isSignedIn}, ${second.email}');
    debugPrint(
      'stored keys: ${(await const FlutterSecureStorage().readAll()).keys.toList()}',
    );

    await second.signOut();
    final third = ApiAuthRepository(
      apiAnswering(200),
      SharedPreferencesAsync(),
    );
    await third.restore();
    debugPrint(
      'after sign-out: ${third.isSignedIn}, form prefilled with ${third.lastEmail}',
    );
  });

  testWidgets('saved books come with a banner', (tester) async {
    final status = OfflineStatus()
      ..showingSaved(DateTime.utc(2026, 11, 15, 10));
    await tester.pumpWidget(
      MultiProvider(
        providers: appProviders(offlineStatus: status),
        child: const BookstoreApp(),
      ),
    );
    await tester.pumpAndSettle();
    debugPrint(
      'banner: ${find.textContaining('Offline: showing books saved on').evaluate().length}',
    );
  });
}
