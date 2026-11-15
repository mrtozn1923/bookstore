import '../domain/models/author.dart';
import '../domain/models/book.dart';
import '../domain/models/money.dart';

/// Temporary in-app catalog until the app talks to Bookstore.Api.
const sampleBooks = [
  Book(
    isbn: '9780441172719',
    title: 'Dune',
    author: Author(name: 'Frank Herbert'),
    price: Money(48900),
    publishedYear: 1965,
  ),
  Book(
    isbn: '9780141439587',
    title: 'Emma',
    author: Author(name: 'Jane Austen'),
    price: Money(21500),
    publishedYear: 1815,
  ),
  Book(
    isbn: '9780547928227',
    title: 'The Hobbit',
    author: Author(name: 'J. R. R. Tolkien'),
    price: Money(32000),
    publishedYear: 1937,
  ),
];
