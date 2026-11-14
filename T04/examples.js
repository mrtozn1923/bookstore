// T04: run with `node T04/examples.js` (or paste the blocks into the browser console in order)

// --- example 1 ---
console.log(typeof null);
console.log(typeof []);
console.log(typeof function () {});

// --- example 2 ---
let price = 450;
console.log(typeof price);
price = "450 TRY";
console.log(typeof price);

// --- example 3 ---
const title = "Dune";
const author = 'Frank Herbert';
const stock = 12;
const rating = 4.5;
const inStock = true;
console.log(typeof title, typeof stock, typeof rating, typeof inStock);

// --- example 4 ---
let giftNote;
console.log(giftNote);
let selectedBook = null;
console.log(selectedBook);

// --- example 5 ---
console.log(Number.MAX_SAFE_INTEGER);
console.log(9007199254740991 + 2);
console.log(9007199254740991n + 2n);

// --- example 6 ---
const id1 = Symbol("id");
const id2 = Symbol("id");
console.log(id1 === id2);
console.log(typeof id1);

// --- example 7 ---
const customer = { name: "Ada", city: "Izmir" };
const titles = ["Dune", "Emma"];
const orderDate = new Date();
console.log(typeof customer, typeof titles, typeof orderDate);

// --- example 8 ---
let bookTitle = "emma";
bookTitle[0] = "E";
console.log(bookTitle);
console.log(bookTitle.toUpperCase());
console.log(bookTitle);

// --- example 9 ---
let dunePrice = 450;
let salePrice = dunePrice;
salePrice = 405;
console.log(dunePrice, salePrice);

const cart1 = { total: 450 };
const cart2 = cart1;
cart2.total = 770;
console.log(cart1.total);

// --- example 10 ---
console.log(typeof typeof 42);
console.log(typeof NaN);
console.log(typeof undefined);
console.log(typeof 10n);
console.log(Array.isArray([]));

// --- example 11 ---
let firstName = "Grace";
let nickname = firstName;
nickname = "Gracie";

const profile = { name: "Grace" };
const backup = profile;
backup.name = "Gracie";

console.log(firstName, nickname);
console.log(profile.name, backup.name);
