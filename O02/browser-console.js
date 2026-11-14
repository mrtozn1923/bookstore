// O02: examples that need a browser; paste these blocks into the DevTools console one by one
// (if a block uses a function from examples.js, paste that file first)

// --- example 8 ---
console.log(Array.from("cat"));                          // ["c", "a", "t"]
console.log(Array.from({ length: 5 }, (_, i) => i * 2)); // [0, 2, 4, 6, 8]

const paragraphs = Array.from(document.querySelectorAll("p"));
console.log(paragraphs.map((el) => el.textContent.length));
