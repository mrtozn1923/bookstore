type EBook = { kind: "ebook"; download(): void };
type PrintedBook = { kind: "printed"; ship(): void };

function isEBook(book: EBook | PrintedBook): boolean {
  return book.kind === "ebook";
}

function deliver(book: EBook | PrintedBook) {
  if (isEBook(book)) {
    book.download();
  }
}
