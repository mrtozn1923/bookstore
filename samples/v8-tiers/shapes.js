// Run with: node --allow-natives-syntax shapes.js
const dune = { title: "Dune", price: 349.9 };
const emma = { title: "Emma", price: 199 };
const hobbit = { price: 289.5, title: "The Hobbit" };

console.log("dune/emma same hidden class:", %HaveSameMap(dune, emma));
console.log("dune/hobbit same hidden class:", %HaveSameMap(dune, hobbit));
