import 'dart:convert';

import 'package:bookstore_app/data/validation_problem.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter_test/flutter_test.dart';

// The shape ASP.NET Core returns for a failed [ApiController] validation
const body = '''
{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1",
 "title":"One or more validation errors occurred.","status":400,
 "errors":{"ShippingAddress.PostalCode":["We do not deliver to this postal code."],
           "ShippingAddress.City":["The field City must be at most 40 characters.",
                                   "City is not in the list."],
           "Items[0].Quantity":["Only 2 copies are in stock."]}}
''';

void main() {
  test('maps errors to fields by prefix', () {
    final problem = ValidationProblem.fromJson(
      jsonDecode(body) as Map<String, Object?>,
    );
    final fields = problem.fieldErrors(prefix: 'ShippingAddress.');
    for (final MapEntry(:key, :value) in fields.entries) {
      debugPrint('$key -> $value');
    }
    expect(fields.keys, ['PostalCode', 'City']);
    expect(problem.fieldErrors().length, 3);
  });

  test('a body without errors', () {
    final problem = ValidationProblem.fromJson({'title': 'Conflict'});
    expect(problem.errors, isEmpty);
    expect(problem.title, 'Conflict');
  });
}
