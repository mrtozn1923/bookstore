// T07: run with `node T07/examples.js` (or paste the blocks into the browser console in order)

// --- example 1 ---
const titles = ["Dune", "Emma", "The Hobbit"];
for (const i in titles) {
  console.log(i + 1);
}

// --- example 2 ---
for (let i = 1; i <= 5; i++) {
  console.log("Lap " + i);
}

// --- example 3 ---
for (let i = 10; i > 0; i -= 3) {
  console.log(i);
}

// --- example 4 ---
const warehouses = ["Ankara", "Izmir", "Istanbul"];
for (let i = 0; i < warehouses.length; i++) {
  console.log("Warehouse " + i + ": " + warehouses[i]);
}

// --- example 5 ---
let value = 1;
let steps = 0;
while (value <= 1000) {
  value *= 2;
  steps++;
}
console.log(value, steps);

// --- example 6 ---
let a = 10;
do {
  console.log("do...while ran:", a);
  a++;
} while (a < 5);

let b = 10;
while (b < 5) {
  console.log("while ran:", b);
  b++;
}

// --- example 7 ---
const cart = ["Dune", "Emma", "Middlemarch"];
for (const title of cart) {
  console.log(title);
}

for (const letter of "Emma") {
  console.log(letter);
}

// --- example 8 ---
const customer = { name: "Ada", city: "Izmir", orders: 3 };
for (const key in customer) {
  console.log(key + ": " + customer[key]);
}

// --- example 9 ---
const prices = [450, 320, 390];
prices.note = "cart prices";
for (const key in prices) {
  console.log(key);
}
for (const price of prices) {
  console.log(price);
}

// --- example 10 ---
const cartPrices = [320, 390, 450, 280, 380];
for (const price of cartPrices) {
  if (price > 400) {
    console.log("First book over 400 TRY:", price);
    break;
  }
}

for (let i = 1; i <= 6; i++) {
  if (i % 2 === 0) {
    continue;
  }
  console.log(i);
}

// --- example 11 ---
for (let i = 0; i < 10; i += 3) {
  if (i === 6) {
    continue;
  }
  console.log(i);
}
