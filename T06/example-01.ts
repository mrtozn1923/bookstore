function printBook(book: { title: string; price: number }) {
  console.log(`${book.title}: ${book.price} TRY`);
}

const dune = { title: "Dune", price: 450, author: "Frank Herbert" };

printBook(dune);
printBook({ title: "Emma", price: 320, author: "Jane Austen" });
