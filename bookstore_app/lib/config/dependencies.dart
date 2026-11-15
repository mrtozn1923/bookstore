import 'package:provider/provider.dart';
import 'package:provider/single_child_widget.dart';

import '../data/repositories/auth_repository.dart';
import '../data/repositories/auth_repository_api.dart';
import '../data/repositories/book_repository.dart';
import '../data/repositories/book_repository_api.dart';
import '../data/repositories/book_repository_sample.dart';
import '../data/repositories/cart_repository.dart';
import '../data/repositories/order_repository.dart';
import '../data/repositories/order_repository_api.dart';
import '../data/sample_catalog.dart';
import '../data/services/api_client.dart';
import '../ui/cart/view_models/cart_view_model.dart';

/// The app's object graph. Tests and widget previews pass their own
/// repositories; whatever they leave out uses sample data.
List<SingleChildWidget> appProviders({
  BookRepository? bookRepository,
  CartRepository? cartRepository,
  AuthRepository? authRepository,
  OrderRepository? orderRepository,
}) => [
  ChangeNotifierProvider<AuthRepository>(
    create: (_) => authRepository ?? SampleAuthRepository(),
  ),
  Provider<BookRepository>(
    create: (_) => bookRepository ?? SampleBookRepository(SampleCatalog()),
  ),
  Provider<CartRepository>(
    create: (_) => cartRepository ?? InMemoryCartRepository(),
  ),
  ChangeNotifierProvider<OrderRepository>(
    create: (_) => orderRepository ?? SampleOrderRepository(),
  ),
  ChangeNotifierProvider(
    create: (context) => CartViewModel(cartRepository: context.read()),
  ),
];

/// What main() runs: every repository talks to Bookstore.Api.
List<SingleChildWidget> apiProviders(ApiClient api) => appProviders(
  bookRepository: ApiBookRepository(api),
  authRepository: ApiAuthRepository(api),
  orderRepository: ApiOrderRepository(api),
);
