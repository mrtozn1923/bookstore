export interface Book {
  title: string;
  price: number;
}

export function formatBook(book: Book): string {
  return `${book.title}: ${book.price} TRY`;
}
