// T16: examples that need a browser; paste these blocks into the DevTools console one by one
// (if a block uses a function from examples.js, paste that file first)

// --- example 3 ---
const selected = document.querySelector('input[name="shipping"]:checked');
console.log(selected ? selected.value : "Nothing selected");
