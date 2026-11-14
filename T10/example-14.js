const cart = ["Dune", "Emma"];
const otherCart = cart;
otherCart.push("The Hobbit");

console.log(cart);               // ["Dune", "Emma", "The Hobbit"]
console.log(cart === otherCart); // true
console.log([1, 2] === [1, 2]);  // false
