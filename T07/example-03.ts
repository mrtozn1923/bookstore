type Book = {
  title: string;
  author: string;
  stock?: number;
};

function printBook(book: Book) {
  console.log(`${book.title} (${book.author})`);
}

function isInStock(book: Book) {
  return (book.stock ?? 0) > 0;
}
