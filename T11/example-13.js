function greet(name = "guest") {
  return `Hello ${name}`;
}

console.log(greet(undefined)); // "Hello guest"
console.log(greet(null));      // "Hello null"
