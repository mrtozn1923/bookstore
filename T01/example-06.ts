const book = { title: "Dune", price: 450 };

// Error: Property 'tittle' does not exist on type '{ title: string; price: number; }'. Did you mean 'title'?
console.log(book.tittle);

// Error: Property 'toUpperCase' does not exist on type 'number'.
book.price.toUpperCase();
