type Book = { title: string; price: number };

function printBook(book: Book) {
  console.log(`${book.title}: ${book.price} TRY`);
}

const dune = { title: "Dune", price: 450, stock: 12 };
printBook(dune);

printBook({ title: "Dune", price: 450, stock: 12 });
