// I03: run with `node I03/examples.js` (or paste the blocks into the browser console in order)

// --- example 1 ---
function createCart() {
  let count = 0;
  return {
    addBook() {
      count += 1;
      return count;
    }
  };
}

const cart = createCart();
cart.addBook();
cart.count = 100;

console.log(cart.addBook());

// --- example 2 ---
class CatalogItem {
  sell() { return "selling"; }
}
class Book extends CatalogItem {
  ship() { return "shipping"; }
}
class EBook extends Book {
  ship() { throw new Error("E-books can't be shipped"); }
}

const ebook = new EBook();
console.log(ebook.sell()); // "selling"
console.log(ebook instanceof Book); // true
// ebook.ship() → Error: E-books can't be shipped

// --- example 3 ---
const canBeSold = (state) => ({
  sell() { return `${state.title} is sold`; }
});
const canBeShipped = (state) => ({
  ship() { return `${state.title} is shipped`; }
});
const canBeDownloaded = (state) => ({
  download() { return `${state.title} is downloaded`; }
});

function createEBook(title) {
  const state = { title };
  return { ...canBeSold(state), ...canBeDownloaded(state) };
}

function createBundle(title) {
  const state = { title };
  return { ...canBeSold(state), ...canBeShipped(state), ...canBeDownloaded(state) };
}

const digitalDune = createEBook("Dune");
const emmaBundle = createBundle("Emma");

console.log(digitalDune.download()); // "Dune is downloaded"
console.log(emmaBundle.ship()); // "Emma is shipped"
console.log("ship" in digitalDune); // false

// --- example 4 ---
function createStockCounter(title) {
  let reserved = 0;
  return {
    reserve() {
      reserved += 1;
      console.log(`${title}: ${reserved} reserved`);
    }
  };
}

const duneStock = createStockCounter("Dune");
const reserve = duneStock.reserve;
reserve(); // "Dune: 1 reserved"

const emmaStock = createStockCounter("Emma");
console.log(duneStock.reserve === emmaStock.reserve); // false

// --- example 5 ---
const Serializable = (Base) => class extends Base {
  serialize() {
    return JSON.stringify(this);
  }
};

const Timestamped = (Base) => class extends Base {
  constructor(...args) {
    super(...args);
    this.createdAt = new Date().toISOString();
  }
};

class Review {
  constructor(text) {
    this.text = text;
  }
}

class StoredReview extends Serializable(Timestamped(Review)) {}

const review = new StoredReview("A classic worth rereading");
console.log(review.serialize());
// {"text":"A classic worth rereading","createdAt":"..."}
console.log(review instanceof Review); // true

// --- example 6 ---
class StoreCredit {
  #balance = 0;

  add(amount) {
    if (amount <= 0) throw new RangeError("Amount must be positive");
    this.#balance += amount;
    return this.#balance;
  }

  static isStoreCredit(value) {
    return #balance in value;
  }
}

const credit = new StoreCredit();
credit.add(250);
credit.balance = 1_000_000;

console.log(credit.add(50)); // 300
console.log(StoreCredit.isStoreCredit(credit)); // true
console.log(StoreCredit.isStoreCredit({ balance: 1 })); // false

// --- example 7 ---
function createNotifier({ send }) {
  return {
    orderPlaced(order) {
      return send(order.email, `Order #${order.id} received. Thanks, ${order.customerName}!`);
    }
  };
}

const consoleNotifier = createNotifier({
  send: (to, text) => console.log(`[${to}] ${text}`)
});

consoleNotifier.orderPlaced({ id: 1042, customerName: "Alan", email: "alan@example.com" });
// "[alan@example.com] Order #1042 received. Thanks, Alan!"

// --- example 8 ---
const author = {
  first: "Jane",
  last: "Austen",
  get full() { return `${this.first} ${this.last}`; }
};

const copy = { ...author };
copy.first = "George";
console.log(copy.full); // "Jane Austen"
