// T02: run with `node T02/examples.js` (or paste the blocks into the browser console in order)

// --- example 6 ---
console.log("Plain info", 42, true);
console.warn("Heads up: only 3 copies of Middlemarch left");
console.error("Payment failed for order 1042");

// --- example 7 ---
const books = [
  { title: "Dune", price: 450 },
  { title: "Emma", price: 320 },
  { title: "The Hobbit", price: 390 }
];
console.table(books);
