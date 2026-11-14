// I02: run with `node I02/examples.js` (or paste the blocks into the browser console in order)

// --- example 1 ---
const config = Object.freeze({
  currency: "TRY",
  shipping: { freeOver: 500 }
});

config.currency = "EUR";
config.shipping.freeOver = 0;

console.log(config.currency, config.shipping.freeOver);

// --- example 2 ---
const book = { title: "Emma" };

console.log(Object.getOwnPropertyDescriptor(book, "title"));
// { value: "Emma", writable: true, enumerable: true, configurable: true }

// --- example 3 ---
const dune = { title: "Dune" };

Object.defineProperty(dune, "isbn", { value: "9780441172719" });

console.log(dune.isbn); // "9780441172719"
console.log(Object.keys(dune)); // ["title"]
console.log(JSON.stringify(dune)); // {"title":"Dune"}

dune.isbn = "0000000000000";
console.log(dune.isbn); // "9780441172719"

delete dune.isbn;
console.log(dune.isbn); // "9780441172719"

// --- example 4 ---
const price = { minorUnits: 45000 };

Object.defineProperty(price, "amount", {
  get() {
    return this.minorUnits / 100;
  },
  set(value) {
    if (typeof value !== "number") throw new TypeError("Expected a number");
    this.minorUnits = Math.round(value * 100);
  },
  enumerable: true
});

console.log(price.amount); // 450
price.amount = 399.9;
console.log(price.minorUnits); // 39990

// --- example 5 ---
const open = Object.preventExtensions({ x: 1 });
open.y = 2;
delete open.x;
console.log(open); // {}

const sealed = Object.seal({ x: 1 });
sealed.x = 5;
sealed.y = 2;
delete sealed.x;
console.log(sealed); // { x: 5 }

const frozen = Object.freeze({ x: 1 });
frozen.x = 5;
frozen.y = 2;
delete frozen.x;
console.log(frozen); // { x: 1 }

// --- example 6 ---
(function () {
  "use strict";
  const locked = Object.freeze({ x: 1 });
  try {
    locked.x = 2;
  } catch (error) {
    console.log(error.name); // "TypeError"
    console.log(error.message);
  }
})();

// --- example 7 ---
function deepFreeze(object) {
  Object.freeze(object);
  for (const key of Reflect.ownKeys(object)) {
    const value = object[key];
    if (typeof value === "object" && value !== null && !Object.isFrozen(value)) {
      deepFreeze(value);
    }
  }
  return object;
}

const storeSettings = deepFreeze({
  shipping: { rates: { standard: 49.9 } },
  languages: ["tr", "en"]
});

storeSettings.shipping.rates.standard = 0;
console.log(storeSettings.shipping.rates.standard); // 49.9
console.log(Object.isFrozen(storeSettings.languages)); // true

// --- example 8 ---
const state = deepFreeze({
  customer: { name: "Ada", city: "Izmir" },
  items: ["Dune", "Emma"],
  shipping: { method: "standard" }
});

const next = {
  ...state,
  customer: { ...state.customer, city: "Ankara" },
  items: [...state.items, "Middlemarch"]
};

console.log(state.customer.city, next.customer.city); // "Izmir" "Ankara"
console.log(state.customer === next.customer); // false
console.log(state.shipping === next.shipping); // true

// --- example 9 ---
const frozenStock = Object.freeze(new Map());
frozenStock.set("9780441172719", 12);
console.log(frozenStock.size); // 1

const frozenDate = Object.freeze(new Date(2026, 0, 1));
frozenDate.setFullYear(2000);
console.log(frozenDate.getFullYear()); // 2000

// --- example 10 ---
const printEdition = Object.freeze({ label() { return "Paperback"; } });
const ebookEdition = Object.create(printEdition);

ebookEdition.label = () => "E-book";
console.log(ebookEdition.label()); // "Paperback"
