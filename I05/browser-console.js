// I05: examples that need a browser; paste these blocks into the DevTools console one by one
// (if a block uses a function from examples.js, paste that file first)

// --- example 3 ---
function nextClick(element) {
  const { promise, resolve } = Promise.withResolvers();
  element.addEventListener("click", resolve, { once: true });
  return promise;
}

console.log("Click anywhere on the page...");
const event = await nextClick(document.body);
console.log("clicked:", event.clientX, event.clientY);
