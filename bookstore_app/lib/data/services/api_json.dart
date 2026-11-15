import '../../domain/models/author.dart';
import '../../domain/models/book.dart';
import '../../domain/models/money.dart';
import '../../domain/models/order.dart';
import '../../domain/models/order_status.dart';
import '../../utils/result.dart';

/// Prices arrive as JSON numbers (C# decimal, e.g. 269.9). This is the only
/// place where a double touches money: it is rounded to whole kurus at once.
Money moneyFromJson(num value) => Money((value * 100).round());

/// GET /api/books items and GET /api/books/{isbn}.
Book bookFromJson(Object? json) => switch (json) {
  final Map<String, Object?> map &&
      {
        'isbn': String isbn,
        'title': String title,
        'author': String author,
        'price': num price,
        'publishedYear': int year,
      } =>
    Book(
      isbn: isbn,
      title: title,
      author: Author(name: author),
      price: moneyFromJson(price),
      publishedYear: year,
      // Only the details endpoint sends a description
      description: map['description'] as String?,
    ),
  _ => throw FormatException('Not a book: $json'),
};

/// GET /api/orders items and the body of POST /api/orders.
Order orderFromJson(Object? json) => switch (json) {
  {
    'id': int id,
    'status': String status,
    'placedAtUtc': String placedAt,
    'total': num total,
  } =>
    Order(
      id: '$id',
      placedAt: DateTime.parse(placedAt),
      total: moneyFromJson(total),
      // The API has no shipping data yet: everything not cancelled is pending
      status: status == 'Cancelled' ? const Cancelled() : const Pending(),
    ),
  _ => throw FormatException('Not an order: $json'),
};

/// A JSON shape we did not expect is a Failure too, not a crash.
Result<T> parseJson<T>(T Function() parse) {
  try {
    return Result.ok(parse());
  } on FormatException catch (e) {
    return Result.failure(e);
  }
}
