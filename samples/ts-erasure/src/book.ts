interface Book {
  title: string;
  price: number;
}

function isCheap(book: Book): boolean {
  return book.price < 300;
}

function withShipping(book: Book): number {
  return book.price + 25;
}

// the API sent price as a string; the cast silences the compiler
const fromApi = JSON.parse('{"title":"Dune","price":"349.90"}') as Book;

console.log(isCheap(fromApi));
console.log(withShipping(fromApi));
