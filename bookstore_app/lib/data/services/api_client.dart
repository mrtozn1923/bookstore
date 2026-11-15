import 'dart:async';
import 'dart:convert';

import 'package:http/http.dart' as http;

import '../../utils/result.dart';
import 'token_store.dart';

/// A non-2xx answer of Bookstore.Api with its ProblemDetails body.
class ApiException implements Exception {
  const ApiException(this.statusCode, this.problem);

  final int statusCode;
  final Map<String, Object?> problem;

  @override
  String toString() =>
      problem['detail'] as String? ??
      problem['title'] as String? ??
      'HTTP $statusCode';
}

/// The request never got an answer: no connection, DNS, timeout.
class NetworkException implements Exception {
  const NetworkException(this.cause);
  final Object cause;

  @override
  String toString() => 'Could not reach the server ($cause)';
}

/// The only class that talks HTTP. Repositories get JSON from it, never
/// a Response, and every failure comes back as a Result.
class ApiClient {
  ApiClient({
    required this.baseUrl,
    http.Client? client,
    TokenStore? tokens,
    this.timeout = const Duration(seconds: 10),
    this.retryDelay = const Duration(milliseconds: 500),
  }) : _client = client ?? http.Client(),
       tokens = tokens ?? InMemoryTokenStore();

  final Uri baseUrl;
  final TokenStore tokens;
  final Duration timeout;
  final Duration retryDelay;
  final http.Client _client;

  /// Called when the refresh token is rejected too: the user must sign in.
  void Function()? onSessionExpired;

  Future<bool>? _refreshing;

  Future<Result<Object?>> get(
    String path, {
    Map<String, String>? query,
    bool auth = false,
  }) => _send('GET', path, query: query, auth: auth, retries: 2);

  // POST and PUT are not retried: a second POST /api/orders is a second order
  Future<Result<Object?>> post(
    String path,
    Object? body, {
    bool auth = false,
  }) => _send('POST', path, body: body, auth: auth);

  Future<Result<Object?>> put(String path, Object? body, {bool auth = false}) =>
      _send('PUT', path, body: body, auth: auth);

  Future<Result<Object?>> _send(
    String method,
    String path, {
    Map<String, String>? query,
    Object? body,
    bool auth = false,
    int retries = 0,
  }) async {
    final uri = baseUrl.replace(path: path, queryParameters: query);
    for (var attempt = 0; ; attempt++) {
      try {
        var response = await _request(method, uri, body, auth: auth);
        if (response.statusCode == 401 && auth && await _refreshTokens()) {
          response = await _request(method, uri, body, auth: auth);
        }
        // 5xx may be temporary; 4xx will not change by asking again
        if (response.statusCode < 500 || attempt >= retries) {
          return _decode(response);
        }
      } on TimeoutException {
        if (attempt >= retries) {
          return const Result.failure(NetworkException('timeout'));
        }
      } on http.ClientException catch (e) {
        if (attempt >= retries) {
          return Result.failure(NetworkException(e.message));
        }
      }
      await Future<void>.delayed(retryDelay * (attempt + 1));
    }
  }

  Future<http.Response> _request(
    String method,
    Uri uri,
    Object? body, {
    required bool auth,
  }) async {
    final request = http.Request(method, uri)
      ..headers['Accept'] = 'application/json';
    if (body != null) {
      request
        ..headers['Content-Type'] = 'application/json'
        ..body = jsonEncode(body);
    }
    if (auth) {
      if (await tokens.read() case (:final accessToken, refreshToken: _)) {
        request.headers['Authorization'] = 'Bearer $accessToken';
      }
    }
    final streamed = await _client.send(request).timeout(timeout);
    return http.Response.fromStream(streamed).timeout(timeout);
  }

  Result<Object?> _decode(http.Response response) {
    final json = response.body.isEmpty ? null : jsonDecode(response.body);
    if (response.statusCode >= 200 && response.statusCode < 300) {
      return Result.ok(json);
    }
    return Result.failure(
      ApiException(
        response.statusCode,
        json is Map<String, Object?> ? json : const {},
      ),
    );
  }

  /// Several requests can get 401 at the same moment; they share one refresh.
  Future<bool> _refreshTokens() =>
      _refreshing ??= _refresh().whenComplete(() => _refreshing = null);

  Future<bool> _refresh() async {
    final current = await tokens.read();
    if (current == null) return false;
    final response = await _request(
      'POST',
      baseUrl.replace(path: '/api/auth/refresh'),
      {'refreshToken': current.refreshToken},
      auth: false,
    );
    if (response.statusCode != 200) {
      await tokens.clear();
      onSessionExpired?.call();
      return false;
    }
    await tokens.write(tokensFromJson(jsonDecode(response.body)));
    return true;
  }
}

/// The body of POST /api/auth/login and /api/auth/refresh.
Tokens tokensFromJson(Object? json) => switch (json) {
  {'accessToken': String access, 'refreshToken': String refresh} => (
    accessToken: access,
    refreshToken: refresh,
  ),
  _ => throw const FormatException('Not a token response'),
};
