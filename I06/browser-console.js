// I06: examples that need a browser; paste these blocks into the DevTools console one by one
// (if a block uses a function from examples.js, paste that file first)

// --- example 10 ---
const all = Iterator.concat(
  ["a", "b"],
  new Set(["c"]),
  naturals().take(2)
);

console.log([...all]); // ["a", "b", "c", 1, 2]
