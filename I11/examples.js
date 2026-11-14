// I11: run with `node I11/examples.js` (or paste the blocks into the browser console in order)

// --- example 5 ---
class Money {
  constructor(amount, currency) {
    this.amount = amount;
    this.currency = currency;
  }
  format() { return `${this.amount} ${this.currency}`; }
}

const copy = structuredClone({
  orderedAt: new Date(0),
  categories: new Set(["classics"]),
  price: new Money(450, "TRY")
});

console.log(copy.orderedAt instanceof Date);  // true
console.log(copy.categories.has("classics")); // true
console.log(copy.price instanceof Money);     // false, a plain object: { amount: 450, currency: "TRY" }

try {
  structuredClone({ applyDiscount: (price) => price * 0.9 });
} catch (e) {
  console.log(e.name); // DataCloneError
}
