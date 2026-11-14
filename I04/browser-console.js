// I04: examples that need a browser; paste these blocks into the DevTools console one by one
// (if a block uses a function from examples.js, paste that file first)

// --- example 2 ---
// one turn of the event loop (simplified)
// 1. Take the oldest task from a task queue and run it to completion.
// 2. Drain the microtask queue completely (including anything added meanwhile).
// 3. If it's time to update the screen, run the rendering step:
//    requestAnimationFrame callbacks, then style, layout, paint.
// 4. Go back to the start.

// --- example 6 ---
document.body.style.background = "red";

const end = performance.now() + 2000;
while (performance.now() < end) {}

document.body.style.background = "";

// --- example 7 ---
document.body.style.background = "red";
requestAnimationFrame(() => {
  document.body.style.background = ""; // red is never painted
});

// To run after a frame has been painted, nest it twice:
// requestAnimationFrame(() => requestAnimationFrame(() => { ... }));

// --- example 9 ---
const button = document.createElement("button");
button.textContent = "Add to cart";
document.body.append(button);

button.addEventListener("click", () => {
  Promise.resolve().then(() => console.log("micro 1"));
  console.log("listener 1");
});
button.addEventListener("click", () => {
  Promise.resolve().then(() => console.log("micro 2"));
  console.log("listener 2");
});

button.click();
// listener 1, listener 2, micro 1, micro 2
