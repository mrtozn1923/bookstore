import 'package:provider/provider.dart';
import 'package:provider/single_child_widget.dart';

import '../data/repositories/auth_repository.dart';
import '../data/repositories/book_repository.dart';
import '../data/repositories/book_repository_sample.dart';
import '../data/repositories/cart_repository.dart';
import '../data/sample_catalog.dart';
import '../ui/cart/view_models/cart_view_model.dart';

/// The app's object graph. Tests pass their own repositories.
List<SingleChildWidget> appProviders({
  BookRepository? bookRepository,
  CartRepository? cartRepository,
  AuthRepository? authRepository,
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
  ChangeNotifierProvider(
    create: (context) => CartViewModel(cartRepository: context.read()),
  ),
];
