/// The outcome of an operation that can fail, returned instead of throwing.
/// Named Ok/Failure so that it does not hide dart:core's Error.
sealed class Result<T> {
  const Result();

  const factory Result.ok(T value) = Ok<T>;
  const factory Result.failure(Object error) = Failure<T>;
}

final class Ok<T> extends Result<T> {
  const Ok(this.value);
  final T value;
}

final class Failure<T> extends Result<T> {
  const Failure(this.error);
  final Object error;
}
