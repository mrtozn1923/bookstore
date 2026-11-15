import '../domain/models/author.dart';
import '../domain/models/book.dart';
import '../domain/models/money.dart';
import 'sample_books.dart';

/// Temporary paged catalog until the app talks to Bookstore.Api.
/// The first books are the real samples; the rest are numbered placeholders
/// so that the list is long enough to scroll and page.
class SampleCatalog {
  SampleCatalog({
    this.total = 60,
    this.pageSize = 20,
    this.latency = const Duration(milliseconds: 300),
  });

  final int total;
  final int pageSize;
  final Duration latency;

  Future<List<Book>> page(int index) async {
    await Future<void>.delayed(latency);
    final start = index * pageSize;
    if (start >= total) return const [];
    final end = start + pageSize > total ? total : start + pageSize;
    return [for (var i = start; i < end; i++) _bookAt(i)];
  }

  Future<Book?> byIsbn(String isbn) async {
    await Future<void>.delayed(latency);
    for (var i = 0; i < total; i++) {
      final book = _bookAt(i);
      if (book.isbn == isbn) return book;
    }
    return null;
  }

  Book _bookAt(int i) => i < sampleBooks.length
      ? sampleBooks[i]
      : Book(
          isbn: 'sample-${i + 1}',
          title: 'Sample Book ${i + 1}',
          author: const Author(name: 'Sample Author'),
          price: Money(10000 + i * 250),
          publishedYear: 2000 + i % 25,
        );
}
