// I09: examples that need a browser; paste these blocks into the DevTools console one by one
// (if a block uses a function from examples.js, paste that file first)

// --- example 4 ---
function openPanel() {
  const bigData = new Array(1_000_000).fill("data");
  window.addEventListener("resize", () => {
    console.log("Panel data:", bigData.length);
  });
}

// --- example 5 ---
function openPanel() {
  const bigData = new Array(1_000_000).fill("data");
  const controller = new AbortController();

  window.addEventListener("resize", () => {
    console.log("Panel data:", bigData.length);
  }, { signal: controller.signal });

  return () => controller.abort();
}

const closePanel = openPanel();
// When the panel closes:
closePanel();

// --- example 8 ---
const removed = [];

function removeCartRow(row) {
  row.remove();
  removed.push(row); // kept for an "undo" feature
}

// --- example 9 ---
const clicks = new WeakMap();

document.addEventListener("click", (e) => {
  const counter = clicks.getOrInsertComputed(e.target, () => ({ count: 0 }));
  counter.count++;
  console.log(e.target.tagName, counter.count);
});
