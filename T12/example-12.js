const original = { stock: 12 };
const alias = original;
alias.stock = 0;

console.log(original.stock);        // 0
console.log(alias === original);    // true
console.log({ a: 1 } === { a: 1 }); // false
