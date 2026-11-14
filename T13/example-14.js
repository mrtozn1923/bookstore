const once = JSON.stringify({ a: 1 });
const twice = JSON.stringify(once);

console.log(once);  // {"a":1}
console.log(twice); // "{\"a\":1}"
