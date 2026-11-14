function calculate(a, b, operation) {
  return operation(a, b);
}

const add = (a, b) => a + b;
const multiply = (a, b) => a * b;

console.log(calculate(6, 3, add));             // 9
console.log(calculate(6, 3, multiply));        // 18
console.log(calculate(6, 3, (a, b) => a - b)); // 3
