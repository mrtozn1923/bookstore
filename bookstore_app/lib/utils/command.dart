import 'package:flutter/foundation.dart';

import 'result.dart';

/// Wraps one user action of a ViewModel: whether it is running and how it
/// ended. The View listens to the command, not to the whole ViewModel.
abstract class Command<T> extends ChangeNotifier {
  bool _running = false;
  Result<T>? _result;

  bool get running => _running;
  Result<T>? get result => _result;
  bool get completed => _result is Ok<T>;
  bool get failed => _result is Failure<T>;

  /// Call after the View has shown the error, so it is not shown twice.
  void clearResult() {
    _result = null;
    notifyListeners();
  }

  Future<void> _run(Future<Result<T>> Function() action) async {
    if (_running) return; // a second tap while running is ignored
    _running = true;
    _result = null;
    notifyListeners();
    try {
      _result = await action();
    } finally {
      _running = false;
      notifyListeners();
    }
  }
}

final class Command0<T> extends Command<T> {
  Command0(this._action);
  final Future<Result<T>> Function() _action;

  Future<void> execute() => _run(_action);
}

final class Command1<T, A> extends Command<T> {
  Command1(this._action);
  final Future<Result<T>> Function(A) _action;

  Future<void> execute(A argument) => _run(() => _action(argument));
}
