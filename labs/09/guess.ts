import { Writable } from "node:stream";

// A slow destination: it needs 1 ms per chunk (a slow client, a busy disk)
const slow = new Writable({
    write(chunk, encoding, callback) {
        setTimeout(callback, 1);
    },
});

let firstFalse = 0;
for (let i = 1; i <= 200_000; i++) {
    const ok = slow.write(`ORD-${100000 + i},9780441172719,2,349.90\n`);
    if (!ok && firstFalse === 0) {
        firstFalse = i;
    }
}
console.log(`highWaterMark: ${slow.writableHighWaterMark} bytes`);
console.log(`write() first returned false at line ${firstFalse}`);
console.log(`loop finished, buffered in memory: ${(slow.writableLength / 1024 / 1024).toFixed(1)} MB`);
// Stop the demo here; draining 200,000 chunks would take minutes
slow.destroy();
