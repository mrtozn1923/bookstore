// I10: run with `node I10/examples.js` (or paste the blocks into the browser console in order)

// --- example 1 ---
function debounce(fn, ms) {
  let timer;
  return (...args) => {
    clearTimeout(timer);
    timer = setTimeout(() => fn(...args), ms);
  };
}

const log = debounce(console.log, 200);
log("a");
log("b");
log("c");
setTimeout(() => log("d"), 300);

// --- example 2 ---
performance.mark("sort-start");

const prices = Array.from({ length: 200_000 }, () => Math.round(100 + Math.random() * 900));
prices.toSorted((a, b) => a - b);

performance.mark("sort-end");

const result = performance.measure("sorting", "sort-start", "sort-end");
console.log(`${result.name}: ${result.duration.toFixed(1)} ms`);

// --- example 4 ---
function throttle(fn, ms) {
  let last = -Infinity;
  let timer = null;
  let lastArgs;

  return (...args) => {
    lastArgs = args;
    const remaining = ms - (performance.now() - last);

    if (remaining <= 0) {
      clearTimeout(timer);
      timer = null;
      last = performance.now();
      fn(...args);
    } else if (!timer) {
      timer = setTimeout(() => {
        timer = null;
        last = performance.now();
        fn(...lastArgs);
      }, remaining);
    }
  };
}

const save = throttle((x) => console.log("ran:", x), 500);
let i = 0;
const ticker = setInterval(() => save(++i), 50);
setTimeout(() => clearInterval(ticker), 2000);
