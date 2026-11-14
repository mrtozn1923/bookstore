// O01: run with `node O01/examples.js` (or paste the blocks into the browser console in order)

// --- example 1 ---
const { a = 1, b = 2 } = { a: undefined, b: null };
console.log(a);
console.log(b);

// --- example 2 ---
const titles = ["Dune", "Emma", "The Hobbit"];

// The old way
const firstTitle = titles[0];
const secondTitle = titles[1];

// Destructuring
const [dune, emma, hobbit] = titles;
console.log(dune, emma, hobbit); // Dune Emma The Hobbit

// --- example 3 ---
const [, , third] = ["a", "b", "c"];
console.log(third); // "c"

let x = 1;
let y = 2;
[x, y] = [y, x];
console.log(x, y); // 2 1

// --- example 4 ---
const customer = { firstName: "Ada", city: "Izmir", orders: 3 };

const { firstName, city } = customer;
console.log(firstName, city); // Ada Izmir

// --- example 5 ---
const { city: town, country = "TR", orders: orderCount = 0 } = customer;
console.log(town, country, orderCount); // Izmir TR 3

// --- example 6 ---
function createButton({ label, color = "gray", size = "md" } = {}) {
  return `${label} (${color}, ${size})`;
}

console.log(createButton({ label: "Add to cart", color: "green" })); // Add to cart (green, md)
console.log(createButton()); // undefined (gray, md)

// --- example 7 ---
// Spread: unpack and scatter
const base = [1, 2, 3];
const more = [0, ...base, 4];           // [0, 1, 2, 3, 4]
console.log(Math.max(...base));         // 3

const defaults = { currency: "TRY", lang: "en" };
const prefs = { ...defaults, currency: "EUR" };
console.log(prefs);                     // { currency: "EUR", lang: "en" }

// Rest: gather
const [head, ...tail] = [10, 20, 30];   // head: 10, tail: [20, 30]
const { currency, ...others } = prefs;  // currency: "EUR", others: { lang: "en" }

function sum(...prices) {
  let total = 0;
  for (const price of prices) total += price;
  return total;
}
console.log(sum(450, 320, 390));        // 1160

// --- example 8 ---
const title = "Dune";
const price = 450;
const field = "stock";

const book = {
  title,                 // title: title
  price,                 // price: price
  [field]: 12,           // stock: 12
  [`${field}Alert`]: 5,  // stockAlert: 5
  describe() {           // describe: function () { ... }
    return `${this.title} - ${this.price} TRY`;
  }
};

console.log(book.stock, book.stockAlert); // 12 5
console.log(book.describe());              // Dune - 450 TRY

// --- example 9 ---
const order = {
  id: 1042,
  customer: { name: "Grace", address: null },
  items: [{ title: "Dune" }]
};

console.log(order.customer.address?.city);  // undefined
console.log(order.items?.[0]?.title);       // "Dune"
console.log(order.coupon?.code);            // undefined
console.log(order.customer.notify?.());     // undefined, no such function

// --- example 10 ---
const shipCity = order.customer.address?.city ?? "No address";
console.log(shipCity); // "No address"

// --- example 11 ---
const cartLine = { quantity: 0, giftNote: "", discount: undefined, coupon: "spring10" };

cartLine.quantity ||= 1;         // 0 is falsy, becomes 1
cartLine.giftNote ??= "No note"; // "" is not null/undefined, stays as is
cartLine.discount ??= 0;         // undefined, becomes 0
cartLine.coupon &&= cartLine.coupon.toUpperCase(); // transform only if present: "SPRING10"

console.log(cartLine);
// { quantity: 1, giftNote: "", discount: 0, coupon: "SPRING10" }

// --- example 12 ---
let width, height;
({ width, height } = { width: 800, height: 600 });
console.log(width, height); // 800 600
