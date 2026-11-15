import 'package:provider/provider.dart';
import 'package:provider/single_child_widget.dart';

import '../data/repositories/auth_repository.dart';
import '../data/repositories/book_repository.dart';
import '../data/repositories/book_repository_api.dart';
import '../data/repositories/book_repository_cached.dart';
import '../data/repositories/book_repository_sample.dart';
import '../data/repositories/cart_repository.dart';
import '../data/repositories/cart_repository_drift.dart';
import '../data/repositories/order_repository.dart';
import '../data/repositories/order_repository_api.dart';
import '../data/sample_catalog.dart';
import '../data/services/api_client.dart';
import '../data/services/app_database.dart';
import '../ui/cart/view_models/cart_view_model.dart';

/// The app's object graph. Tests and widget previews pass their own
/// repositories; whatever they leave out uses sample data.
List<SingleChildWidget> appProviders({
  BookRepository? bookRepository,
  CartRepository? cartRepository,
  AuthRepository? authRepository,
  OrderRepository? orderRepository,
  OfflineStatus? offlineStatus,
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
  ChangeNotifierProvider<OfflineStatus>(
    create: (_) => offlineStatus ?? OfflineStatus(),
  ),
  ChangeNotifierProvider(
    create: (context) => CartViewModel(cartRepository: context.read()),
  ),
];

/// What main() runs: the repositories talk to Bookstore.Api and, when a
/// database is given, keep the catalog and the cart on the device.
List<SingleChildWidget> apiProviders({
  required ApiClient api,
  required AuthRepository auth,
  AppDatabase? db,
}) {
  final status = OfflineStatus();
  final books = ApiBookRepository(api);
  return appProviders(
    authRepository: auth,
    bookRepository: db == null
        ? books
        : CachedBookRepository(remote: books, db: db, status: status),
    cartRepository: db == null
        ? InMemoryCartRepository()
        : DriftCartRepository(db),
    orderRepository: ApiOrderRepository(api),
    offlineStatus: status,
  );
}
