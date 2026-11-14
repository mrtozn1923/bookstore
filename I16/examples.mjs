// I16: run with `node I16/examples.mjs` (or paste the blocks into the browser console in order)

// --- example 1 ---
const start = new Date(2026, 0, 31); // January 31, 2026
const next = start;
next.setMonth(1); // one month later

console.log(start.getMonth(), start.getDate());

// --- example 6 ---
function resource(name) {
  console.log(`${name} opened`);
  return {
    [Symbol.dispose]() {
      console.log(`${name} closed`);
    },
  };
}

{
  using a = resource("A");
  using b = resource("B");
  console.log("work done");
}
// A opened, B opened, work done, B closed, A closed

// --- example 7 ---
function measure(label) {
  const start = performance.now();
  return {
    [Symbol.dispose]() {
      console.log(`${label}: ${(performance.now() - start).toFixed(1)} ms`);
    },
  };
}

function heavyWork() {
  using _ = measure("heavyWork");
  let total = 0;
  for (let i = 0; i < 1e6; i++) total += i;
  return total; // the measurement is logged no matter where the function exits
}

heavyWork();

// --- example 9 ---
async function run() {
  await using writer = {
    async [Symbol.asyncDispose]() {
      await new Promise((resolve) => setTimeout(resolve, 500));
      console.log("writer closed");
    },
  };
  console.log("working");
}

await run();
console.log("done");
// working, writer closed, done

// --- example 10 ---
if (typeof Temporal === "undefined") {
  // Safari: load a Temporal polyfill or carry on with Date and Intl
}

// --- example 11 ---
let usingVar = true;
try {
  new Function("{ using x = null; }");
} catch {
  usingVar = false;
}
console.log("using supported?", usingVar);

// --- example 13 ---
try {
  using a = { [Symbol.dispose]() { console.log("a"); } };
  using b = { [Symbol.dispose]() { console.log("b"); } };
  console.log("c");
  throw new Error("d");
} catch (error) {
  console.log(error.message);
}
