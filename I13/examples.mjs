// I13: run with `node I13/examples.mjs` (or paste the blocks into the browser console in order)

// --- example 1 ---
const values = new Uint8Array([255, 256, 257, -1]);
console.log(values);

// --- example 2 ---
const blob = new Blob(["Les ", "Misérables"], { type: "text/plain" });
console.log(blob.size, blob.type); // 15 "text/plain"

const text = await blob.text();
console.log(text); // "Les Misérables"

// --- example 6 ---
const buffer = new ArrayBuffer(4);
const bytes = new Uint8Array(buffer);
const words = new Uint16Array(buffer);

bytes[0] = 1;
bytes[1] = 2;

console.log(words[0]); // 513
console.log(buffer.byteLength, bytes.length, words.length); // 4 4 2

// --- example 10 ---
const a = new Uint8Array([10, 20, 30, 40]);
const b = a.subarray(1, 3);
const c = a.slice(1, 3);
b[0] = 99;
c[1] = 77;
console.log(a, b, c);
