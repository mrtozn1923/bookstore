function totalPrice(books) {
  let total = 0;
  for (const book of books) total += book.price;
  return total;
}

const books = [
  { title: "Dune", price: 349.9 },
  { title: "Emma", price: 199 },
  { title: "The Hobbit", price: 289.5 },
];

let sum = 0;
for (let i = 0; i < 100_000; i++) sum += totalPrice(books);

// a book from another source: same fields, different order -> another hidden class
books.push({ price: 310, title: "Middlemarch" });
sum += totalPrice(books);
console.log(`sum: ${sum.toFixed(1)}`);
