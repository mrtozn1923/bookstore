import 'dart:async';

import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';
import 'package:provider/provider.dart';

import '../data/repositories/auth_repository.dart';
import '../data/repositories/order_repository.dart';
import '../data/services/api_client.dart';
import '../domain/models/delivery_address.dart';
import '../domain/models/order.dart';
import '../ui/auth/widgets/sign_in_screen.dart';
import '../ui/book_details/widgets/book_details_route.dart';
import '../ui/cart/view_models/cart_view_model.dart';
import '../ui/cart/widgets/cart_screen.dart';
import '../ui/checkout/widgets/delivery_address_screen.dart';
import '../ui/core/ui/scaffold_with_nav_bar.dart';
import '../ui/home/widgets/home_screen.dart';
import '../ui/orders/widgets/orders_route.dart';
import '../utils/result.dart';
import 'routes.dart';

GoRouter createRouter({
  required AuthRepository auth,
  String? initialLocation,
}) => GoRouter(
  initialLocation: initialLocation ?? Routes.catalog,
  // Sign-in and sign-out run redirect again
  refreshListenable: auth,
  redirect: (context, state) =>
      authRedirect(state.uri, signedIn: auth.isSignedIn),
  routes: [
    GoRoute(path: '/', redirect: (_, _) => Routes.catalog),
    StatefulShellRoute.indexedStack(
      builder: (context, state, navigationShell) =>
          ScaffoldWithNavBar(navigationShell: navigationShell),
      branches: [
        StatefulShellBranch(
          routes: [
            GoRoute(
              path: Routes.catalog,
              builder: (context, state) => const HomeScreen(),
              routes: [
                GoRoute(
                  path: 'books/:isbn',
                  builder: (context, state) =>
                      BookDetailsRoute(isbn: state.pathParameters['isbn']!),
                ),
              ],
            ),
          ],
        ),
        StatefulShellBranch(
          routes: [
            GoRoute(
              path: Routes.cart,
              builder: (context, state) => CartScreen(
                viewModel: context.read<CartViewModel>(),
                onCheckout: () => unawaited(_checkout(context)),
              ),
              routes: [
                GoRoute(
                  path: 'address',
                  builder: (context, state) => DeliveryAddressScreen(
                    onSubmit: (address) => _placeOrder(context, auth, address),
                  ),
                ),
              ],
            ),
          ],
        ),
        StatefulShellBranch(
          routes: [
            GoRoute(
              path: Routes.orders,
              builder: (context, state) => const OrdersRoute(),
            ),
          ],
        ),
      ],
    ),
    GoRoute(
      path: Routes.signIn,
      builder: (context, state) => SignInScreen(
        onSignIn: (email, password) async {
          final result = await auth.signIn(email: email, password: password);
          return switch (result) {
            Ok() => null,
            Failure(:final error) => '$error',
          };
        },
      ),
    ),
  ],
);

Future<void> _checkout(BuildContext context) async {
  final order = await context.push<Order>(Routes.address);
  if (order == null || !context.mounted) return;
  ScaffoldMessenger.of(context)
      .showSnackBar(SnackBar(content: Text('Order ${order.id} placed')));
}

/// Sends the cart as an order. Returns the API's ProblemDetails so that the
/// form can show them, or null after the order page has been closed.
Future<Map<String, Object?>?> _placeOrder(
  BuildContext context,
  AuthRepository auth,
  DeliveryAddress address,
) async {
  final cart = context.read<CartViewModel>();
  final result = await context.read<OrderRepository>().placeOrder(
    lines: cart.lines,
    address: address,
    email: auth.email!,
  );
  switch (result) {
    case Ok(:final value):
      await cart.clear.execute();
      if (context.mounted) context.pop(value);
      return null;
    case Failure(error: ApiException(:final problem)):
      return problem;
    case Failure(:final error):
      return {'title': '$error'};
  }
}

/// The auth guard, as a plain function so it can be unit tested.
String? authRedirect(Uri uri, {required bool signedIn}) {
  final path = uri.path;
  final needsSignIn = path.startsWith(Routes.orders) || path == Routes.address;
  if (!signedIn && needsSignIn) {
    return Uri(
      path: Routes.signIn,
      queryParameters: {'from': uri.toString()},
    ).toString();
  }
  if (signedIn && path == Routes.signIn) {
    final from = uri.queryParameters['from'];
    // Only a path inside the app: "/orders" yes, "//other.example" no
    final isLocal =
        from != null && from.startsWith('/') && !from.startsWith('//');
    return isLocal ? from : Routes.catalog;
  }
  return null;
}
