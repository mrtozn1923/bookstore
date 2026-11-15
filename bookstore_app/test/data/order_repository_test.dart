import 'package:bookstore_app/data/repositories/order_repository_api.dart';
import 'package:bookstore_app/data/sample_books.dart';
import 'package:bookstore_app/data/services/api_client.dart';
import 'package:bookstore_app/data/validation_problem.dart';
import 'package:bookstore_app/domain/models/delivery_address.dart';
import 'package:bookstore_app/domain/models/order.dart';
import 'package:bookstore_app/utils/result.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';

import '../testing/api_fixtures.dart';

const address = DeliveryAddress(
  fullName: 'Ada Lovelace',
  addressLine: '1 Example Street',
  city: 'Istanbul',
  postalCode: '34710',
  phone: '0212 555 01 01',
);

ApiOrderRepository repository(
  String orderBody,
  int orderStatus,
  List<String> log,
) => ApiOrderRepository(
  ApiClient(
    baseUrl: Uri.parse('http://127.0.0.1:52813'),
    client: MockClient((request) async {
      log.add('${request.method} ${request.url.path} ${request.body}');
      final (body, status) = switch (request.url.path) {
        '/api/carts' => (newCart, 201),
        '/api/orders' => (orderBody, orderStatus),
        _ => ('{}', 200),
      };
      return http.Response(body, status);
    }),
  ),
);

void main() {
  final lines = [(book: sampleBooks.first, quantity: 2)];

  test('checkout creates a cart, fills it and orders it', () async {
    final log = <String>[];
    final result = await repository(
      placedOrder,
      201,
      log,
    ).placeOrder(lines: lines, address: address, email: 'ada@example.com');
    log.forEach(debugPrint);
    final order = (result as Ok<Order>).value;
    debugPrint('order ${order.id}, total ${order.total.format()}');
  });

  test('CustomerName errors land on the full name field', () async {
    final result = await repository(
      customerNameTooLong,
      400,
      [],
    ).placeOrder(lines: lines, address: address, email: 'ada@example.com');
    final error = (result as Failure).error as ApiException;
    debugPrint(
      '${error.statusCode}: '
      '${ValidationProblem.fromJson(error.problem).fieldErrors(prefix: 'ShippingAddress.')}',
    );
  });

  test('409 out of stock is a problem without field errors', () async {
    final result = await repository(
      outOfStock,
      409,
      [],
    ).placeOrder(lines: lines, address: address, email: 'ada@example.com');
    debugPrint('${(result as Failure).error}');
  });
}
