type EBook = { kind: "ebook"; download(): void };
type PrintedBook = { kind: "printed"; ship(): void };

function isEBook(book: EBook | PrintedBook) {
  return book.kind === "ebook";
}

function deliver(book: EBook | PrintedBook) {
  if (isEBook(book)) {
    book.download();
  }
}

function isNonEmptyString(value: unknown) {
  return typeof value === "string" && value.length > 0;
}

function shout(value: string | number) {
  if (isNonEmptyString(value)) {
    // Error: Property 'toUpperCase' does not exist on type 'string | number'.
    console.log(value.toUpperCase());
  }
}
