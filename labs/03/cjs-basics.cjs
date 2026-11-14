const first = require("./cart.cjs");
const second = require("./cart.cjs");

first.createCart("Ada");
first.createCart("Alan");

console.log("same object:", first === second);
console.log("carts created:", second.createdCount());
console.log("cached:", Object.keys(require.cache).map((f) => f.split("/").pop()));
