import 'package:flutter/foundation.dart';

import '../../utils/result.dart';

/// Signed-in state for the whole app; the router listens to it.
abstract class AuthRepository extends ChangeNotifier {
  bool get isSignedIn;
  String? get email;

  /// The email of the last sign-in on this device, if the app remembers it.
  String? get lastEmail => null;
  Future<Result<void>> signIn({
    required String email,
    required String password,
  });
  Future<Result<void>> signOut();
}

/// Accepts any credentials; the JWT sign-in against Bookstore.Api comes in
/// the API integration post.
class SampleAuthRepository extends AuthRepository {
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
    _email = email;
    notifyListeners();
    return const Result.ok(null);
  }

  @override
  Future<Result<void>> signOut() async {
    _email = null;
    notifyListeners();
    return const Result.ok(null);
  }
}
