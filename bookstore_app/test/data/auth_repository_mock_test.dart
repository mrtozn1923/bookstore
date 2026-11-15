import 'package:bookstore_app/data/repositories/auth_repository_api.dart';
import 'package:bookstore_app/data/services/api_client.dart';
import 'package:bookstore_app/data/services/token_store.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:shared_preferences_platform_interface/in_memory_shared_preferences_async.dart';
import 'package:shared_preferences_platform_interface/shared_preferences_async_platform_interface.dart';

class MockTokenStore extends Mock implements TokenStore {}

void main() {
  // any() needs a value of the parameter's type; records are no exception
  setUpAll(() => registerFallbackValue((accessToken: '', refreshToken: '')));

  setUp(() {
    SharedPreferencesAsyncPlatform.instance =
        InMemorySharedPreferencesAsync.empty();
  });

  test('sign-out deletes the tokens exactly once', () async {
    // The question is "was clear() called?", not "what is stored?":
    // an interaction, which is what a mock is for
    final tokens = MockTokenStore();
    when(tokens.clear).thenAnswer((_) async {});
    final auth = ApiAuthRepository(
      ApiClient(baseUrl: Uri.parse('http://127.0.0.1:52813'), tokens: tokens),
      SharedPreferencesAsync(),
    );

    await auth.signOut();

    verify(tokens.clear).called(1);
    verifyNever(() => tokens.write(any()));
    debugPrint('clear() called once, write() never');
  });

  test('a method nobody stubbed', () async {
    final tokens = MockTokenStore();
    try {
      await tokens.read();
    } on TypeError catch (e) {
      debugPrint('${e.runtimeType}: $e');
    }
  });
}
