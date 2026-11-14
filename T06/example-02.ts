function printBook(book: { title: string; price: number }) {
  console.log(`${book.title}: ${book.price} TRY`);
}

// Error: Property 'price' is missing in type '{ title: string; }' but required in type '{ title: string; price: number; }'.
printBook({ title: "Middlemarch" });
