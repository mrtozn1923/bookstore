type Book = { title: string; price: number };

const first = { title: "Dune" } as Book;
console.log(first.price.toFixed(2)); // TypeError: Cannot read properties of undefined (reading 'toFixed')
