// I11: examples that need a browser; paste these blocks into the DevTools console one by one
// (if a block uses a function from examples.js, paste that file first)

// --- example 1 ---
const code = `onmessage = (e) => postMessage(e.data.price * e.data.quantity);`;
const blob = new Blob([code], { type: "text/javascript" });
const worker = new Worker(URL.createObjectURL(blob));

worker.onmessage = (e) => console.log("from worker:", e.data);

const line = { price: 450, quantity: 2 };
worker.postMessage(line);
line.quantity = 100;
console.log("main thread:", line.quantity);

// --- example 6 ---
const helper = new Worker(URL.createObjectURL(new Blob([`
  onmessage = (e) => {
    const bytes = new Uint8Array(e.data);
    postMessage("Worker received " + bytes.length + " bytes");
  };
`], { type: "text/javascript" })));

helper.onmessage = (e) => console.log(e.data);

const buffer = new ArrayBuffer(64 * 1024 * 1024);
helper.postMessage(buffer, [buffer]);
console.log("Size on the main thread:", buffer.byteLength); // 0

// --- example 10 ---
const probe = new Worker(URL.createObjectURL(new Blob([`
  postMessage({
    document: typeof document,
    window: typeof window,
    localStorage: typeof localStorage,
    fetch: typeof fetch,
    setTimeout: typeof setTimeout,
    indexedDB: typeof indexedDB
  });
`], { type: "text/javascript" })));

probe.onmessage = (e) => console.table(e.data);

// --- example 11 ---
function runInWorker(worker, data) {
  return new Promise((resolve, reject) => {
    worker.addEventListener("message", (e) => resolve(e.data), { once: true });
    worker.addEventListener("error", reject, { once: true });
    worker.postMessage(data);
  });
}

// Usage: const result = await runInWorker(worker, 5_000_000);
