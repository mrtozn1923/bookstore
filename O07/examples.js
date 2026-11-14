// O07: run with `node O07/examples.js` (or paste the blocks into the browser console in order)

// --- example 1 ---
class Counter {
  #count = 0;

  increment() {
    this.#count++;
    return this;
  }

  get value() {
    return this.#count;
  }
}

const c = new Counter();
c.increment().increment();
console.log(c.value, c.count);

// --- example 2 ---
class Customer {
  constructor(name, email) {
    this.name = name;
    this.email = email;
  }

  greet() {
    return `Hi, I'm ${this.name}`;
  }
}

const ada = new Customer("Ada", "ada@example.com");
const grace = new Customer("Grace", "grace@example.com");

console.log(ada.greet()); // "Hi, I'm Ada"
console.log(grace.name);  // "Grace"

// --- example 3 ---
class CartItem {
  giftWrap = false;
  addedAt = new Date();

  constructor(title) {
    this.title = title;
  }

  toggleGiftWrap() {
    this.giftWrap = !this.giftWrap;
  }
}

const item = new CartItem("Dune");
item.toggleGiftWrap();
console.log(item.giftWrap); // true

// --- example 4 ---
class Price {
  constructor(minorUnits) {
    this.minorUnits = minorUnits; // price in kuruş
  }

  get amount() {
    return this.minorUnits / 100;
  }

  set amount(value) {
    this.minorUnits = Math.round(value * 100);
  }
}

const price = new Price(45000);
console.log(price.amount);     // 450

price.amount = 399.9;
console.log(price.minorUnits); // 39990

// --- example 7 ---
class StoreConfig {
  static defaults;

  static {
    try {
      this.defaults = JSON.parse('{"currency":"TRY","lang":"tr"}');
    } catch {
      this.defaults = { currency: "TRY", lang: "en" };
    }
  }
}

console.log(StoreConfig.defaults.lang); // "tr"

// --- example 9 ---
class CatalogItem {
  constructor(title) {
    this.title = title;
  }

  describe() {
    return `${this.title} is in the catalog`;
  }
}

class EBook extends CatalogItem {
  constructor(title, fileFormat) {
    super(title);
    this.fileFormat = fileFormat;
  }

  describe() {
    return `${super.describe()}: download as ${this.fileFormat}`;
  }
}

const duneEbook = new EBook("Dune", "EPUB");
console.log(duneEbook.describe());
// "Dune is in the catalog: download as EPUB"

console.log(duneEbook instanceof EBook);       // true
console.log(duneEbook instanceof CatalogItem); // true

// --- example 13 ---
class Timer {
  seconds = 0;

  tick() {
    this.seconds++;
    console.log(this.seconds);
  }
}

const timer = new Timer();
setTimeout(timer.tick, 1000); // NaN, because this is no longer timer

// --- example 14 ---
class Box {
  static created = 0;
  #size;

  constructor(size) {
    this.#size = size;
    Box.created++;
  }

  get size() {
    return this.#size;
  }
}

const a = new Box(3);
const b = new Box(5);
a.size = 10;
console.log(a.size, b.size, Box.created, a.created);
