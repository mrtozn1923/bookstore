import 'package:flutter_test/flutter_test.dart';
import 'package:integration_test/integration_test.dart';

import '../test/flows/checkout_flow.dart';

void main() {
  // Runs the app on a real device or emulator instead of the test VM
  IntegrationTestWidgetsFlutterBinding.ensureInitialized();

  testWidgets('checkout from sign-in to the order list', checkoutFlow);
}
