// Runs against a real Bookstore.Api; skipped when API_BASE_URL is not set:
// API_BASE_URL=http://127.0.0.1:52813 TEST_EMAIL=... TEST_PASSWORD=... \
//   flutter test test/live
@Tags(['live'])
library;

import 'dart:io';

import 'package:bookstore_app/data/repositories/auth_repository_api.dart';
import 'package:bookstore_app/data/repositories/book_repository_api.dart';
import 'package:bookstore_app/data/repositories/order_repository_api.dart';
import 'package:bookstore_app/data/services/api_client.dart';
import 'package:bookstore_app/domain/models/book.dart';
import 'package:bookstore_app/domain/models/delivery_address.dart';
import 'package:bookstore_app/domain/models/order.dart';
import 'package:bookstore_app/utils/result.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;

/// Prints every request with its status code (never headers or bodies).
class LoggingClient extends http.BaseClient {
  final _inner = http.Client();

  @override
  Future<http.StreamedResponse> send(http.BaseRequest request) async {
    final response = await _inner.send(request);
    debugPrint(
      '  ${request.method} ${request.url.path} -> ${response.statusCode}',
    );
    return response;
  }
}

void main() {
  final env = Platform.environment;
  final baseUrl = env['API_BASE_URL'];

  test('the app repositories against Bookstore.Api', () async {
    final api = ApiClient(
      baseUrl: Uri.parse(baseUrl!),
      client: LoggingClient(),
    );
    final books = ApiBookRepository(api);
    final auth = ApiAuthRepository(api);
    final orders = ApiOrderRepository(api);

    final page = (await books.getPage(0) as Ok<List<Book>>).value;
    debugPrint(
      'page 0: ${page.length} books, first: '
      '${page.take(3).map((b) => '${b.title} ${b.price.format()}').join(', ')}',
    );

    final wrong = await auth.signIn(
      email: env['TEST_EMAIL']!,
      password: 'wrong',
    );
    debugPrint('wrong password: ${(wrong as Failure).error}');
    await auth.signIn(
      email: env['TEST_EMAIL']!,
      password: env['TEST_PASSWORD']!,
    );
    debugPrint('signed in: ${auth.isSignedIn}');

    final dune = (await books.getByIsbn('9780441172719') as Ok<Book>).value;
    final placed = await orders.placeOrder(
      lines: [(book: dune, quantity: 1)],
      address: const DeliveryAddress(
        fullName: 'Ada Lovelace',
        addressLine: '1 Example Street',
        city: 'Istanbul',
        postalCode: '34710',
        phone: '0212 555 01 01',
      ),
      email: env['TEST_EMAIL']!,
    );
    final order = (placed as Ok<Order>).value;
    debugPrint('order ${order.id}: ${order.total.format()}');

    debugPrint('waiting for the access token to expire...');
    await Future<void>.delayed(const Duration(seconds: 8));
    final mine = (await orders.getOrders() as Ok<List<Order>>).value;
    debugPrint('my orders: ${mine.map((o) => o.id).join(', ')}');
  }, skip: baseUrl == null ? 'API_BASE_URL is not set' : false);
}
