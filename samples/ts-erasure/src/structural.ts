interface Book {
  isbn: string;
  title: string;
  price: number;
}

// what the API returns: never declared as a Book, but it has the same shape
type BookDto = { isbn: string; title: string; price: number; currency: string };

function withShipping(book: Book): number {
  return book.price + 25;
}

const fromApi: BookDto = { isbn: "9780441172719", title: "Dune", price: 349.9, currency: "TRY" };
console.log(withShipping(fromApi));
