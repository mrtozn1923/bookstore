// I07: run with `node I07/examples.mjs` (or paste the blocks into the browser console in order)

// --- example 2 ---
const countdown = {
  [Symbol.asyncIterator]() {
    let left = 3;
    return {
      next() {
        return new Promise((resolve) => {
          setTimeout(() => {
            if (left === 0) {
              resolve({ value: undefined, done: true });
            } else {
              resolve({ value: left--, done: false });
            }
          }, 500);
        });
      }
    };
  }
};

for await (const n of countdown) {
  console.log(n);
}
console.log("Sale starts!");

// --- example 3 ---
const wait = (ms) => new Promise((r) => setTimeout(r, ms));

async function* countDown(start) {
  for (let i = start; i > 0; i--) {
    await wait(500);
    yield i;
  }
}

for await (const n of countDown(3)) {
  console.log(n);
}

// --- example 4 ---
const requests = [
  wait(300).then(() => "book details"),
  wait(100).then(() => "stock"),
  wait(200).then(() => "reviews")
];

for await (const result of requests) {
  console.log(result);
}

// --- example 5 ---
async function* orderFeed() {
  try {
    let orderId = 1040;
    while (true) {
      await wait(200);
      yield ++orderId;
    }
  } finally {
    console.log("Order feed closed");
  }
}

for await (const orderId of orderFeed()) {
  console.log("New order:", orderId);
  if (orderId === 1043) break;
}

// --- example 6 ---
const list = await Array.fromAsync(countDown(3));
console.log(list); // [3, 2, 1]

const squares = await Array.fromAsync(countDown(3), (n) => n * n);
console.log(squares); // [9, 4, 1]

// --- example 7 ---
async function* books() {
  let page = 1;
  while (true) {
    const url = `https://openlibrary.org/search.json?author=jane+austen&fields=key,title&limit=20&page=${page}`;
    console.log("Sending request, page:", page);
    const response = await fetch(url);
    if (!response.ok) throw new Error(`HTTP ${response.status}`);
    const { docs } = await response.json();
    if (docs.length === 0) return;
    yield* docs;
    page++;
  }
}

for await (const book of books()) {
  if (book.title === "The History of England") {
    console.log("Found:", book.key, book.title);
    break;
  }
}

// --- example 8 ---
const response = await fetch("https://openlibrary.org/search.json?author=jane+austen&limit=1000");
let total = 0;
let chunkCount = 0;

for await (const chunk of response.body) {
  total += chunk.length;
  chunkCount++;
}
console.log(`${chunkCount} chunks, ${total} bytes in total`);

// --- example 9 ---
const stream = new ReadableStream({
  start(controller) {
    controller.enqueue("Hello");
    controller.enqueue("stream");
    controller.close();
  }
});

for await (const chunk of stream) {
  console.log(chunk);
}

// --- example 10 ---
const reader = (await fetch("https://openlibrary.org/search.json?author=jane+austen&limit=20")).body.getReader();

while (true) {
  const { value, done } = await reader.read();
  if (done) break;
  console.log("Chunk:", value.length, "bytes");
}

// --- example 11 ---
async function* filter(source, predicate) {
  for await (const x of source) {
    if (predicate(x)) yield x;
  }
}

console.log(await Array.fromAsync(filter(countDown(5), (n) => n % 2 === 1)));
// [5, 3, 1]
