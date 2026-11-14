// T09: run with `node T09/examples.js` (or paste the blocks into the browser console in order)

// --- example 1 ---
console.log(0.1 + 0.2 === 0.3);
console.log(parseInt("42px"));
console.log(Number("42px"));

// --- example 2 ---
console.log(typeof 5);              // "number"
console.log(typeof 5.5);            // "number"
console.log(Number.isInteger(5.0)); // true
console.log(1_000_000);             // 1000000

// --- example 3 ---
console.log(0.1 + 0.2); // 0.30000000000000004
console.log(1.1 + 2.2); // 3.3000000000000003
console.log(0.1 * 3);   // 0.30000000000000004

// --- example 4 ---
const sum = 0.1 + 0.2;
console.log(sum.toFixed(2));                       // "0.30"
console.log(Math.abs(sum - 0.3) < Number.EPSILON); // true

const priceInKurus = 44999;                        // 449.99 TRY
const totalInKurus = priceInKurus * 3;             // 134997
console.log(totalInKurus / 100);                   // 1349.97

// --- example 6 ---
console.log(Number("42"));      // 42
console.log(Number(" 12 "));    // 12
console.log(Number("42px"));    // NaN
console.log(Number(""));        // 0
console.log(parseInt("42px"));  // 42
console.log(parseInt("3.9"));   // 3
console.log(parseInt("px42"));  // NaN
console.log(parseFloat("3.9kg")); // 3.9
console.log(parseInt("ff", 16));  // 255

// --- example 7 ---
console.log(typeof NaN);              // "number"
console.log(NaN === NaN);             // false
console.log(Number.isNaN(NaN));       // true
console.log(Number.isNaN("hello"));   // false
console.log(isNaN("hello"));          // true

// --- example 8 ---
console.log(1 / 0);                    // Infinity
console.log(-1 / 0);                   // -Infinity
console.log(0 / 0);                    // NaN
console.log(Number.isFinite(1 / 0));   // false
console.log(Number.isFinite(42));      // true

// --- example 9 ---
console.log(Number.MAX_SAFE_INTEGER);          // 9007199254740991
console.log(2 ** 53);                          // 9007199254740992
console.log(2 ** 53 + 1);                      // 9007199254740992
console.log(Number.isSafeInteger(2 ** 53));    // false
console.log(9007199254740993n + 1n);           // 9007199254740994n

// --- example 10 ---
console.log(Math.round(4.5));  // 5
console.log(Math.round(4.4));  // 4
console.log(Math.floor(4.7));  // 4
console.log(Math.ceil(4.1));   // 5
console.log(Math.trunc(4.7));  // 4
console.log(Math.floor(-4.5)); // -5
console.log(Math.trunc(-4.5)); // -4

// --- example 11 ---
console.log(Math.max(450, 320, 390)); // 450
console.log(Math.min(450, 320, 390)); // 320
console.log(Math.abs(-7));            // 7
console.log(2 ** 10);                 // 1024

// --- example 12 ---
const min = 1;
const max = 6;
const dice = Math.floor(Math.random() * (max - min + 1)) + min;
console.log(dice); // an integer from 1 to 6

// --- example 13 ---
const tr = new Intl.NumberFormat("tr-TR");
console.log(tr.format(1234567.891)); // "1.234.567,891"

const cartTotal = 1160; // Dune + Emma + The Hobbit
const tl = new Intl.NumberFormat("tr-TR", { style: "currency", currency: "TRY" });
console.log(tl.format(cartTotal));   // "₺1.160,00"

const usd = new Intl.NumberFormat("en-US", { style: "currency", currency: "USD" });
console.log(usd.format(cartTotal));  // "$1,160.00"

const eur = new Intl.NumberFormat("de-DE", { style: "currency", currency: "EUR" });
console.log(eur.format(cartTotal));  // "1.160,00 €"

// --- example 14 ---
const percent = new Intl.NumberFormat("tr-TR", { style: "percent" });
console.log(percent.format(0.256));  // "%26"

const compact = new Intl.NumberFormat("tr-TR", { notation: "compact" });
console.log(compact.format(1500000)); // "1,5 Mn"

const twoDigits = new Intl.NumberFormat("tr-TR", { maximumFractionDigits: 2 });
console.log(twoDigits.format(3.14159)); // "3,14"

console.log((1234.5).toLocaleString("tr-TR")); // "1.234,5"

// --- example 15 ---
const price = (450).toFixed(2);
console.log(price);              // "450.00"
console.log(price + 40);         // "450.0040"
console.log(Number(price) + 40); // 490

// --- example 16 ---
console.log((5).toFixed(2)); // "5.00"
console.log(5..toFixed(2));  // "5.00"
