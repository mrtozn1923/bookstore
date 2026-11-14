class Customer {
  greet() {
    return "Hello";
  }
}

console.log(typeof Customer);                    // "function"
console.log(typeof Customer.prototype.greet);    // "function"
console.log(Object.getPrototypeOf(new Customer()) === Customer.prototype); // true
