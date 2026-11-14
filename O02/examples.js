// O02: run with `node O02/examples.js` (or paste the blocks into the browser console in order)

// --- example 1 ---
console.log(["1", "2", "3"].map(parseInt));

// --- example 2 ---
const books = [
  { title: "Dune", category: "science-fiction", price: 450, stock: 12 },
  { title: "Emma", category: "classics", price: 320, stock: 8 },
  { title: "The Hobbit", category: "fantasy", price: 390, stock: 5 },
  { title: "Middlemarch", category: "classics", price: 380, stock: 3 },
  { title: "Frankenstein", category: "science-fiction", price: 280, stock: 0 }
];

const titles = books.map((b) => b.title);
console.log(titles); // ["Dune", "Emma", "The Hobbit", "Middlemarch", "Frankenstein"]

const discounted = books.map((b) => ({ ...b, price: b.price * 0.9 }));
console.log(discounted[0].price, books[0].price); // 405 450

// --- example 3 ---
const inStock = books.filter((b) => b.stock > 0);
console.log(inStock.length); // 4

const expensive = books.find((b) => b.price > 350);
console.log(expensive.title); // "Dune"

console.log(books.findIndex((b) => b.price > 350));      // 0
console.log(books.findLast((b) => b.price > 350).title); // "Middlemarch"
console.log(books.findLastIndex((b) => b.price > 350));  // 3
console.log(books.find((b) => b.price > 5000));          // undefined

// --- example 4 ---
console.log(books.some((b) => b.stock === 0)); // true
console.log(books.every((b) => b.price > 10)); // true

// --- example 5 ---
const totalValue = books.reduce((sum, b) => sum + b.price * b.stock, 0);
console.log(totalValue); // 11050

// step by step:
// sum = 0     + 450 * 12 = 5400
// sum = 5400  + 320 * 8  = 7960
// sum = 7960  + 390 * 5  = 9910
// sum = 9910  + 380 * 3  = 11050
// sum = 11050 + 280 * 0  = 11050

// --- example 6 ---
const statuses = ["Paid", "Shipped", "Paid", "Paid"];
const counts = statuses.reduce((acc, status) => {
  acc[status] = (acc[status] ?? 0) + 1;
  return acc;
}, {});
console.log(counts); // { Paid: 3, Shipped: 1 }

// --- example 7 ---
const nested = [1, [2, 3], [4, [5, 6]]];
console.log(nested.flat());         // [1, 2, 3, 4, [5, 6]]
console.log(nested.flat(Infinity)); // [1, 2, 3, 4, 5, 6]

const orders = [
  { id: 1041, items: ["Dune", "Emma"] },
  { id: 1042, items: ["The Hobbit"] }
];
console.log(orders.flatMap((o) => o.items)); // ["Dune", "Emma", "The Hobbit"]

// --- example 9 ---
const report = books
  .filter((b) => b.stock > 0)
  .map((b) => `${b.title}: ${b.price} TRY`)
  .join(", ");

console.log(report);
// "Dune: 450 TRY, Emma: 320 TRY, The Hobbit: 390 TRY, Middlemarch: 380 TRY"

// --- example 10 ---
const prices = [390, 280, 450, 320];

console.log(prices.toSorted((a, b) => a - b)); // [280, 320, 390, 450]
console.log(prices.toReversed());              // [320, 450, 280, 390]
console.log(prices.toSpliced(1, 2, 300));      // [390, 300, 320]
console.log(prices.with(0, 400));              // [400, 280, 450, 320]
console.log(prices.with(-1, 350));             // [390, 280, 450, 350]

console.log(prices); // [390, 280, 450, 320], untouched

// --- example 11 ---
const cheapestFirst = books
  .toSorted((a, b) => a.price - b.price)
  .map((b) => b.title);
console.log(cheapestFirst); // ["Frankenstein", "Emma", "Middlemarch", "The Hobbit", "Dune"]

// --- example 12 ---
const byCategory = Object.groupBy(books, (b) => b.category);
console.log(Object.keys(byCategory)); // ["science-fiction", "classics", "fantasy"]
console.log(byCategory.classics.map((b) => b.title)); // ["Emma", "Middlemarch"]

const byStock = Object.groupBy(books, (b) => (b.stock > 0 ? "available" : "soldOut"));
console.log(byStock.soldOut[0].title); // "Frankenstein"

// --- example 13 ---
const byPrice = Map.groupBy(books, (b) => b.price >= 380);
console.log(byPrice.get(true).map((b) => b.title)); // ["Dune", "The Hobbit", "Middlemarch"]
