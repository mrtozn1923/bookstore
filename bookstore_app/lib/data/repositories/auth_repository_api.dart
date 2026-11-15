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
  ApiAuthRepository(this._api) {
    _api.onSessionExpired = () {
      _email = null;
      notifyListeners(); // the router's redirect sends the user to sign-in
    };
  }

  final ApiClient _api;
  String? _email;

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
        _email = email;
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
