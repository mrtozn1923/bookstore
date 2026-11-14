// O03: run with `node O03/examples.js` (or paste the blocks into the browser console in order)

// --- example 1 ---
for (var i = 0; i < 3; i++) {
  setTimeout(() => console.log(i), 0);
}

// --- example 2 ---
const appName = "Bookstore";       // global scope

function showCart() {
  const itemCount = 3;              // function scope
  if (itemCount > 0) {
    const message = `${appName}: ${itemCount} books`; // block scope
    var legacy = "var ignores blocks";
    console.log(message);           // "Bookstore: 3 books"
  }
  console.log(legacy);              // "var ignores blocks"
  console.log(typeof message);      // "undefined", not visible here
}

showCart();
console.log(typeof itemCount);      // "undefined", not visible outside

// --- example 3 ---
const level = "global";

function outer() {
  const level = "outer";
  function inner() {
    console.log(level);
  }
  return inner;
}

function elsewhere() {
  const level = "elsewhere";
  const fn = outer();
  fn();
}

elsewhere(); // "outer"

// --- example 4 ---
console.log(greetUser("Ada")); // "Hello Ada"
console.log(cartTotal);        // undefined
var cartTotal = 770;
console.log(cartTotal);        // 770

function greetUser(name) {
  return `Hello ${name}`;
}

// --- example 5 ---
console.log(typeof sayBye); // "undefined"
var sayBye = function () {
  return "See you";
};
console.log(sayBye());      // "See you"

// --- example 8 ---
function createCounter() {
  let count = 0;
  return function () {
    count++;
    return count;
  };
}

const counter = createCounter();
console.log(counter()); // 1
console.log(counter()); // 2

const another = createCounter();
console.log(another()); // 1
console.log(counter()); // 3

// --- example 9 ---
function createGiftCard(initial) {
  let balance = initial;
  return {
    topUp(amount) {
      balance += amount;
      return balance;
    },
    spend(amount) {
      if (amount > balance) return "Insufficient balance";
      balance -= amount;
      return balance;
    },
    getBalance: () => balance
  };
}

const giftCard = createGiftCard(300);
console.log(giftCard.topUp(150)); // 450
console.log(giftCard.spend(500)); // "Insufficient balance"
console.log(giftCard.balance);    // undefined, no direct access

// --- example 10 ---
function percentOff(percent) {
  return (price) => (price * (100 - percent)) / 100;
}

const tenPercentOff = percentOff(10);
const quarterOff = percentOff(25);
console.log([450, 320, 390].map(tenPercentOff)); // [405, 288, 351]
console.log(quarterOff(320));                    // 240

function once(fn) {
  let done = false;
  return (...args) => {
    if (done) return;
    done = true;
    return fn(...args);
  };
}

const submitOrder = once(() => console.log("Order 1042 submitted"));
submitOrder(); // "Order 1042 submitted"
submitOrder(); // nothing happens

// --- example 11 ---
for (let j = 0; j < 3; j++) {
  setTimeout(() => console.log(j), 0);
}
// 0 1 2

// --- example 12 ---
for (var k = 0; k < 3; k++) {
  (function (copy) {
    setTimeout(() => console.log(copy), 0);
  })(k);
}
// 0 1 2
