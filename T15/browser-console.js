// T15: examples that need a browser; paste these blocks into the DevTools console one by one
// (if a block uses a function from examples.js, paste that file first)

// --- example 8 ---
const btn = document.querySelector("button");
btn.onclick = () => console.log("First");
btn.onclick = () => console.log("Second"); // wipes out the first

btn.addEventListener("click", () => console.log("Third"));
btn.addEventListener("click", () => console.log("Fourth")); // both run

// --- example 9 ---
const btn = document.querySelector("button");
btn.addEventListener("click", () => console.log("Click"));
btn.removeEventListener("click", () => console.log("Click"));
