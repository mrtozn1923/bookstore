import 'package:bookstore_app/ui/core/validators.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  test('email', () {
    expect(Validators.email(''), 'This field is required');
    expect(Validators.email('ada@'), 'Enter a valid email address');
    expect(Validators.email(' ada@example.com '), isNull);
  });

  test('postal code and phone', () {
    expect(Validators.postalCode('3471'), 'Postal code has 5 digits');
    expect(Validators.postalCode('34710'), isNull);
    expect(Validators.phone('0212 555 01 01'), isNull);
    expect(Validators.phone('555 01'), 'Enter a 10 or 11 digit phone number');
  });
}
