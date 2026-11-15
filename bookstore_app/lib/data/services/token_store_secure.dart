import 'package:flutter_secure_storage/flutter_secure_storage.dart';

import 'token_store.dart';

/// Tokens in the Keychain (iOS) or in Keystore-encrypted storage (Android).
class SecureTokenStore implements TokenStore {
  SecureTokenStore([this._storage = const FlutterSecureStorage()]);

  final FlutterSecureStorage _storage;
  static const _access = 'access_token';
  static const _refresh = 'refresh_token';

  @override
  Future<Tokens?> read() async {
    final access = await _storage.read(key: _access);
    final refresh = await _storage.read(key: _refresh);
    if (access == null || refresh == null) return null;
    return (accessToken: access, refreshToken: refresh);
  }

  @override
  Future<void> write(Tokens tokens) async {
    await _storage.write(key: _access, value: tokens.accessToken);
    await _storage.write(key: _refresh, value: tokens.refreshToken);
  }

  @override
  Future<void> clear() async {
    await _storage.delete(key: _access);
    await _storage.delete(key: _refresh);
  }
}
