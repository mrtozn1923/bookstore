// T09: examples that need a browser; paste these blocks into the DevTools console one by one
// (if a block uses a function from examples.js, paste that file first)

// --- example 5 ---
let total = 0;
for (let i = 0; i < 10; i++) {
  total += 0.1;
}
console.log(total);                             // 0.9999999999999999
console.log(Math.sumPrecise([0.1, 0.1, 0.1, 0.1, 0.1, 0.1, 0.1, 0.1, 0.1, 0.1])); // 1

console.log(0.1 + 0.2 + 0.3);                   // 0.6000000000000001
console.log(Math.sumPrecise([0.1, 0.2, 0.3]));  // 0.6
console.log(Math.sumPrecise([0.1, 0.2]));       // 0.30000000000000004
