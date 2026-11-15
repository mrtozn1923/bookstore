import 'dart:async';
import 'dart:convert';

import 'package:bookstore_app/data/repositories/book_repository.dart';
import 'package:bookstore_app/data/repositories/book_repository_api.dart';
import 'package:bookstore_app/data/services/api_client.dart';
import 'package:bookstore_app/data/services/api_json.dart';
import 'package:bookstore_app/data/services/token_store.dart';
import 'package:bookstore_app/domain/models/book.dart';
import 'package:bookstore_app/utils/result.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';

import '../testing/api_fixtures.dart';

final base = Uri.parse('http://127.0.0.1:52813');

http.Response json(String body, int status) => http.Response(
  body,
  status,
  headers: {'content-type': 'application/json; charset=utf-8'},
);

void main() {
  test('a decimal price becomes whole kurus', () {
    // Jane Eyre costs 269.9 in the seed data
    debugPrint('269.9 * 100 = ${269.9 * 100}');
    debugPrint('(269.9 * 100).toInt() = ${(269.9 * 100).toInt()}');
    debugPrint('moneyFromJson(269.9) = ${moneyFromJson(269.9).kurus} kurus');
  });

  test('a page of books from the API', () async {
    final requests = <Uri>[];
    final api = ApiClient(
      baseUrl: base,
      client: MockClient((request) async {
        requests.add(request.url);
        return json(booksPage1, 200);
      }),
    );
    final result = await ApiBookRepository(api, pageSize: 2).getPage(0);
    final books = (result as Ok<List<Book>>).value;
    debugPrint('GET ${requests.single}');
    debugPrint(books.map((b) => '${b.title} ${b.price.format()}').join(', '));
  });

  test('404 becomes BookNotFound', () async {
    final api = ApiClient(
      baseUrl: base,
      client: MockClient((_) async => json(notFound, 404)),
    );
    final result = await ApiBookRepository(api).getByIsbn('0000000000000');
    final error = (result as Failure).error;
    debugPrint('${error.runtimeType}: $error');
    expect(error, isA<BookNotFound>());
  });

  test('GET is retried on 503, POST is not', () async {
    var gets = 0, posts = 0;
    final api = ApiClient(
      baseUrl: base,
      retryDelay: Duration.zero,
      client: MockClient((request) async {
        if (request.method == 'GET') gets++;
        if (request.method == 'POST') posts++;
        return request.method == 'GET' && gets == 3
            ? json(dune, 200)
            : http.Response('', 503);
      }),
    );
    final get = await api.get('/api/books/9780441172719');
    final post = await api.post('/api/carts', null);
    debugPrint('GET: ${get.runtimeType} after $gets attempts');
    debugPrint('POST: ${(post as Failure).error} after $posts attempt');
  });

  test('a server that does not answer times out', () async {
    final api = ApiClient(
      baseUrl: base,
      timeout: const Duration(milliseconds: 50),
      retryDelay: Duration.zero,
      client: MockClient((_) => Completer<http.Response>().future),
    );
    final result = await api.get('/api/books');
    debugPrint('${(result as Failure).error}');
  });

  test('401 refreshes the token once for parallel requests', () async {
    final tokens = InMemoryTokenStore();
    await tokens.write((accessToken: 'old-access', refreshToken: 'refresh-1'));
    final log = <String>[];
    final api = ApiClient(
      baseUrl: base,
      tokens: tokens,
      client: MockClient((request) async {
        final auth = request.headers['Authorization'];
        log.add('${request.method} ${request.url.path} ${auth ?? ''}'.trim());
        if (request.url.path == '/api/auth/refresh') {
          await Future<void>.delayed(const Duration(milliseconds: 10));
          return json(
            jsonEncode({
              'tokenType': 'Bearer',
              'accessToken': 'new-access',
              'expiresIn': 3600,
              'refreshToken': 'refresh-2',
            }),
            200,
          );
        }
        return auth == 'Bearer new-access'
            ? json('[]', 200)
            : json(unauthorized, 401);
      }),
    );
    final results = await Future.wait([
      api.get('/api/orders', auth: true),
      api.get('/api/orders', auth: true),
    ]);
    log.forEach(debugPrint);
    debugPrint('results: ${results.map((r) => r.runtimeType).join(', ')}');
    debugPrint('stored refresh token: ${(await tokens.read())!.refreshToken}');
  });

  test('a rejected refresh token ends the session', () async {
    final tokens = InMemoryTokenStore();
    await tokens.write((accessToken: 'old-access', refreshToken: 'revoked'));
    var expired = 0;
    final api = ApiClient(
      baseUrl: base,
      tokens: tokens,
      client: MockClient((_) async => json(unauthorized, 401)),
    )..onSessionExpired = () => expired++;
    final result = await api.get('/api/orders', auth: true);
    debugPrint(
      '${(result as Failure).error}, session expired: $expired, '
      'tokens: ${await tokens.read()}',
    );
  });
}
