// I01: run with `node I01/examples.js` (or paste the blocks into the browser console in order)

// --- example 1 ---
const catalogItem = { currency: "TRY" };
const dune = Object.create(catalogItem);
dune.title = "Dune";

console.log(dune.currency);
console.log(Object.keys(dune));
console.log("currency" in dune);

// --- example 2 ---
const book = { title: "Emma" };

console.log(book.toString()); // "[object Object]"
console.log(Object.getPrototypeOf(book) === Object.prototype); // true
console.log(Object.getPrototypeOf(Object.prototype)); // null

// --- example 3 ---
const pricing = { currencyCode() { return "TRY"; } };
const order = {};

Object.setPrototypeOf(order, pricing);
console.log(Object.getPrototypeOf(order) === pricing); // true
console.log(order.currencyCode()); // "TRY"

const stockByIsbn = Object.create(null);
console.log(stockByIsbn.__proto__); // undefined
console.log(Object.getPrototypeOf(stockByIsbn)); // null

// --- example 4 ---
const paperback = {
  format: "paperback",
  describe() {
    return `${this.title} (${this.format})`;
  }
};
const hobbit = Object.create(paperback);
hobbit.title = "The Hobbit";

console.log(hobbit.describe()); // "The Hobbit (paperback)"

hobbit.format = "ebook";
console.log(hobbit.format, paperback.format); // "ebook" "paperback"
console.log(Object.hasOwn(hobbit, "format")); // true

// --- example 5 ---
function Customer(name) {
  this.name = name;
}

Customer.prototype.greet = function () {
  return `Welcome back, ${this.name}`;
};

const linus = new Customer("Linus");
const grace = new Customer("Grace");

console.log(linus.greet()); // "Welcome back, Linus"
console.log(linus.greet === grace.greet); // true
console.log(Object.getPrototypeOf(linus) === Customer.prototype); // true

// --- example 6 ---
const listing = {
  describe() {
    return `${this.title}, ${this.price} TRY`;
  }
};

const middlemarch = Object.create(listing);
middlemarch.title = "Middlemarch";
middlemarch.price = 380;

const emma = Object.create(listing, {
  title: { value: "Emma", enumerable: true },
  price: { value: 320, enumerable: true }
});

console.log(middlemarch.describe()); // "Middlemarch, 380 TRY"
console.log(emma.describe()); // "Emma, 320 TRY"

// --- example 7 ---
class Book {
  constructor(title) {
    this.title = title;
  }
  info() {
    return `Book: ${this.title}`;
  }
}

class EBook extends Book {
  fileFormat() {
    return "EPUB";
  }
}

const ebook = new EBook("Dune");

console.log(typeof Book); // "function"
console.log(Object.getPrototypeOf(ebook) === EBook.prototype); // true
console.log(Object.getPrototypeOf(EBook.prototype) === Book.prototype); // true
console.log(Object.hasOwn(Book.prototype, "info")); // true
console.log(ebook instanceof Book); // true

// --- example 8 ---
Array.prototype.last = function () {
  return this[this.length - 1];
};

console.log(["Dune", "Emma", "Middlemarch"].last()); // "Middlemarch"

// --- example 9 ---
function Cart() {}
Cart.prototype.items = [];

const adaCart = new Cart();
const alanCart = new Cart();
adaCart.items.push("Dune");

console.log(alanCart.items); // ["Dune"]
