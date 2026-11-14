// I09: run with `node I09/examples.js` (or paste the blocks into the browser console in order)

// --- example 1 ---
let customer = { name: "Ada" };
const recentCustomers = [customer];
const visits = new WeakMap();
visits.set(customer, 3);

customer = null;

console.log(recentCustomers[0].name);
console.log(visits.has(recentCustomers[0]));

// --- example 2 ---
function createOrder() {
  const quantity = 2;
  const item = { title: "Dune", price: 450 };
  return item;
}

const order = createOrder();
console.log(order.title); // Dune

// --- example 3 ---
let book = { title: "Dune" };
let author = { name: "Frank Herbert" };
book.author = author;
author.latestBook = book;

book = null;
author = null;
// The two objects still point to each other, but they can't be reached from the roots.
// Both are candidates for collection.

// --- example 6 ---
const state = { stockChecks: [] };

const timer = setInterval(() => {
  state.stockChecks.push(new Date().toISOString());
}, 1000);

// When this part of the page closes:
clearInterval(timer);

// --- example 7 ---
function prepareSalesReport() {
  const orderTotals = new Array(1_000_000).fill(450);
  return {
    summary: () => `Order count: ${orderTotals.length}`
  };
}

const report = prepareSalesReport();
console.log(report.summary()); // orderTotals stays in memory as long as report lives

// --- example 10 ---
const cache = new Map();

const cleanup = new FinalizationRegistry((key) => {
  const ref = cache.get(key);
  if (ref && !ref.deref()) cache.delete(key);
});

function fetchCached(key, create) {
  const existing = cache.get(key)?.deref();
  if (existing) return existing;
  const fresh = create();
  cache.set(key, new WeakRef(fresh));
  cleanup.register(fresh, key);
  return fresh;
}

const r1 = fetchCached("sales-2026-01", () => ({ rows: new Array(100_000).fill(0) }));
const r2 = fetchCached("sales-2026-01", () => ({ rows: [] }));
console.log(r1 === r2); // true
