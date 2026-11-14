function printBook(book: { title: string; author: string; stock?: number }) {
  console.log(`${book.title} (${book.author})`);
}

function isInStock(book: { title: string; author: string; stock?: number }) {
  return (book.stock ?? 0) > 0;
}
