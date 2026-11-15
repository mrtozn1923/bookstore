import 'package:shared_preferences/shared_preferences.dart';

import '../../utils/result.dart';
import '../services/api_client.dart';
import 'auth_repository.dart';

class InvalidCredentials implements Exception {
  const InvalidCredentials();

  @override
  String toString() => 'The email or password is not correct.';
}

/// Signs in with ASP.NET Core Identity's /login and keeps the tokens in the
/// ApiClient's TokenStore; ApiClient adds them to requests.
class ApiAuthRepository extends AuthRepository {
  ApiAuthRepository(this._api, this._prefs) {
    _api.onSessionExpired = () {
      _email = null;
      notifyListeners(); // the router's redirect sends the user to sign-in
    };
  }

  final ApiClient _api;
  // Not a secret: only used to show who is signed in and to prefill the form
  final SharedPreferencesAsync _prefs;
  static const _emailKey = 'last_email';
  String? _email;
  String? _lastEmail;

  @override
  String? get lastEmail => _lastEmail;

  /// Called once at start-up: tokens on the device mean a signed-in user.
  Future<void> restore() async {
    _lastEmail = await _prefs.getString(_emailKey);
    if (await _api.tokens.read() != null) _email = _lastEmail;
  }

  @override
  bool get isSignedIn => _email != null;

  @override
  String? get email => _email;

  @override
  Future<Result<void>> signIn({
    required String email,
    required String password,
  }) async {
    final result = await _api.post('/api/auth/login', {
      'email': email,
      'password': password,
    });
    switch (result) {
      case Ok(:final value):
        await _api.tokens.write(tokensFromJson(value));
        await _prefs.setString(_emailKey, email);
        _email = _lastEmail = email;
        notifyListeners();
        return const Result.ok(null);
      case Failure(error: ApiException(statusCode: 401)):
        return const Result.failure(InvalidCredentials());
      case Failure(:final error):
        return Result.failure(error);
    }
  }

  @override
  Future<Result<void>> signOut() async {
    await _api.tokens.clear();
    _email = null;
    notifyListeners();
    return const Result.ok(null);
  }
}
