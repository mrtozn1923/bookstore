type EBook = { download: () => void };
type PrintedBook = { ship: () => void };

function deliver(book: EBook | PrintedBook) {
  if ("download" in book) {
    // book: EBook
    book.download();
  } else {
    // book: PrintedBook
    book.ship();
  }
}
