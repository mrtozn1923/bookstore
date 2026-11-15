function isCheap(book) {
  return book.price < 300;
}

const books = [
  { title: "Dune", price: 349.9 },
  { title: "Emma", price: 199 },
  { title: "The Hobbit", price: 289.5 },
];

let matches = 0;
for (let i = 0; i < 200_000; i++) {
  if (isCheap(books[i % books.length])) matches++;
}
console.log(`isCheap calls: 200000, matches: ${matches}`);
