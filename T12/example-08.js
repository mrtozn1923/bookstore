const prices = { dune: 450, emma: 320, hobbit: 390 };

console.log(Object.keys(prices));        // ["dune", "emma", "hobbit"]
console.log(Object.values(prices));      // [450, 320, 390]
console.log(Object.entries(prices));
// [["dune", 450], ["emma", 320], ["hobbit", 390]]

console.log(Object.keys(prices).length); // 3
