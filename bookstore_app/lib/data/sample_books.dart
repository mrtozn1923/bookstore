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
    description:
        'On the desert planet Arrakis, young Paul Atreides is drawn into a '
        'struggle over the most valuable substance in the universe.',
  ),
  Book(
    isbn: '9780141439587',
    title: 'Emma',
    author: Author(name: 'Jane Austen'),
    price: Money(21500),
    publishedYear: 1815,
    format: .ebook,
    description:
        'A wealthy young woman in a quiet English village keeps arranging '
        'other people\'s marriages and keeps getting them wrong.',
  ),
  Book(
    isbn: '9780547928227',
    title: 'The Hobbit',
    subtitle: 'or There and Back Again',
    author: Author(name: 'J. R. R. Tolkien'),
    price: Money(32000),
    publishedYear: 1937,
    format: .hardcover,
    description:
        'Bilbo Baggins leaves his comfortable home to help a group of dwarves '
        'win back their mountain and its treasure from a dragon.',
  ),
];
