// O07: examples that need a browser; paste these blocks into the DevTools console one by one
// (if a block uses a function from examples.js, paste that file first)

// --- example 8 ---
class GiftCard {
  #balance = 0;

  topUp(amount) {
    this.#check(amount);
    this.#balance += amount;
  }

  spend(amount) {
    this.#check(amount);
    if (amount > this.#balance) {
      throw new Error("Insufficient balance");
    }
    this.#balance -= amount;
  }

  get balance() {
    return this.#balance;
  }

  #check(amount) {
    if (amount <= 0) throw new RangeError("Amount must be positive");
  }
}

const card = new GiftCard();
card.topUp(500);
card.spend(120);
console.log(card.balance);  // 380
console.log(card.#balance); // SyntaxError
