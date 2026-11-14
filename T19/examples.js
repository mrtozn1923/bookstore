// T19: run with `node T19/examples.js` (or paste the blocks into the browser console in order)

// --- example 1 ---
const pattern = /cat/g;
console.log(pattern.test("cat"));
console.log(pattern.test("cat"));

// --- example 2 ---
const r1 = /\d+/g;                  // literal: between two slashes
const r2 = new RegExp("\\d+", "g"); // constructor: the pattern as a string

const title = "dune";
const r3 = new RegExp(title, "i");  // the pattern comes from a variable

// --- example 3 ---
const titlePattern = /dune/i;
console.log(titlePattern.test("Frank Herbert's Dune is back in stock"));  // true
console.log("Frank Herbert's Dune is back in stock".match(titlePattern)); // ["Dune", index: 16, ...]
console.log("Emma".match(/xyz/));                                         // null

const tags = "classic, romance, classic";
console.log(tags.match(/classic/g));                 // ["classic", "classic"]
console.log(tags.replace(/classic/, "bestseller"));  // "bestseller, romance, classic"
console.log(tags.replace(/classic/g, "bestseller")); // "bestseller, romance, bestseller"

// --- example 4 ---
"Order 1042: 2 books, total 770 TRY".match(/\d+/g); // ["1042", "2", "770"]
"Phone: 0532-123-45-67".replace(/\D/g, "");         // "05321234567"

/^\w+$/.test("cafe");      // true
/^\w+$/.test("café");      // false
/^\p{L}+$/u.test("café");  // true

// --- example 5 ---
/^\d{5}$/.test("34000");       // true: exactly 5 digits
/^\d{2,4}$/.test("123");       // true: between 2 and 4 digits
/^\d{3,}$/.test("12");         // false: at least 3 digits
/https?:\/\//.test("http://a"); // true: with or without the s

const quoted = '"Dune" and "Emma"';
quoted.match(/".+"/)[0];  // '"Dune" and "Emma"'
quoted.match(/".+?"/)[0]; // '"Dune"'

// --- example 6 ---
const result = "Delivery date: 2026-03-15".match(/(\d{4})-(\d{2})-(\d{2})/);
console.log(result[0]); // "2026-03-15": the whole match
console.log(result[1]); // "2026": the first group
console.log(result[3]); // "15": the third group

const named = "2026-03-15".match(/(?<year>\d{4})-(?<month>\d{2})-(?<day>\d{2})/);
console.log(named.groups.year, named.groups.month); // "2026" "03"

console.log("2026-03-15".replace(/(\d{4})-(\d{2})-(\d{2})/, "$3.$2.$1")); // "15.03.2026"

// --- example 7 ---
const note = "Restock ISBN 9780441172719 and ISBN 9780141439587 this week";

for (const match of note.matchAll(/ISBN (\d{13})/g)) {
  console.log(match[1], match.index);
}
// "9780441172719" 8
// "9780141439587" 31

// --- example 8 ---
"Dune dune DUNE".match(/dune/gi);  // ["Dune", "dune", "DUNE"]
"one\ntwo\ncafé".match(/^\w+$/gm); // ["one", "two"]
"one\ntwo\ncafé".match(/^\w+$/g);  // null
/istanbul/i.test("İSTANBUL");      // false

// --- example 10 ---
const text = "Price list v3.5 updates 345 books.";
const searchTerm = "3.5";

console.log(text.match(new RegExp(searchTerm, "g")));                // ["3.5", "345"]
console.log(text.match(new RegExp(RegExp.escape(searchTerm), "g"))); // ["3.5"]

// --- example 11 ---
console.log("a1b22c333".match(/\d+/g));
console.log("a1b22c333".match(/\d/g).length);
console.log("2026-03-15".replace(/-/g, "/"));
