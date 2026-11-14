// I05: run with `node I05/examples.mjs` (or paste the blocks into the browser console in order)

// --- example 1 ---
const ankara = new Promise((resolve) => setTimeout(() => resolve("Ankara"), 100));
const izmir = Promise.reject(new Error("Izmir warehouse is offline"));
const istanbul = new Promise((resolve) => setTimeout(() => resolve("Istanbul"), 300));

Promise.any([izmir, ankara, istanbul]).then((value) => console.log("any:", value));

Promise.race([izmir, ankara, istanbul]).then(
  (value) => console.log("race:", value),
  (error) => console.log("race error:", error.message)
);

// --- example 2 ---
const delay = (ms, value) => new Promise((resolve) => setTimeout(resolve, ms, value));

const results = await Promise.allSettled([
  delay(50, "book details"),
  Promise.reject(new Error("review service is down")),
  delay(80, "recommendations")
]);

for (const result of results) {
  if (result.status === "fulfilled") console.log("ok:", result.value);
  else console.log("error:", result.reason.message);
}

// --- example 4 ---
function parseCart(text) {
  if (!text) throw new Error("cart is empty");
  return JSON.parse(text);
}

// Promise.resolve(parseCart("")) → throws before it ever reaches the chain

Promise.try(parseCart, "")
  .then((cart) => console.log(cart))
  .catch((error) => console.log("caught:", error.message));
// "caught: cart is empty"

// --- example 5 ---
function wait(ms, { signal } = {}) {
  return new Promise((resolve, reject) => {
    signal?.throwIfAborted();

    const onAbort = () => {
      clearTimeout(timerId);
      reject(signal.reason);
    };

    const timerId = setTimeout(() => {
      signal?.removeEventListener("abort", onAbort);
      resolve();
    }, ms);

    signal?.addEventListener("abort", onAbort, { once: true });
  });
}

const controller = new AbortController();

wait(5000, { signal: controller.signal })
  .then(() => console.log("done"))
  .catch((error) => console.log("aborted:", error.name)); // "aborted: AbortError"

controller.abort();

// --- example 6 ---
function withTimeout(promise, ms) {
  let timerId;
  const timeout = new Promise((_, reject) => {
    timerId = setTimeout(() => reject(new Error(`did not finish within ${ms} ms`)), ms);
  });
  return Promise.race([promise, timeout]).finally(() => clearTimeout(timerId));
}

withTimeout(wait(2000), 500).catch((error) => console.log(error.message));
// "did not finish within 500 ms"

// --- example 7 ---
const userController = new AbortController();
const signal = AbortSignal.any([userController.signal, AbortSignal.timeout(500)]);

wait(2000, { signal }).catch((error) => console.log(error.name));
// "TimeoutError"

// --- example 8 ---
async function retry(task, { retries = 3, baseDelay = 200, signal } = {}) {
  for (let attempt = 0; ; attempt++) {
    try {
      return await task({ attempt, signal });
    } catch (error) {
      if (attempt >= retries || signal?.aborted) throw error;
      const backoff = baseDelay * 2 ** attempt;
      const jitter = Math.random() * baseDelay;
      await wait(backoff + jitter, { signal });
    }
  }
}

let calls = 0;
const checkStock = async () => {
  calls += 1;
  if (calls < 3) throw new Error(`attempt ${calls} failed`);
  return "12 in stock";
};

console.log(await retry(checkStock), calls); // "12 in stock" 3

// --- example 9 ---
async function mapWithLimit(items, limit, worker) {
  const results = new Array(items.length);
  let next = 0;

  async function runner() {
    while (next < items.length) {
      const index = next++;
      results[index] = await worker(items[index], index);
    }
  }

  const runners = Array.from({ length: Math.min(limit, items.length) }, runner);
  await Promise.all(runners);
  return results;
}

let active = 0;
const output = await mapWithLimit([1, 2, 3, 4, 5, 6, 7], 2, async (bookId) => {
  active += 1;
  console.log(`start ${bookId} (running: ${active})`);
  await wait(300);
  active -= 1;
  return `${bookId}.jpg`;
});

console.log(output); // ["1.jpg", "2.jpg", "3.jpg", "4.jpg", "5.jpg", "6.jpg", "7.jpg"]
