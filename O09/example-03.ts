type EBook = { kind: "ebook"; download(): void };
type PrintedBook = { kind: "printed"; ship(): void };

function isEBook(book: EBook | PrintedBook): book is EBook {
  return book.kind === "ebook";
}

function deliver(book: EBook | PrintedBook) {
  if (isEBook(book)) {
    book.download(); // book: EBook
  } else {
    book.ship(); // book: PrintedBook
  }
}

deliver({ kind: "printed", ship: () => console.log("Shipping") }); // Shipping
