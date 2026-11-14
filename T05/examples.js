// T05: run with `node T05/examples.js` (or paste the blocks into the browser console in order)

// --- example 1 ---
console.log(0 || "default");
console.log(0 ?? "default");
console.log("5" == 5);
console.log("5" === 5);

// --- example 2 ---
console.log(7 + 3);
console.log(7 - 3);
console.log(7 * 3);
console.log(7 / 2);
console.log(7 % 3);
console.log(2 ** 10);

// --- example 3 ---
console.log("10" + 2);
console.log("10" - 2);
console.log("3" * "4");
console.log(+"42");
console.log(+"hello");

// --- example 4 ---
let cartTotal = 450;
cartTotal += 390;
cartTotal -= 40;
cartTotal *= 0.9;
cartTotal /= 3;
console.log(cartTotal);

// --- example 5 ---
let n = 5;
const a = n++;
const b = ++n;
console.log(a, b, n);

// --- example 6 ---
console.log(5 > 3);
console.log(5 <= 5);
console.log("Emma" < "Dune");
console.log("10" < "9");
console.log("10" < 9);

// --- example 7 ---
console.log(0 == "");
console.log(0 == "0");
console.log("" == "0");
console.log(null == undefined);
console.log(null == 0);
console.log(NaN == NaN);

// --- example 8 ---
console.log(Boolean(false));
console.log(Boolean(0), Boolean(-0), Boolean(0n));
console.log(Boolean(""));
console.log(Boolean(null), Boolean(undefined));
console.log(Boolean(NaN));

// --- example 9 ---
console.log(Boolean("0"), Boolean("false"), Boolean(" "));
console.log(Boolean([]), Boolean({}));

// --- example 10 ---
console.log("" || "Guest");
console.log("Ada" || "Guest");
console.log("Ada" && "Welcome");
console.log(null && "Welcome");

// --- example 11 ---
const isLoggedIn = true;
isLoggedIn && console.log("Loading your orders...");

const customerName = "";
const displayName = customerName || "Guest";
console.log(displayName);

// --- example 12 ---
const quantity = 0;
console.log(quantity || 1);
console.log(quantity ?? 1);
