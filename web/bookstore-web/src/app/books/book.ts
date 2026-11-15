export type BookFormat = 'hardcover' | 'paperback' | 'ebook';

export interface Book {
  slug: string;
  title: string;
  author: string;
  genre: string;
  format: BookFormat;
  language: 'en' | 'tr';
  year: number;
  // kapak dosyasının kökü: kitap + yazar, dilden bağımsız (ör. dune-frank-herbert)
  coverStem: string;
}

// sunucudaki BookCatalog ile aynı kayıtlardan küçük bir örnek
export const BOOKS: Book[] = [
  { slug: 'dune', title: 'Dune', author: 'Frank Herbert', genre: 'science-fiction', format: 'hardcover', language: 'en', year: 1965, coverStem: 'dune-frank-herbert' },
  { slug: 'foundation', title: 'Foundation', author: 'Isaac Asimov', genre: 'science-fiction', format: 'ebook', language: 'en', year: 1951, coverStem: 'foundation-isaac-asimov' },
  { slug: 'neuromancer', title: 'Neuromancer', author: 'William Gibson', genre: 'science-fiction', format: 'hardcover', language: 'en', year: 1984, coverStem: 'neuromancer-william-gibson' },
  { slug: 'the-hobbit', title: 'The Hobbit', author: 'J. R. R. Tolkien', genre: 'fantasy', format: 'hardcover', language: 'en', year: 1937, coverStem: 'the-hobbit-j-r-r-tolkien' },
];

const FORMAT_TR: Record<BookFormat, string> = { hardcover: 'ciltli baskı', paperback: 'karton kapaklı baskı', ebook: 'e-kitap' };
const FORMAT_EN: Record<BookFormat, string> = { hardcover: 'hardcover edition', paperback: 'paperback edition', ebook: 'ebook edition' };

// alt metni katalog verisinden: sunucudaki CoverImages.Alt ile aynı kural
export function coverAlt(book: Book, locale: string): string {
  return locale.startsWith('tr')
    ? `${book.title}, ${book.author}, ${FORMAT_TR[book.format]} kapağı`
    : `Cover of ${book.title} by ${book.author}, ${FORMAT_EN[book.format]}`;
}
