// O05: run with `node O05/examples.js` (or paste the blocks into the browser console in order)

// --- example 1 ---
const prices = {};
const dune = { title: "Dune" };
const emma = { title: "Emma" };

prices[dune] = 450;
prices[emma] = 320;

console.log(prices[dune]);
console.log(new Set([1, "1", 1, NaN, NaN]).size);

// --- example 2 ---
const ada = { name: "Ada" };
const alan = { name: "Alan" };

const lastSeen = new Map();
lastSeen.set(ada, "09:30").set(alan, "11:15");

console.log(lastSeen.get(ada));  // "09:30"
console.log(lastSeen.has(alan)); // true
console.log(lastSeen.size);      // 2

lastSeen.delete(alan);
console.log(lastSeen.size);      // 1

const mixed = new Map();
mixed.set(1, "number").set("1", "string");
console.log(mixed.get(1), mixed.get("1")); // "number" "string"

// --- example 3 ---
const preferences = { currency: "TRY", lang: "en" };
const preferencesMap = new Map(Object.entries(preferences));
console.log(preferencesMap.get("currency")); // "TRY"

const backToObject = Object.fromEntries(preferencesMap);
console.log(backToObject); // { currency: "TRY", lang: "en" }

// --- example 4 ---
const stock = new Map([
  ["Dune", 12],
  ["Frankenstein", 0],
  ["Emma", 8]
]);

for (const [title, qty] of stock) {
  console.log(`${title}: ${qty}`);
}

console.log([...stock.keys()]);   // ["Dune", "Frankenstein", "Emma"]
console.log([...stock.values()]); // [12, 0, 8]

stock.forEach((qty, title) => console.log(title, qty));

// --- example 7 ---
const tags = new Set();
tags.add("classic").add("romance").add("classic");
console.log(tags.size);           // 2
console.log(tags.has("romance")); // true
tags.delete("romance");

const unique = [...new Set([3, 1, 3, 2, 1])];
console.log(unique); // [3, 1, 2]

// --- example 8 ---
const ankaraStock = new Set(["Dune", "Emma", "The Hobbit", "Middlemarch"]);
const izmirStock = new Set(["Frankenstein", "Emma", "Middlemarch", "Pride and Prejudice"]);

console.log(ankaraStock.union(izmirStock));
// Set(6) {"Dune", "Emma", "The Hobbit", "Middlemarch", "Frankenstein", "Pride and Prejudice"}
console.log(ankaraStock.intersection(izmirStock));        // Set(2) {"Emma", "Middlemarch"}
console.log(ankaraStock.difference(izmirStock));          // Set(2) {"Dune", "The Hobbit"}
console.log(ankaraStock.symmetricDifference(izmirStock));
// Set(4) {"Dune", "The Hobbit", "Frankenstein", "Pride and Prejudice"}

console.log(new Set(["Emma", "Middlemarch"]).isSubsetOf(ankaraStock)); // true
console.log(ankaraStock.isSupersetOf(new Set(["Dune"])));              // true
console.log(ankaraStock.isDisjointFrom(new Set(["Frankenstein"])));    // true
