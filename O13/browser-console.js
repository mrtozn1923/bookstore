// O13: examples that need a browser; paste these blocks into the DevTools console one by one
// (if a block uses a function from examples.js, paste that file first)

// --- example 3 ---
document.querySelector("#card").addEventListener("click", (event) => {
  console.log("target:", event.target.id);               // "buy"
  console.log("currentTarget:", event.currentTarget.id); // "card"
});

// --- example 8 ---
document.addEventListener("keydown", (event) => {
  if ((event.ctrlKey || event.metaKey) && event.key === "k") {
    event.preventDefault();
    console.log("Open the book search");
  }
});
