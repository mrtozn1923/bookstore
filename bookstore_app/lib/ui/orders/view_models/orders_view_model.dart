import 'dart:async';

import 'package:flutter/foundation.dart';

import '../../../data/repositories/order_repository.dart';
import '../../../domain/models/order.dart';
import '../../../utils/command.dart';
import '../../../utils/result.dart';

class OrdersViewModel extends ChangeNotifier {
  OrdersViewModel({required OrderRepository orderRepository})
    : _repository = orderRepository {
    load = Command0(_load);
    // A new order placed from the cart tab shows up here without a refresh
    _repository.addListener(_reload);
    unawaited(load.execute());
  }

  final OrderRepository _repository;
  List<Order> _orders = const [];

  late final Command0<void> load;

  List<Order> get orders => _orders;

  void _reload() => unawaited(load.execute());

  Future<Result<void>> _load() async {
    switch (await _repository.getOrders()) {
      case Ok(:final value):
        _orders = List.unmodifiable(value);
        notifyListeners();
        return const Result.ok(null);
      case Failure(:final error):
        return Result.failure(error);
    }
  }

  @override
  void dispose() {
    _repository.removeListener(_reload);
    super.dispose();
  }
}
