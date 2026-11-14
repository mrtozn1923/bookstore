// I12: examples that need a browser; paste these blocks into the DevTools console one by one
// (if a block uses a function from examples.js, paste that file first)

// --- example 1 ---
const observer = new IntersectionObserver((entries) => {
  console.log("callback ran:", entries.length);
});

observer.observe(document.body);
console.log("observe called");

// --- example 4 ---
const cart = document.createElement("ul");
document.body.append(cart);

const cartObserver = new MutationObserver((records) => {
  console.log("Record count:", records.length);
  for (const r of records) {
    console.log(r.type, r.addedNodes.length, r.attributeName);
  }
});

cartObserver.observe(cart, { childList: true, attributes: true, subtree: true });

for (let i = 0; i < 3; i++) {
  cart.append(document.createElement("li"));
}
cart.setAttribute("data-count", "3");
console.log("Loop done");

// --- example 6 ---
function watchSize(element, handle) {
  const observer = new ResizeObserver((entries) => handle(entries[0]));
  observer.observe(element);
  return () => observer.disconnect(); // cleanup function
}

const stop = watchSize(document.body, (entry) => {
  console.log("Width:", entry.contentBoxSize[0].inlineSize);
});

// When this section is removed from the page:
stop();
